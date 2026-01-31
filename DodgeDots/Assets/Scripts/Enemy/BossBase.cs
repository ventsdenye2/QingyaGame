using System;
using System.Collections.Generic;
using UnityEngine;
using DodgeDots.Core;

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

        protected float _currentHealth;
        protected BossState _currentState;
        protected int _currentPhase;

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

        protected virtual void Awake()
        {
            _currentHealth = maxHealth;
            _currentState = BossState.Idle;
            _currentPhase = 0;
        }

        protected virtual void Start()
        {
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

        // 以下方法由子类实现具体的Boss行为
        protected abstract void OnBattleStart();
        protected abstract void OnPhaseEnter(int phase);
    }
}
