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
        [SerializeField] protected List<float> phaseHealthThresholds = new List<float> { 0.7f, 0.4f };

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

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public bool CanTakeDamage => _currentState == BossState.Fighting;
        public BossState CurrentState => _currentState;
        public int CurrentPhase => _currentPhase;

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

            // 子类可以重写此方法进行初始化
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
            float previousHealthPercent = previousHealth / maxHealth;
            float currentHealthPercent = currentHealth / maxHealth;

            for (int i = 0; i < phaseHealthThresholds.Count; i++)
            {
                if (previousHealthPercent > phaseHealthThresholds[i] &&
                    currentHealthPercent <= phaseHealthThresholds[i])
                {
                    EnterPhase(i + 1);
                    break;
                }
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
        /// 执行攻击
        /// </summary>
        protected virtual void ExecuteAttack(BossAttackData attackData)
        {
            if (_bulletManager == null || attackData == null)
            {
                Debug.LogWarning("BulletManager或AttackData为空，无法执行攻击");
                return;
            }

            Vector2 bossPosition = transform.position;

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

        // 以下方法由子类实现具体的Boss行为
        protected abstract void OnBattleStart();
        protected abstract void OnPhaseEnter(int phase);
    }
}
