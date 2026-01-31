using System;
using UnityEngine;
using DodgeDots.Core;

namespace DodgeDots.Player
{
    /// <summary>
    /// 玩家生命值管理
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IHealth, IDamageable
    {
        [Header("生命值设置")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invincibleDuration = 1f; // 受伤后的无敌时间

        private float _currentHealth;
        private bool _isInvincible;
        private float _invincibleTimer;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public bool CanTakeDamage => !_isInvincible && IsAlive;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnDamageTaken;
        public event Action OnInvincibleStart;
        public event Action OnInvincibleEnd;

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        private void Update()
        {
            // 更新无敌时间
            if (_isInvincible)
            {
                _invincibleTimer -= Time.deltaTime;
                if (_invincibleTimer <= 0)
                {
                    _isInvincible = false;
                    OnInvincibleEnd?.Invoke();
                }
            }
        }

        public void TakeDamage(float damage, GameObject source = null)
        {
            if (!CanTakeDamage) return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            OnDamageTaken?.Invoke();

            // 启动无敌时间
            if (_currentHealth > 0 && invincibleDuration > 0)
            {
                _isInvincible = true;
                _invincibleTimer = invincibleDuration;
                OnInvincibleStart?.Invoke();
            }

            // 检查是否死亡
            if (_currentHealth <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            _currentHealth = maxHealth;
            _isInvincible = false;
            _invincibleTimer = 0;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// 设置最大生命值
        /// </summary>
        public void SetMaxHealth(float value)
        {
            maxHealth = value;
            _currentHealth = Mathf.Min(_currentHealth, maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// 是否处于无敌状态
        /// </summary>
        public bool IsInvincible => _isInvincible;
    }
}
