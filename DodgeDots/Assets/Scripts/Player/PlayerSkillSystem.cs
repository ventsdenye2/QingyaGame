using System;
using System.Collections;
using UnityEngine;
using DodgeDots.Bullet;
using DodgeDots.Core;

namespace DodgeDots.Player
{
    /// <summary>
    /// 玩家技能系统
    /// 处理技能的释放、持续时间和伤害检测
    /// </summary>
    public class PlayerSkillSystem : MonoBehaviour
    {
        private bool _inputLocked = false;
        [Header("攻击技能")]
        [SerializeField] private float skillDuration = 3f;             // 技能持续时间（秒）
        [SerializeField] private float skillDamage = 30f;              // 技能对Boss的伤害
        [SerializeField] private float skillEnergyCost = 60f;          // 攻击技能消耗能量
        [SerializeField] private float bossHealthThreshold = 100f;     // Boss血量阈值（大于此值时改为发射追踪弹）

        [Header("追踪弹幕技能")]
        [SerializeField] private BulletConfig homingBulletConfig;
        [SerializeField] private float homingTurnSpeed = 360f;         // 追踪转向速度（度/秒）
        [SerializeField] private int homingBurstCount = 10;            // 连发数量
        [SerializeField] private float homingBurstInterval = 0.05f;    // 连发间隔（秒）
        [SerializeField] private float instantSkillSpriteDuration = 0.2f;

        [Header("攻击视觉效果")]
        [SerializeField] private Sprite attackActiveSprite;
        [SerializeField] private Sprite homingActiveSprite;

        [Header("护盾技能")]
        [SerializeField] private float shieldDuration = 3f;            // 护盾持续时间（秒）
        [SerializeField] private float shieldEnergyCost = 60f;         // 护盾消耗能量

        [Header("护盾视觉效果")]
        [SerializeField] private SpriteRenderer spriteRenderer;        // 用于显示技能激活的视觉效果
        [SerializeField] private Sprite skillActiveSprite;
        [SerializeField] private Color skillActiveColor = Color.yellow;
        [SerializeField] private Color skillInactiveColor = Color.white;

        [Header("复活技能")]
        [SerializeField] private float resurrectionWaitTime = 1f;     // 复活等待时间（秒）
        [SerializeField] private bool hasResurrection = true;         // 本关卡是否有复活机会

        [Header("音效")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip shieldSfx;
        [SerializeField] private AudioClip resurrectionSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        private PlayerEnergy _playerEnergy;
        private PlayerHealth _playerHealth;
        private PlayerSkillManager _skillManager;
        private Rigidbody2D _rigidbody;
        private CircleCollider2D _collider;
        private bool _isAttackActive = false;
        private bool _isShieldActive = false;
        private int _skillInvincibleRefCount = 0;
        private Coroutine _attackCoroutine;
        private Coroutine _shieldCoroutine;
        private Coroutine _instantSkillVisualCoroutine;
        private GameObject _bossGameObject;
        private Enemy.BossBase _boss;
        private Vector2 _lastFramePosition;  // 上一帧的位置
        private Collider2D _lastContactBoss;     // 上一帧接触的Boss，用于判断是否新接触
        private Sprite _originalSprite;
        private bool _resurrectionUsed = false; // 本关卡复活是否已使用
        private Coroutine _resurrectionCoroutine; // 复活协程

        private enum SkillType
        {
            None,
            Attack,
            AttackHoming,
            Shield,
            Resurrection
        }

        public bool IsSkillActive => _isAttackActive || _isShieldActive;

        public event Action OnSkillStarted;
        public event Action OnSkillEnded;

        private void Start()
        {
            _playerEnergy = GetComponent<PlayerEnergy>();
            _playerHealth = GetComponent<PlayerHealth>();
            _skillManager = GetComponent<PlayerSkillManager>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
                if (sfxSource == null)
                {
                    sfxSource = gameObject.AddComponent<AudioSource>();
                }
                sfxSource.playOnAwake = false;
            }
            sfxSource.volume = sfxVolume;

            // 初始化UI颜色
            if (spriteRenderer != null)
            {
                _originalSprite = spriteRenderer.sprite;
                spriteRenderer.color = skillInactiveColor;
            }

            // 监听玩家死亡事件，自动触发复活
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath += OnPlayerDeath;
            }

            CacheBossReference();
        }

        private void Update()
        {
            if (_inputLocked) return;
            // 教程/暂停期间不允许释放技能（避免消耗能量或改变游戏状态）
            if (Time.timeScale <= 0f)
            {
                return;
            }

            // 监听鼠标左键点击（攻击技能）
            if (Input.GetMouseButtonDown(0))
            {
                TryActivateAttackSkill();
            }

            // 监听鼠标右键点击（护盾技能）
            if (Input.GetMouseButtonDown(1))
            {
                TryActivateShieldSkill();
            }
        }

        private void OnDestroy()
        {
            // 取消监听死亡事件
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath -= OnPlayerDeath;
            }
        }

        /// <summary>
        /// 尝试激活技能
        /// </summary>
        private void TryActivateAttackSkill()
        {
            if (_isAttackActive)
            {
                return; // 技能正在进行中，无法再次激活
            }

            CacheBossReference();
            bool useHoming = _boss != null && _boss.CurrentHealth > bossHealthThreshold;

            if (useHoming && !HasSkill(PlayerSkillType.AttackHoming))
            {
                Debug.Log("攻击技能(追踪)未启用，无法释放。");
                return;
            }

            if (!useHoming && !HasSkill(PlayerSkillType.AttackMelee))
            {
                Debug.Log("攻击技能(近战)未启用，无法释放。");
                return;
            }

            if (_playerEnergy == null || !_playerEnergy.TryConsumeEnergy(skillEnergyCost))
            {
                Debug.Log("能量不足，无法释放技能！");
                return;
            }

            if (useHoming)
            {
                StartCoroutine(FireHomingShotBurst(_boss));
                return;
            }

            ActivateAttackSkill();
        }

        /// <summary>
        /// 激活攻击技能
        /// </summary>
        private void ActivateAttackSkill()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
            }

            Debug.Log("攻击技能激活！");
            _attackCoroutine = StartCoroutine(AttackSkillCoroutine());
        }

        /// <summary>
        /// 攻击技能激活协程
        /// </summary>
        private IEnumerator AttackSkillCoroutine()
        {
            int beforeCount = GetActiveSkillCount();
            _isAttackActive = true;
            if (beforeCount == 0)
            {
                OnSkillStarted?.Invoke();
            }

            UpdateSkillVisual();

            // 记录初始位置
            _lastFramePosition = transform.position;
            _lastContactBoss = null;  // 重置上一帧接触记录

            float elapsedTime = 0f;

            while (elapsedTime < skillDuration)
            {
                elapsedTime += Time.deltaTime;

                // 每帧检查一次与Boss的碰撞（改为更频繁）
                CheckCollisionWithBoss();

                // 更新上一帧位置
                _lastFramePosition = transform.position;

                yield return null;
            }

            EndAttackSkill();
        }

        private IEnumerator FireHomingShotBurst(Enemy.BossBase boss)
        {
            if (BulletManager.Instance == null)
            {
                Debug.LogWarning("BulletManager未初始化，无法发射追踪弹幕。");
                yield break;
            }

            int count = Mathf.Max(1, homingBurstCount);
            float interval = Mathf.Max(0f, homingBurstInterval);

            for (int i = 0; i < count; i++)
            {
                Vector2 origin = transform.position;
                Vector2 direction = (boss.transform.position - transform.position).normalized;

                var bullet = BulletManager.Instance.SpawnBullet(origin, direction, Team.Player, homingBulletConfig);
                if (bullet == null)
                {
                    Debug.LogWarning("生成追踪弹幕失败。");
                    yield break;
                }

                var homing = bullet.GetComponent<HomingBulletBehavior>();
                if (homing == null)
                {
                    homing = bullet.gameObject.AddComponent<HomingBulletBehavior>();
                }

                homing.Configure(boss.transform, homingTurnSpeed);

                if (interval > 0f && i < count - 1)
                {
                    yield return new WaitForSeconds(interval);
                }
            }

            PlayInstantSkillVisual(PlayerSkillType.AttackHoming);

            // 发射追踪弹幕不进入技能持续状态，也不赋予无敌
        }

        private void CacheBossReference()
        {
            if (_boss != null) return;

            _bossGameObject = GameObject.FindGameObjectWithTag("Boss");
            if (_bossGameObject != null)
            {
                _boss = _bossGameObject.GetComponent<Enemy.BossBase>();
            }

            if (_boss == null)
            {
                _boss = FindObjectOfType<Enemy.BossBase>();
            }
        }

        /// <summary>
        /// 尝试激活护盾技能
        /// </summary>
        private void TryActivateShieldSkill()
        {
            if (_isShieldActive)
            {
                return; // 技能正在进行中，无法再次激活
            }

            if (_playerEnergy == null || !_playerEnergy.TryConsumeEnergy(shieldEnergyCost))
            {
                Debug.Log("能量不足，无法释放护盾！");
                return;
            }

            if (!HasSkill(PlayerSkillType.Shield))
            {
                Debug.Log("护盾技能未启用，无法释放。");
                return;
            }

            ActivateShieldSkill();
        }

        /// <summary>
        /// 激活护盾技能
        /// </summary>
        private void ActivateShieldSkill()
        {
            if (_shieldCoroutine != null)
            {
                StopCoroutine(_shieldCoroutine);
            }

            Debug.Log("护盾技能激活！");
            PlayShieldSfx();
            _shieldCoroutine = StartCoroutine(ShieldSkillCoroutine());
        }

        /// <summary>
        /// 护盾技能激活协程
        /// </summary>
        private IEnumerator ShieldSkillCoroutine()
        {
            int beforeCount = GetActiveSkillCount();
            _isShieldActive = true;
            if (beforeCount == 0)
            {
                OnSkillStarted?.Invoke();
            }
            UpdateSkillVisual();
            if (_playerHealth != null)
            {
                SetSkillInvincible(true);
            }

            float elapsedTime = 0f;
            while (elapsedTime < shieldDuration)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            EndShieldSkill();
        }

        /// <summary>
        /// 检查与Boss的碰撞
        /// 检测玩家碰撞体是否真正与Boss碰撞体接触
        /// 每次从"未接触"变为"接触"时造成一次伤害
        /// </summary>
        private void CheckCollisionWithBoss()
        {
            if (_collider == null)
            {
                Debug.LogWarning("[技能检测] 玩家没有CircleCollider2D组件");
                return;
            }

            Debug.Log($"[技能检测] 检测玩家碰撞体与Boss的接触");

            // 使用OverlapCircle检测玩家周围是否有Boss
            // 范围使用碰撞体半径作为基础
            float detectionRadius = _collider.radius;
            Vector2 playerPos = transform.position;
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, detectionRadius);

            Debug.Log($"[技能检测] 玩家位置: {playerPos}, 检测范围: {detectionRadius}, 找到 {hits.Length} 个碰撞体");

            // 本帧是否接触到Boss
            Collider2D currentContactBoss = null;

            foreach (Collider2D col in hits)
            {
                if (col == null || col == _collider)
                {
                    continue;
                }

                Debug.Log($"[技能检测] 检测到碰撞体: {col.gameObject.name}, 标签: {col.tag}");

                // 检查是否是Boss
                Enemy.BossBase boss = col.GetComponent<Enemy.BossBase>();
                if (boss != null)
                {
                    Debug.Log($"[技能检测] 找到Boss! Boss当前状态: {boss.CurrentState}, CanTakeDamage: {boss.CanTakeDamage}");
                    
                    currentContactBoss = col;
                    
                    // 只有当从"未接触"变为"接触"时才造成伤害（新的接触）
                    if (col != _lastContactBoss)
                    {
                        Debug.Log($"[技能检测] 新接触Boss，造成伤害: {skillDamage}");
                        
                        // 对Boss造成伤害
                        boss.TakeDamage(skillDamage, gameObject);
                        Debug.Log($"技能命中Boss！造成 {skillDamage} 点伤害，Boss血量: {boss.CurrentHealth}/{boss.MaxHealth}");
                    }
                    else
                    {
                        Debug.Log($"[技能检测] 继续接触同一个Boss，本帧不造成伤害");
                    }
                    
                    break; // 找到Boss后退出循环
                }
            }

            // 更新上一帧的接触状态
            _lastContactBoss = currentContactBoss;
        }

        /// <summary>
        /// 强制结束技能（用于调试）
        /// </summary>
        public void EndSkill()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }
            if (_shieldCoroutine != null)
            {
                StopCoroutine(_shieldCoroutine);
                _shieldCoroutine = null;
            }

            EndAttackSkill();
            EndShieldSkill();
        }

        /// <summary>
        /// 设置技能持续时间
        /// </summary>
        public void SetSkillDuration(float duration)
        {
            skillDuration = duration;
        }

        /// <summary>
        /// 设置技能伤害
        /// </summary>
        public void SetSkillDamage(float damage)
        {
            skillDamage = damage;
        }

        /// <summary>
        /// 结束当前技能并清理状态
        /// </summary>
        private void EndAttackSkill()
        {
            if (!_isAttackActive) return;
            int beforeCount = GetActiveSkillCount();
            _isAttackActive = false;
            UpdateSkillVisual();
            if (beforeCount > 0 && GetActiveSkillCount() == 0)
            {
                OnSkillEnded?.Invoke();
            }
        }

        private void EndShieldSkill()
        {
            if (!_isShieldActive) return;
            int beforeCount = GetActiveSkillCount();
            _isShieldActive = false;
            SetSkillInvincible(false);
            UpdateSkillVisual();
            if (beforeCount > 0 && GetActiveSkillCount() == 0)
            {
                OnSkillEnded?.Invoke();
            }
        }

        private void SetSkillInvincible(bool active)
        {
            if (_playerHealth != null)
            {
                if (active)
                {
                    _skillInvincibleRefCount++;
                }
                else
                {
                    _skillInvincibleRefCount = Mathf.Max(0, _skillInvincibleRefCount - 1);
                }
                _playerHealth.SetSkillInvincible(_skillInvincibleRefCount > 0);
            }
        }

        private int GetActiveSkillCount()
        {
            return (_isAttackActive ? 1 : 0) + (_isShieldActive ? 1 : 0);
        }

        private void UpdateSkillVisual()
        {
            if (spriteRenderer == null) return;

            if (_isAttackActive)
            {
                // 视觉优先级：攻击形象 > 护盾形象。
                // 需求：开盾期间左键释放“刀形象”的近战技能时，应显示刀而不是一直保持盾。
                if (attackActiveSprite != null)
                {
                    spriteRenderer.sprite = attackActiveSprite;
                }
                else if (_originalSprite != null)
                {
                    spriteRenderer.sprite = _originalSprite;
                }
                spriteRenderer.color = skillInactiveColor;
                return;
            }

            if (_isShieldActive)
            {
                if (skillActiveSprite != null)
                {
                    spriteRenderer.sprite = skillActiveSprite;
                    spriteRenderer.color = Color.white;
                }
                else
                {
                    if (_originalSprite != null)
                    {
                        spriteRenderer.sprite = _originalSprite;
                    }
                    spriteRenderer.color = skillActiveColor;
                }
                return;
            }

            if (_originalSprite != null)
            {
                spriteRenderer.sprite = _originalSprite;
            }
            spriteRenderer.color = skillInactiveColor;
        }

        /// <summary>
        /// 玩家死亡后自动触发复活
        /// </summary>
        private void OnPlayerDeath()
        {
            // 如果已使用复活机会或本关卡没有复活机会，则不复活
            if (_resurrectionUsed || !hasResurrection)
            {
                Debug.Log("[复活技能] 复活机会已用尽或不可用。");
                return;
            }

            // 检查是否有复活技能
            if (!HasSkill(PlayerSkillType.Resurrection))
            {
                Debug.Log("[复活技能] 复活技能未启用，无法复活。");
                return;
            }

            Debug.Log("[复活技能] 检测到玩家死亡，触发复活机制");

            // 启动复活协程
            if (_resurrectionCoroutine != null)
            {
                StopCoroutine(_resurrectionCoroutine);
            }
            _resurrectionCoroutine = StartCoroutine(ResurrectionCoroutine());
        }

        /// <summary>
        /// 复活协程
        /// </summary>
        private IEnumerator ResurrectionCoroutine()
        {
            Debug.Log("[复活技能] 开始复活流程，等待时间：" + resurrectionWaitTime + "秒");

            // 等待指定时间
            yield return new WaitForSeconds(resurrectionWaitTime);

            // 获取当前鼠标位置
            Vector3 resurrectionPosition = GetMouseWorldPosition();

            // 复活玩家
            if (_playerHealth != null)
            {
                _playerHealth.Resurrect();
                Debug.Log("[复活技能] 玩家复活在位置：" + resurrectionPosition);
            }
            PlayResurrectionSfx();

            // 移动玩家到鼠标位置
            if (_rigidbody != null)
            {
                _rigidbody.position = resurrectionPosition;
            }

            // 标记复活已使用
            _resurrectionUsed = true;

            // 通知BossBattleLevel玩家已复活，继续战斗
            NotifyBattleResumed();

            _resurrectionCoroutine = null;
        }

        private void PlayShieldSfx()
        {
            if (shieldSfx == null || sfxSource == null) return;
            sfxSource.PlayOneShot(shieldSfx, sfxVolume);
        }

        private void PlayResurrectionSfx()
        {
            if (resurrectionSfx == null || sfxSource == null) return;
            sfxSource.PlayOneShot(resurrectionSfx, sfxVolume);
        }

        /// <summary>
        /// 通知战斗关卡玩家已复活
        /// </summary>
        private void NotifyBattleResumed()
        {
            var battleLevel = FindObjectOfType<DodgeDots.Level.BossBattleLevel>();
            if (battleLevel != null)
            {
                battleLevel.ResumeBattle();
                Debug.Log("[复活技能] 已通知战斗关卡继续");
            }
        }

        /// <summary>
        /// 获取鼠标的世界坐标
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[复活技能] 主相机为null，使用玩家当前位置");
                return transform.position;
            }

            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            return mouseWorldPos;
        }

        /// <summary>
        /// 重置复活机会（关卡开始时调用）
        /// </summary>
        public void ResetResurrection()
        {
            _resurrectionUsed = false;
            Debug.Log("[复活技能] 复活机会已重置");
        }

        /// <summary>
        /// 检查是否有可用的复活机会
        /// </summary>
        public bool HasAvailableResurrection()
        {
            return !_resurrectionUsed && hasResurrection && HasSkill(PlayerSkillType.Resurrection);
        }

        public void SetInputLocked(bool locked)
        {
            _inputLocked = locked;
        }

        private bool HasSkill(PlayerSkillType skillType)
        {
            if (_skillManager == null) return true;
            return _skillManager.HasSkill(skillType);
        }

        private void ApplySkillVisual(PlayerSkillType skillType)
        {
            if (spriteRenderer == null) return;

            Sprite targetSprite = null;
            switch (skillType)
            {
                case PlayerSkillType.AttackMelee:
                    targetSprite = attackActiveSprite;
                    break;
                case PlayerSkillType.AttackHoming:
                    targetSprite = homingActiveSprite;
                    break;
                case PlayerSkillType.Shield:
                    targetSprite = skillActiveSprite;
                    break;
            }

            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
        }

        private void PlayInstantSkillVisual(PlayerSkillType skillType)
        {
            if (spriteRenderer == null) return;

            if (_instantSkillVisualCoroutine != null)
            {
                StopCoroutine(_instantSkillVisualCoroutine);
            }

            _instantSkillVisualCoroutine = StartCoroutine(InstantSkillVisualCoroutine(skillType));
        }

        private IEnumerator InstantSkillVisualCoroutine(PlayerSkillType skillType)
        {
            ApplySkillVisual(skillType);
            yield return new WaitForSeconds(Mathf.Max(0f, instantSkillSpriteDuration));

            UpdateSkillVisual();
        }
    }
}
