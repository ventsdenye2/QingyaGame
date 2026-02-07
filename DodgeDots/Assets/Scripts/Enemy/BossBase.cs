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

        [Header("文案配置")]
        [SerializeField] protected BossDialogueConfig dialogueConfig;

        protected float _currentHealth;
        protected BossState _currentState;
        protected int _currentPhase;
        [System.NonSerialized] protected BulletManager _bulletManager;
        protected Transform _playerTransform;

        // 发射源管理
        protected Dictionary<EmitterType, EmitterPoint> _emitters;

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
            // 攻击和移动现在由BossSequenceController驱动
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
            SetState(BossState.Defeated);
            OnDeath?.Invoke();
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
        /// 获取发射源组件（供BossSequenceController使用）
        /// </summary>
        public virtual EmitterPoint GetEmitter(EmitterType emitterType)
        {
            if (_emitters != null && _emitters.TryGetValue(emitterType, out EmitterPoint emitter))
            {
                return emitter;
            }
            return null;
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
        /// 执行攻击动作（供BossSequenceController调用）
        /// </summary>
        public void ExecuteAttackAction(BossAttackAction attackAction)
        {
            if (_currentState != BossState.Fighting || attackAction == null)
            {
                Debug.LogWarning("[BossBase] Cannot execute attack action: not in Fighting state or attackAction is null");
                return;
            }

            if (_bulletManager == null)
            {
                Debug.LogWarning("[BossBase] BulletManager is null, cannot execute attack");
                return;
            }

            // 支持多发射源同时发射
            if (attackAction.useMultipleEmitters && attackAction.multipleEmitters != null && attackAction.multipleEmitters.Length > 0)
            {
                foreach (EmitterType emitterType in attackAction.multipleEmitters)
                {
                    ExecuteSingleEmitterAttackAction(attackAction, emitterType);
                }
            }
            else
            {
                // 单发射源发射
                ExecuteSingleEmitterAttackAction(attackAction, attackAction.emitterType);
            }
        }

        /// <summary>
        /// 从单个发射源执行攻击动作
        /// </summary>
        protected virtual void ExecuteSingleEmitterAttackAction(BossAttackAction attackAction, EmitterType emitterType)
        {
            Vector2 position = GetEmitterPosition(emitterType);

            switch (attackAction.attackType)
            {
                case BossAttackType.Circle:
                    _bulletManager.SpawnCirclePattern(
                        position,
                        attackAction.circleCount,
                        Team.Enemy,
                        attackAction.bulletConfig,
                        attackAction.circleStartAngle
                    );
                    break;

                case BossAttackType.Fan:
                    Vector2 fanDirection = new Vector2(
                        Mathf.Cos(attackAction.fanCenterAngle * Mathf.Deg2Rad),
                        Mathf.Sin(attackAction.fanCenterAngle * Mathf.Deg2Rad)
                    );
                    _bulletManager.SpawnFanPattern(
                        position,
                        fanDirection,
                        attackAction.fanCount,
                        attackAction.fanSpreadAngle,
                        Team.Enemy,
                        attackAction.bulletConfig
                    );
                    break;

                case BossAttackType.Single:
                    Vector2 singleDirection = new Vector2(
                        Mathf.Cos(attackAction.singleDirection * Mathf.Deg2Rad),
                        Mathf.Sin(attackAction.singleDirection * Mathf.Deg2Rad)
                    );
                    _bulletManager.SpawnBullet(
                        position,
                        singleDirection,
                        Team.Enemy,
                        attackAction.bulletConfig
                    );
                    break;

                case BossAttackType.Aiming:
                    Vector2 aimDirection = GetAimingDirection(
                        position,
                        attackAction.aimingPredictMovement,
                        attackAction.bulletConfig
                    );
                    _bulletManager.SpawnFanPattern(
                        position,
                        aimDirection,
                        attackAction.aimingBulletCount,
                        attackAction.aimingSpreadAngle,
                        Team.Enemy,
                        attackAction.bulletConfig
                    );
                    break;

                case BossAttackType.Custom:
                    OnCustomAttackAction(attackAction);
                    break;
            }
        }

        /// <summary>
        /// 自定义攻击动作（由子类重写实现特殊攻击）
        /// </summary>
        protected virtual void OnCustomAttackAction(BossAttackAction attackAction)
        {
            Debug.LogWarning($"自定义攻击 {attackAction.customAttackId} 未实现");
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
        /// 自定义攻击（由子类重写实现特殊攻击）
        /// </summary>
        protected virtual void OnCustomAttack(BossAttackData attackData)
        {
            Debug.LogWarning($"自定义攻击 {attackData.customAttackId} 未实现");
        }

        /// <summary>
        /// 自定义移动（由子类重写实现特殊移动）
        /// 供BossSequenceController调用
        /// </summary>
        public virtual void ExecuteCustomMove(EmitterType emitterType, int customMoveId)
        {
            Debug.LogWarning($"自定义移动 {customMoveId} for {emitterType} 未实现");
        }

        // 以下方法由子类实现具体的Boss行为
        protected abstract void OnBattleStart();
        protected abstract void OnPhaseEnter(int phase);
    }
}
