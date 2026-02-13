using System;
using System.Collections;
using UnityEngine;
using DodgeDots.Bullet;
using DodgeDots.Core;

namespace DodgeDots.Player
{
    /// <summary>
    /// 玩家技能系统
    /// 处理技能的释放、持续时间和伤害检测，以及动画状态切换
    /// </summary>
    public class PlayerSkillSystem : MonoBehaviour
    {
        [Header("攻击技能")]
        [SerializeField] private float skillDuration = 0.5f;           // 【建议】设为和动画时长接近，如 0.5f
        [SerializeField] private float skillDamage = 30f;              // 技能对Boss的伤害
        [SerializeField] private float skillEnergyCost = 60f;          // 攻击技能消耗能量
        [SerializeField] private float bossHealthThreshold = 100f;     // Boss血量阈值

        [Header("追踪弹幕技能")]
        [SerializeField] private BulletConfig homingBulletConfig;
        [SerializeField] private float homingTurnSpeed = 360f;
        [SerializeField] private int homingBurstCount = 10;
        [SerializeField] private float homingBurstInterval = 0.05f;
        [SerializeField] private float instantSkillSpriteDuration = 0.2f;

        [Header("攻击视觉效果")]
        [SerializeField] private Sprite attackActiveSprite; // 如果没有动画机，将回退使用此图片
        [SerializeField] private Sprite homingActiveSprite;

        [Header("护盾技能")]
        [SerializeField] private float shieldDuration = 3f;
        [SerializeField] private float shieldEnergyCost = 60f;

        [Header("护盾视觉效果")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite skillActiveSprite;
        [SerializeField] private Color skillActiveColor = Color.yellow;
        [SerializeField] private Color skillInactiveColor = Color.white;

        [Header("复活技能")]
        [SerializeField] private float resurrectionWaitTime = 1f;
        [SerializeField] private bool hasResurrection = true;

        [Header("音效")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip shieldSfx;
        [SerializeField] private AudioClip resurrectionSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        // --- 内部变量 ---
        private PlayerEnergy _playerEnergy;
        private PlayerHealth _playerHealth;
        private PlayerSkillManager _skillManager;
        private Rigidbody2D _rigidbody;
        private CircleCollider2D _collider;

        // --- 动画相关 ---
        private Animator _animator;
        // 【修改】改为 Trigger 类型的 Hash，名称必须与 Animator 面板里的参数名 "Attack" 一致
        private static readonly int AnimParamAttackTrigger = Animator.StringToHash("Attack");

        private bool _isAttackActive = false;
        private bool _isShieldActive = false;
        private int _skillInvincibleRefCount = 0;

        private Coroutine _attackCoroutine;
        private Coroutine _shieldCoroutine;
        private Coroutine _instantSkillVisualCoroutine;
        private Coroutine _resurrectionCoroutine;

        private GameObject _bossGameObject;
        private Enemy.BossBase _boss;
        private Vector2 _lastFramePosition;
        private Collider2D _lastContactBoss;
        private Sprite _originalSprite;
        private bool _resurrectionUsed = false;

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

            // 获取 Animator (优先自身，其次子物体)
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
                if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            sfxSource.volume = sfxVolume;

            // 初始化UI颜色
            if (spriteRenderer != null)
            {
                _originalSprite = spriteRenderer.sprite;
                spriteRenderer.color = skillInactiveColor;
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnDeath += OnPlayerDeath;
            }

            CacheBossReference();
        }

        private bool _inputLocked = false;
        public void SetInputLocked(bool locked) => _inputLocked = locked;

        private void Update()
        {
            if (_inputLocked) return;

            // 左键攻击
            if (Input.GetMouseButtonDown(0)) TryActivateAttackSkill();
            // 右键护盾
            if (Input.GetMouseButtonDown(1)) TryActivateShieldSkill();
        }

        private void OnDestroy()
        {
            if (_playerHealth != null) _playerHealth.OnDeath -= OnPlayerDeath;
        }

        // --- 攻击逻辑 ---

        private void TryActivateAttackSkill()
        {
            Debug.Log(">>> [2] 进入 TryActivateAttackSkill 方法");

            if (_isAttackActive)
            {
                Debug.LogWarning(">>> [X] 失败：正在攻击中 (_isAttackActive = true)，禁止重复触发");
                return;
            }

            CacheBossReference(); // 尝试获取Boss

            // 打印 Boss 状态
            Debug.Log($">>> [3] Boss引用: {(_boss != null ? _boss.name : "null")}");

            // 判定是否使用追踪弹（Boss存在且血量大于阈值）
            bool useHoming = _boss != null && _boss.CurrentHealth > bossHealthThreshold;
            Debug.Log($">>> [4] 判定模式: {(useHoming ? "远程追踪(Homing)" : "近战普攻(Melee)")}");

            // 检查技能是否解锁
            if (useHoming)
            {
                if (!HasSkill(PlayerSkillType.AttackHoming))
                {
                    Debug.LogWarning(">>> [X] 失败：没学会 AttackHoming 技能");
                    return;
                }
            }
            else
            {
                // 近战检查
                if (!HasSkill(PlayerSkillType.AttackMelee))
                {
                    Debug.LogWarning(">>> [X] 失败：没学会 AttackMelee 技能 (请检查 PlayerSkillManager)");
                    return;
                }
            }

            Debug.Log(">>> [5] 技能已学会，准备检查能量...");

            // 检查能量
            if (_playerEnergy == null)
            {
                Debug.LogError(">>> [X] 致命错误：找不到 PlayerEnergy 组件！无法扣除能量！");
                return;
            }

            // 尝试扣除能量
            bool energyConsumed = _playerEnergy.TryConsumeEnergy(skillEnergyCost);
            if (!energyConsumed)
            {
                Debug.LogWarning($">>> [X] 失败：能量不足！需要: {skillEnergyCost}");
                return;
            }

            Debug.Log(">>> [6] 能量扣除成功，准备执行 ActivateAttackSkill");

            if (_animator != null)
            {
                Debug.Log($">>> 正在控制物体 [{_animator.gameObject.name}] 上的 Animator 发送 Trigger 'Attack'");
                _animator.SetTrigger(AnimParamAttackTrigger);
            }

            if (useHoming)
            {
                StartCoroutine(FireHomingShotBurst(_boss));
                return;
            }

            ActivateAttackSkill();
        }

        private void ActivateAttackSkill()
        {
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            // Debug.Log("攻击技能激活！");
            _attackCoroutine = StartCoroutine(AttackSkillCoroutine());
        }

        private IEnumerator AttackSkillCoroutine()
        {
            int beforeCount = GetActiveSkillCount();
            _isAttackActive = true;
            if (beforeCount == 0) OnSkillStarted?.Invoke();

            // 1. 【核心修改】设置 Trigger，触发一次攻击动画
            // 只要 Animator 设置了 "Attack" -> "Idle" (Has Exit Time = true)，它就会自动播完回来

            Debug.Log($"尝试触发动画 Trigger: Attack | Animator是否为空: {_animator == null}");
            if (_animator != null)
            {
                _animator.SetTrigger(AnimParamAttackTrigger);
            }

            // 更新视觉（仅当没有动画机时才生效）
            UpdateSkillVisual();

            _lastFramePosition = transform.position;
            _lastContactBoss = null;

            float elapsedTime = 0f;

            // 这里控制的是“伤害判定持续时间”
            // 注意：如果你的动画很短，建议在 Inspector 把 Skill Duration 改小一点（例如 0.5 或 0.6）
            while (elapsedTime < skillDuration)
            {
                elapsedTime += Time.deltaTime;
                CheckCollisionWithBoss();
                _lastFramePosition = transform.position;
                yield return null;
            }

            EndAttackSkill();
        }

        private void EndAttackSkill()
        {
            if (!_isAttackActive) return;
            int beforeCount = GetActiveSkillCount();

            _isAttackActive = false;

            // Trigger 不需要手动关闭，它是一次性的
            // 这里只需要恢复一些内部状态即可

            UpdateSkillVisual();

            if (beforeCount > 0 && GetActiveSkillCount() == 0)
            {
                OnSkillEnded?.Invoke();
            }
        }

        // --- 追踪弹逻辑 ---

        private IEnumerator FireHomingShotBurst(Enemy.BossBase boss)
        {
            if (BulletManager.Instance == null) yield break;

            int count = Mathf.Max(1, homingBurstCount);
            float interval = Mathf.Max(0f, homingBurstInterval);

            for (int i = 0; i < count; i++)
            {
                Vector2 origin = transform.position;
                Vector2 direction = (boss.transform.position - transform.position).normalized;

                var bullet = BulletManager.Instance.SpawnBullet(origin, direction, Team.Player, homingBulletConfig);
                if (bullet != null)
                {
                    var homing = bullet.GetComponent<HomingBulletBehavior>();
                    if (homing == null) homing = bullet.gameObject.AddComponent<HomingBulletBehavior>();
                    homing.Configure(boss.transform, homingTurnSpeed);
                }

                if (interval > 0f && i < count - 1) yield return new WaitForSeconds(interval);
            }

            PlayInstantSkillVisual(PlayerSkillType.AttackHoming);
        }

        private void CacheBossReference()
        {
            if (_boss != null) return;
            _bossGameObject = GameObject.FindGameObjectWithTag("Boss");
            if (_bossGameObject != null) _boss = _bossGameObject.GetComponent<Enemy.BossBase>();
            if (_boss == null) _boss = FindObjectOfType<Enemy.BossBase>();
        }

        // --- 护盾逻辑 ---

        private void TryActivateShieldSkill()
        {
            if (_isShieldActive) return;

            if (_playerEnergy == null || !_playerEnergy.TryConsumeEnergy(shieldEnergyCost))
            {
                Debug.Log("能量不足，无法释放护盾！");
                return;
            }

            if (!HasSkill(PlayerSkillType.Shield)) return;

            ActivateShieldSkill();
        }

        private void ActivateShieldSkill()
        {
            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);

            Debug.Log("护盾技能激活！");
            PlayShieldSfx();
            _shieldCoroutine = StartCoroutine(ShieldSkillCoroutine());
        }

        private IEnumerator ShieldSkillCoroutine()
        {
            int beforeCount = GetActiveSkillCount();
            _isShieldActive = true;
            if (beforeCount == 0) OnSkillStarted?.Invoke();

            UpdateSkillVisual();

            if (_playerHealth != null) SetSkillInvincible(true);

            float elapsedTime = 0f;
            while (elapsedTime < shieldDuration)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            EndShieldSkill();
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

        // --- 碰撞检测 ---

        private void CheckCollisionWithBoss()
        {
            if (_collider == null) return;

            float detectionRadius = _collider.radius;
            Vector2 playerPos = transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, detectionRadius);

            Collider2D currentContactBoss = null;

            foreach (Collider2D col in hits)
            {
                if (col == null || col == _collider) continue;

                Enemy.BossBase boss = col.GetComponent<Enemy.BossBase>();
                if (boss != null)
                {
                    currentContactBoss = col;
                    if (col != _lastContactBoss)
                    {
                        boss.TakeDamage(skillDamage, gameObject);
                        Debug.Log($"技能命中Boss！造成 {skillDamage} 点伤害");
                    }
                    break;
                }
            }
            _lastContactBoss = currentContactBoss;
        }

        // --- 工具方法 ---

        public void EndSkill()
        {
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            if (_shieldCoroutine != null) StopCoroutine(_shieldCoroutine);
            EndAttackSkill();
            EndShieldSkill();
        }

        public void SetSkillDuration(float duration) => skillDuration = duration;
        public void SetSkillDamage(float damage) => skillDamage = damage;

        private void SetSkillInvincible(bool active)
        {
            if (_playerHealth != null)
            {
                if (active) _skillInvincibleRefCount++;
                else _skillInvincibleRefCount = Mathf.Max(0, _skillInvincibleRefCount - 1);
                _playerHealth.SetSkillInvincible(_skillInvincibleRefCount > 0);
            }
        }

        private int GetActiveSkillCount()
        {
            return (_isAttackActive ? 1 : 0) + (_isShieldActive ? 1 : 0);
        }

        /// <summary>
        /// 更新玩家视觉效果（兼容 Animator 和 旧版 Sprite 替换）
        /// </summary>
        private void UpdateSkillVisual()
        {
            if (spriteRenderer == null) return;

            // 1. 攻击状态
            if (_isAttackActive)
            {
                // 如果没有 Animator，手动切图；如果有 Animator，它自己会切，我们不管。
                if (_animator == null)
                {
                    if (attackActiveSprite != null) spriteRenderer.sprite = attackActiveSprite;
                    else if (_originalSprite != null) spriteRenderer.sprite = _originalSprite;
                }

                // 颜色可以保留，通常 Animator 不控制颜色
                spriteRenderer.color = skillInactiveColor;
                return;
            }

            // 2. 护盾状态
            if (_isShieldActive)
            {
                // 注意：如果 Animator 正在播放 Idle 动画，它会每帧重置 Sprite。
                // 如果想要护盾发光，改颜色是最安全的。
                if (skillActiveSprite != null && _animator == null)
                {
                    spriteRenderer.sprite = skillActiveSprite;
                    spriteRenderer.color = Color.white;
                }
                else
                {
                    // 如果有动画机，只变色
                    if (_animator == null && _originalSprite != null)
                    {
                        spriteRenderer.sprite = _originalSprite;
                    }
                    spriteRenderer.color = skillActiveColor;
                }
                return;
            }

            // 3. 默认状态 (Idle)
            if (_animator == null)
            {
                // 只有在没动画机时才手动还原图片
                if (_originalSprite != null) spriteRenderer.sprite = _originalSprite;
            }
            spriteRenderer.color = skillInactiveColor;
        }

        // --- 复活逻辑 ---

        private void OnPlayerDeath()
        {
            if (_resurrectionUsed || !hasResurrection) return;
            if (!HasSkill(PlayerSkillType.Resurrection)) return;

            if (_resurrectionCoroutine != null) StopCoroutine(_resurrectionCoroutine);
            _resurrectionCoroutine = StartCoroutine(ResurrectionCoroutine());
        }

        private IEnumerator ResurrectionCoroutine()
        {
            Debug.Log("[复活技能] 开始复活流程");
            yield return new WaitForSeconds(resurrectionWaitTime);

            Vector3 resurrectionPosition = GetMouseWorldPosition();
            if (_playerHealth != null) _playerHealth.Resurrect();
            PlayResurrectionSfx();

            if (_rigidbody != null) _rigidbody.position = resurrectionPosition;

            _resurrectionUsed = true;
            NotifyBattleResumed();
            _resurrectionCoroutine = null;
        }

        private void PlayShieldSfx()
        {
            if (shieldSfx != null && sfxSource != null) sfxSource.PlayOneShot(shieldSfx, sfxVolume);
        }

        private void PlayResurrectionSfx()
        {
            if (resurrectionSfx != null && sfxSource != null) sfxSource.PlayOneShot(resurrectionSfx, sfxVolume);
        }

        private void NotifyBattleResumed()
        {
            var battleLevel = FindObjectOfType<DodgeDots.Level.BossBattleLevel>();
            if (battleLevel != null) battleLevel.ResumeBattle();
        }

        private Vector3 GetMouseWorldPosition()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return transform.position;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            return mouseWorldPos;
        }

        public void ResetResurrection() => _resurrectionUsed = false;
        public bool HasAvailableResurrection() => !_resurrectionUsed && hasResurrection && HasSkill(PlayerSkillType.Resurrection);

        private bool HasSkill(PlayerSkillType skillType)
        {
            if (_skillManager == null) return true;
            return _skillManager.HasSkill(skillType);
        }

        // 用于瞬发技能（如追踪弹）的短暂视觉效果
        private void PlayInstantSkillVisual(PlayerSkillType skillType)
        {
            if (spriteRenderer == null) return;
            if (_instantSkillVisualCoroutine != null) StopCoroutine(_instantSkillVisualCoroutine);
            _instantSkillVisualCoroutine = StartCoroutine(InstantSkillVisualCoroutine(skillType));
        }

        private IEnumerator InstantSkillVisualCoroutine(PlayerSkillType skillType)
        {
            // 如果有动画机，瞬发技能的切图可能无效，这里保留逻辑作为备用
            if (_animator == null)
            {
                Sprite targetSprite = null;
                switch (skillType)
                {
                    case PlayerSkillType.AttackHoming: targetSprite = homingActiveSprite; break;
                }
                if (targetSprite != null) spriteRenderer.sprite = targetSprite;
            }

            yield return new WaitForSeconds(Mathf.Max(0f, instantSkillSpriteDuration));
            UpdateSkillVisual();
        }
    }
}