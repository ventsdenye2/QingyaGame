using System;
using System.Collections;
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

        [Header("受击闪红")]
        [SerializeField] private SpriteRenderer playerSprite;
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float damageFlashDuration = 0.12f;

        [Header("音效")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip damageSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        private float _currentHealth;
        private bool _isInvincible;
        private bool _isSkillInvincible;
        private bool _isDialogueInvincible;
        private float _invincibleTimer;
        private Coroutine _damageFlashRoutine;
        private bool _flashColorCached;
        private Color _flashOriginalColor;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public bool CanTakeDamage => !_isInvincible && !_isSkillInvincible && !_isDialogueInvincible && IsAlive;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnDamageTaken;
        public event Action OnInvincibleStart;
        public event Action OnInvincibleEnd;
        public event Action OnResurrected; // 復活事件

        private void Awake()
        {
            _currentHealth = maxHealth;

            if (playerSprite == null)
            {
                playerSprite = GetComponent<SpriteRenderer>();
            }

            if (playerSprite != null && !_flashColorCached)
            {
                _flashOriginalColor = playerSprite.color;
                _flashColorCached = true;
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
            FlashSpriteOnDamage();
            PlayDamageSfx();

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

        private void FlashSpriteOnDamage()
        {
            if (playerSprite == null) return;

            if (_damageFlashRoutine != null)
            {
                StopCoroutine(_damageFlashRoutine);
            }

            _damageFlashRoutine = StartCoroutine(FlashSpriteRoutine());
        }

        private IEnumerator FlashSpriteRoutine()
        {
            if (!_flashColorCached)
            {
                _flashOriginalColor = playerSprite.color;
                _flashColorCached = true;
            }

            playerSprite.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration);
            if (playerSprite != null)
            {
                playerSprite.color = _flashOriginalColor;
            }
        }

        private void PlayDamageSfx()
        {
            if (damageSfx == null || sfxSource == null) return;
            sfxSource.PlayOneShot(damageSfx, sfxVolume);
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
            _isSkillInvincible = false;
            _isDialogueInvincible = false;
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

        /// <summary>
        /// 设置技能无敌（护盾）
        /// </summary>
        public void SetSkillInvincible(bool active)
        {
            _isSkillInvincible = active;
        }

        /// <summary>
        /// 文案/对话显示期间的无敌（例如Boss阶段标题与对白出现时）
        /// </summary>
        public void SetDialogueInvincible(bool active)
        {
            _isDialogueInvincible = active;
        }

        /// <summary>
        /// 复活玩家
        /// </summary>
        public void Resurrect()
        {
            if (IsAlive) return; // 只有死亡才能复活

            _currentHealth = maxHealth;
            _isInvincible = false;
            _invincibleTimer = 0;
            _isSkillInvincible = false;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            OnResurrected?.Invoke();
        }
    }
}
