using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DodgeDots.Core;
using DodgeDots.Bullet;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// Boss状态枚举
    /// </summary>
    public enum BossState
    {
        Idle,       // 空闲
        Fighting,   // 战斗中
        Defeated    // 被击败
    }

    /// <summary>
    /// Boss基类，提供可扩展的Boss行为框架
    /// </summary>
    public abstract class BossBase : MonoBehaviour, IHealth, IDamageable
    {
        [Header("Boss基础设置")]
        [SerializeField] protected float maxHealth = 1000f;
        [SerializeField] protected string bossName = "Boss";

        [Header("阶段设置")]
        [SerializeField] protected List<float> phaseHealthThresholds = new List<float> { 0.7f, 0.3f };

        [Header("攻击配置")]
        [SerializeField] protected BossAttackConfig attackConfig;

        [Header("文案配置")]
        [SerializeField] protected BossDialogueConfig dialogueConfig;

        protected float _currentHealth;
        protected BossState _currentState;
        protected int _currentPhase;
        [System.NonSerialized] protected BulletManager _bulletManager;
        protected Coroutine _attackCoroutine;
        protected int _currentAttackIndex;
        protected Transform _playerTransform;

        // 发射源管理
        protected Dictionary<EmitterType, EmitterPoint> _emitters;
        protected EmitterPoint _defaultEmitter;

        // 节拍驱动模式：若启用，boss 不会自动执行攻击循环，仅响应 TriggerBeatAttack()
        [System.NonSerialized] public bool beatDrivenMode = false;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public bool CanTakeDamage => _currentState == BossState.Fighting;
        public BossState CurrentState => _currentState;
        public int CurrentPhase => _currentPhase;
        public string BossName => bossName;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action<int> OnPhaseChanged;
        public event Action<BossState> OnStateChanged;
        public event Action<BossPhaseDialogue> OnPhaseDialogue;

        protected virtual void Awake()
        {
            _currentHealth = maxHealth;
            _currentState = BossState.Idle;
            _currentPhase = 0;
            _currentAttackIndex = 0;
        }

        protected virtual void Start()
        {
            // 在Start中获取BulletManager，确保单例已初始化
            _bulletManager = BulletManager.Instance;

            if (_bulletManager == null)
            {
                Debug.LogError("BulletManager未找到！请确保场景中存在BulletManager组件。");
            }

            // 初始化发射源
            InitializeEmitters();
            CachePlayerTransform();

            // 子类可以重写此方法进行初始化
        }

        /// <summary>
        /// 初始化发射源
        /// 自动查找Boss身上所有的EmitterPoint组件并注册
        /// </summary>
        protected virtual void InitializeEmitters()
        {
            _emitters = new Dictionary<EmitterType, EmitterPoint>();

            // 查找所有子物体上的EmitterPoint组件
            EmitterPoint[] emitterPoints = GetComponentsInChildren<EmitterPoint>();

            foreach (EmitterPoint emitter in emitterPoints)
            {
                if (!_emitters.ContainsKey(emitter.EmitterType))
                {
                    _emitters.Add(emitter.EmitterType, emitter);
                    Debug.Log($"注册发射源: {emitter.EmitterType} at {emitter.name}");
                }
                else
                {
                    Debug.LogWarning($"发射源类型 {emitter.EmitterType} 已存在，跳过 {emitter.name}");
                }
            }

            // 如果没有找到MainCore，使用Boss自身位置作为默认发射源
            if (!_emitters.ContainsKey(EmitterType.MainCore))
            {
                Debug.LogWarning("未找到MainCore发射源，将使用Boss自身位置作为默认发射源");
            }
        }

        public virtual void TakeDamage(float damage, GameObject source = null)
        {
            if (!CanTakeDamage) return;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            // 检查是否进入新阶段
            CheckPhaseTransition(previousHealth, _currentHealth);

            // 检查是否死亡
            if (_currentHealth <= 0)
            {
                OnBossDefeated();
            }
        }

        public virtual void Heal(float amount)
        {
            if (!IsAlive) return;

            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public virtual void ResetHealth()
        {
            _currentHealth = maxHealth;
            _currentPhase = 0;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public virtual void StartBattle()
        {
            SetState(BossState.Fighting);
            OnBattleStart();

            // 如果启用了节拍驱动模式，不启动自动攻击循环，由外部节拍控制器驱动
            if (beatDrivenMode)
            {
                Debug.Log("[BossBase] Beat-driven mode enabled. Waiting for TriggerBeatAttack() calls instead of auto attack loop.");
                return;
            }

            // 如果有攻击配置，启动攻击循环
            if (attackConfig != null && attackConfig.attackSequence != null && attackConfig.attackSequence.Length > 0)
            {
                _attackCoroutine = StartCoroutine(AttackLoopCoroutine());
            }
        }

        /// <summary>
        /// 检查阶段转换
        /// </summary>
        protected virtual void CheckPhaseTransition(float previousHealth, float currentHealth)
        {
            float currentHealthPercent = currentHealth / maxHealth;

            // 遍历所有阶段阈值，找到当前应该处于的最高阶段
            int targetPhase = 0;
            for (int i = 0; i < phaseHealthThresholds.Count; i++)
            {
                if (currentHealthPercent <= phaseHealthThresholds[i])
                {
                    targetPhase = i + 1;
                }
                else
                {
                    break; // 因为列表是降序的，一旦血量比例高于某个阈值就可以停止
                }
            }

            // 只有当进入新阶段时才触发
            if (targetPhase > _currentPhase)
            {
                EnterPhase(targetPhase);
            }
        }

        /// <summary>
        /// 进入新阶段
        /// </summary>
        protected virtual void EnterPhase(int phase)
        {
            _currentPhase = phase;
            OnPhaseChanged?.Invoke(_currentPhase);

            // 触发阶段文案
            if (dialogueConfig != null)
            {
                BossPhaseDialogue dialogue = dialogueConfig.GetDialogueByPhase(_currentPhase);
                if (dialogue != null)
                {
                    OnPhaseDialogue?.Invoke(dialogue);
                }
            }

            OnPhaseEnter(_currentPhase);
        }

        /// <summary>
        /// 设置Boss状态
        /// </summary>
        protected virtual void SetState(BossState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            OnStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Boss被击败时调用
        /// </summary>
        protected virtual void OnBossDefeated()
        {
            StopAttackLoop();
            SetState(BossState.Defeated);
            OnDeath?.Invoke();
        }

        /// <summary>
        /// 攻击循环协程
        /// </summary>
        protected virtual IEnumerator AttackLoopCoroutine()
        {
            while (_currentState == BossState.Fighting && attackConfig != null)
            {
                // 执行当前攻击
                BossAttackData currentAttack = attackConfig.attackSequence[_currentAttackIndex];
                yield return StartCoroutine(ExecuteAttackCoroutine(currentAttack));

                // 移动到下一个攻击
                _currentAttackIndex++;

                // 检查是否需要循环
                if (_currentAttackIndex >= attackConfig.attackSequence.Length)
                {
                    if (attackConfig.loopSequence)
                    {
                        _currentAttackIndex = 0;
                        // 完成一轮后的延迟
                        if (attackConfig.delayAfterLoop > 0)
                        {
                            yield return new WaitForSeconds(attackConfig.delayAfterLoop);
                        }
                    }
                    else
                    {
                        // 不循环则停止
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 执行单个攻击的协程
        /// </summary>
        protected virtual IEnumerator ExecuteAttackCoroutine(BossAttackData attackData)
        {
            // 攻击前延迟
            if (attackData.delayBeforeAttack > 0)
            {
                yield return new WaitForSeconds(attackData.delayBeforeAttack);
            }

            // 如果有移动配置，同时执行移动和攻击
            if (attackData.moveType != BossMoveType.None)
            {
                StartCoroutine(ExecuteMoveCoroutine(attackData));
            }

            // 执行攻击
            ExecuteAttack(attackData);
        }

        /// <summary>
        /// 获取发射源位置
        /// </summary>
        protected virtual Vector2 GetEmitterPosition(EmitterType emitterType)
        {
            if (_emitters != null && _emitters.TryGetValue(emitterType, out EmitterPoint emitter))
            {
                return emitter.Position;
            }

            // 如果找不到指定的发射源，使用Boss自身位置
            Debug.LogWarning($"未找到发射源 {emitterType}，使用Boss自身位置");
            return transform.position;
        }

        /// <summary>
        /// 缂撳瓨Player Transform
        /// </summary>
        protected virtual void CachePlayerTransform()
        {
            if (_playerTransform != null) return;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
            }
        }

        /// <summary>
        /// 鑾峰彇鐬勫噯鏂瑰悜锛堝彲閫夐娴嬭€佸緱鍒扮殑浣嶇疆锛?
        /// </summary>
        protected virtual Vector2 GetAimingDirection(Vector2 origin, bool predictMovement, BulletConfig config)
        {
            CachePlayerTransform();
            if (_playerTransform == null)
            {
                Debug.LogWarning("Aiming attack failed: Player transform not found.");
                return Vector2.up;
            }

            Vector2 targetPosition = _playerTransform.position;

            if (predictMovement)
            {
                Rigidbody2D playerBody = _playerTransform.GetComponent<Rigidbody2D>();
                if (playerBody != null)
                {
                    float bulletSpeed = (config != null && config.defaultSpeed > 0f) ? config.defaultSpeed : 5f;
                    float distance = Vector2.Distance(origin, targetPosition);
                    float leadTime = bulletSpeed > 0.01f ? distance / bulletSpeed : 0f;
                    targetPosition += playerBody.velocity * leadTime;
                }
            }

            Vector2 direction = targetPosition - origin;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Vector2.up;
            }

            return direction.normalized;
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        protected virtual void ExecuteAttack(BossAttackData attackData)
        {
            if (_bulletManager == null || attackData == null)
            {
                Debug.LogWarning("BulletManager或AttackData为空，无法执行攻击");
                return;
            }

            // 支持多发射源同时发射
            if (attackData.useMultipleEmitters && attackData.multipleEmitters != null && attackData.multipleEmitters.Length > 0)
            {
                foreach (EmitterType emitterType in attackData.multipleEmitters)
                {
                    ExecuteSingleEmitterAttack(attackData, emitterType);
                }
            }
            else
            {
                // 单发射源发射
                ExecuteSingleEmitterAttack(attackData, attackData.emitterType);
            }
        }

        /// <summary>
        /// 从单个发射源执行攻击
        /// </summary>
        protected virtual void ExecuteSingleEmitterAttack(BossAttackData attackData, EmitterType emitterType)
        {
            Vector2 bossPosition = GetEmitterPosition(emitterType);

            switch (attackData.attackType)
            {
                case BossAttackType.Circle:
                    _bulletManager.SpawnCirclePattern(
                        bossPosition,
                        attackData.circleCount,
                        Team.Enemy,
                        attackData.bulletConfig,
                        attackData.circleStartAngle
                    );
                    break;

                case BossAttackType.Fan:
                    Vector2 fanDirection = new Vector2(
                        Mathf.Cos(attackData.fanCenterAngle * Mathf.Deg2Rad),
                        Mathf.Sin(attackData.fanCenterAngle * Mathf.Deg2Rad)
                    );
                    _bulletManager.SpawnFanPattern(
                        bossPosition,
                        fanDirection,
                        attackData.fanCount,
                        attackData.fanSpreadAngle,
                        Team.Enemy,
                        attackData.bulletConfig
                    );
                    break;

                case BossAttackType.Single:
                    Vector2 singleDirection = new Vector2(
                        Mathf.Cos(attackData.singleDirection * Mathf.Deg2Rad),
                        Mathf.Sin(attackData.singleDirection * Mathf.Deg2Rad)
                    );
                    _bulletManager.SpawnBullet(
                        bossPosition,
                        singleDirection,
                        Team.Enemy,
                        attackData.bulletConfig
                    );
                    break;

                case BossAttackType.Aiming:
                    Vector2 aimDirection = GetAimingDirection(
                        bossPosition,
                        attackData.aimingPredictMovement,
                        attackData.bulletConfig
                    );
                    _bulletManager.SpawnFanPattern(
                        bossPosition,
                        aimDirection,
                        attackData.aimingBulletCount,
                        attackData.aimingSpreadAngle,
                        Team.Enemy,
                        attackData.bulletConfig
                    );
                    break;

                case BossAttackType.Custom:
                    OnCustomAttack(attackData);
                    break;
            }
        }

        /// <summary>
        /// 停止攻击循环
        /// </summary>
        protected virtual void StopAttackLoop()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }
        }

        /// <summary>
        /// 自定义攻击（由子类重写实现特殊攻击）
        /// </summary>
        protected virtual void OnCustomAttack(BossAttackData attackData)
        {
            Debug.LogWarning($"自定义攻击 {attackData.customAttackId} 未实现");
        }

        /// <summary>
        /// 执行移动的协程
        /// </summary>
        protected virtual IEnumerator ExecuteMoveCoroutine(BossAttackData attackData)
        {
            Vector3 startPosition = transform.position;
            float elapsedTime = 0f;

            switch (attackData.moveType)
            {
                case BossMoveType.ToPosition:
                    // 移动到目标位置
                    while (elapsedTime < attackData.moveDuration)
                    {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / attackData.moveDuration;
                        transform.position = Vector3.Lerp(startPosition, attackData.targetPosition, t);
                        yield return null;
                    }
                    transform.position = attackData.targetPosition;
                    break;

                case BossMoveType.ByDirection:
                    // 沿方向移动
                    Vector2 direction = new Vector2(
                        Mathf.Cos(attackData.moveDirection * Mathf.Deg2Rad),
                        Mathf.Sin(attackData.moveDirection * Mathf.Deg2Rad)
                    );
                    Vector3 targetPos = startPosition + (Vector3)(direction * attackData.moveDistance);

                    while (elapsedTime < attackData.moveDuration)
                    {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / attackData.moveDuration;
                        transform.position = Vector3.Lerp(startPosition, targetPos, t);
                        yield return null;
                    }
                    transform.position = targetPos;
                    break;

                case BossMoveType.Custom:
                    // 自定义移动
                    yield return StartCoroutine(OnCustomMove(attackData));
                    break;
            }
        }

        /// <summary>
        /// 自定义移动（由子类重写实现特殊移动）
        /// </summary>
        protected virtual IEnumerator OnCustomMove(BossAttackData attackData)
        {
            Debug.LogWarning($"自定义移动 {attackData.customMoveId} 未实现");
            yield return null;
        }

        /// <summary>
        /// 由节拍控制器触发，立即执行当前攻击序列的下一个攻击
        /// </summary>
        public virtual void TriggerBeatAttack()
        {
            if (_currentState != BossState.Fighting || attackConfig == null || attackConfig.attackSequence == null || attackConfig.attackSequence.Length == 0)
            {
                Debug.LogWarning("[BossBase] Cannot trigger beat attack: not in Fighting state or no attack config");
                return;
            }

            // 获取当前攻击数据
            BossAttackData currentAttack = attackConfig.attackSequence[_currentAttackIndex];
            
            // 直接执行攻击（不经过延迟协程）
            ExecuteAttack(currentAttack);
            
            Debug.Log($"[BossBase] TriggerBeatAttack executed attack #{_currentAttackIndex}");

            // 准备下一个攻击
            _currentAttackIndex++;
            if (_currentAttackIndex >= attackConfig.attackSequence.Length)
            {
                if (attackConfig.loopSequence)
                {
                    _currentAttackIndex = 0;
                }
                else
                {
                    _currentAttackIndex = attackConfig.attackSequence.Length - 1; // 停留在最后一个
                }
            }
        }

        // 以下方法由子类实现具体的Boss行为
        protected abstract void OnBattleStart();
        protected abstract void OnPhaseEnter(int phase);
    }
}
