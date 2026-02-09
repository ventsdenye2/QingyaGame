using System;
using UnityEngine;

namespace DodgeDots.Player
{
    /// <summary>
    /// 玩家能量系统
    /// 管理主角的能量值，包括恢复和消耗
    /// </summary>
    public class PlayerEnergy : MonoBehaviour
    {
        [Header("能量设置")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float energyRecoveryRate = 20f;          // 每次恢复的能量值
        [SerializeField] private float energyRecoveryInterval = 2f;       // 恢复间隔（秒）
        [SerializeField] private float safeTimeAfterBullet = 2f;          // 被弹幕击中后需要多长时间才能开始恢复
        [SerializeField] private float skillCost = 100f;                  // 技能消耗的能量

        private float _currentEnergy;
        private float _timeSinceLastDamage = 0f;
        private float _timeSinceLastRecovery = 0f;
        private bool _canRecover = false;
        private PlayerHealth _playerHealth;
        private bool _energyFullTriggered;

        public float CurrentEnergy => _currentEnergy;
        public float MaxEnergy => maxEnergy;
        public float EnergyPercent => _currentEnergy / maxEnergy;
        public bool IsFull => Mathf.Approximately(_currentEnergy, maxEnergy);
        public bool CanUseSkill => _currentEnergy >= skillCost;

        public event Action<float, float> OnEnergyChanged;        // (currentEnergy, maxEnergy)
        public event Action OnEnergyFull;
        public event Action OnSkillCasted;

        [Header("音效")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip energyFullSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("能量提示")]
        [SerializeField] private float energyFullThreshold = 60f;

        private void Awake()
        {
            _currentEnergy = maxEnergy;
            _timeSinceLastDamage = float.MaxValue;
            _timeSinceLastRecovery = 0f;
            _canRecover = false;

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

        private void Start()
        {
            // 获取玩家生命值组件，监听受伤事件
            _playerHealth = GetComponent<PlayerHealth>();
            if (_playerHealth != null)
            {
                _playerHealth.OnDamageTaken += OnPlayerDamaged;
            }

            CheckAndTriggerEnergyFull(true);
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDamageTaken -= OnPlayerDamaged;
            }
        }

        private void Update()
        {
            UpdateRecoveryTimer();
            CheckAndRecoverEnergy();
        }

        /// <summary>
        /// 更新恢复计时器
        /// </summary>
        private void UpdateRecoveryTimer()
        {
            _timeSinceLastDamage += Time.deltaTime;

            // 如果距离上次被击中的时间超过安全时间，允许恢复
            if (_timeSinceLastDamage >= safeTimeAfterBullet)
            {
                _canRecover = true;
            }
        }

        /// <summary>
        /// 检查并恢复能量
        /// </summary>
        private void CheckAndRecoverEnergy()
        {
            if (!_canRecover || IsFull)
            {
                return;
            }

            _timeSinceLastRecovery += Time.deltaTime;

            // 每隔energyRecoveryInterval秒恢复一次
            if (_timeSinceLastRecovery >= energyRecoveryInterval)
            {
                RecoverEnergy(energyRecoveryRate);
                _timeSinceLastRecovery = 0f;
            }
        }

        /// <summary>
        /// 恢复能量
        /// </summary>
        public void RecoverEnergy(float amount)
        {
            if (IsFull) return;

            _currentEnergy = Mathf.Min(maxEnergy, _currentEnergy + amount);

            OnEnergyChanged?.Invoke(_currentEnergy, maxEnergy);

            CheckAndTriggerEnergyFull(false);
        }

        private void PlayEnergyFullSfx()
        {
            if (energyFullSfx == null || sfxSource == null) return;
            sfxSource.PlayOneShot(energyFullSfx, sfxVolume);
        }

        /// <summary>
        /// 消耗能量（用于释放技能）
        /// </summary>
        public bool TryConsumeEnergy(float amount)
        {
            if (_currentEnergy < amount)
            {
                return false;
            }

            _currentEnergy -= amount;
            OnEnergyChanged?.Invoke(_currentEnergy, maxEnergy);
            OnSkillCasted?.Invoke();
            if (_currentEnergy < energyFullThreshold)
            {
                _energyFullTriggered = false;
            }

            return true;
        }

        /// <summary>
        /// 当玩家被击中时调用
        /// </summary>
        private void OnPlayerDamaged()
        {
            // 重置计时器，需要等待safeTimeAfterBullet秒才能恢复
            _timeSinceLastDamage = 0f;
            _canRecover = false;
            _timeSinceLastRecovery = 0f;
        }

        /// <summary>
        /// 重置能量
        /// </summary>
        public void ResetEnergy()
        {
            _currentEnergy = maxEnergy;
            _timeSinceLastDamage = float.MaxValue;
            _canRecover = false;
            _energyFullTriggered = false;
            OnEnergyChanged?.Invoke(_currentEnergy, maxEnergy);
            CheckAndTriggerEnergyFull(true);
        }

        /// <summary>
        /// 设置能量恢复速度
        /// </summary>
        public void SetEnergyRecoveryRate(float rate)
        {
            energyRecoveryRate = rate;
        }

        /// <summary>
        /// 设置能量恢复间隔
        /// </summary>
        public void SetEnergyRecoveryInterval(float interval)
        {
            energyRecoveryInterval = interval;
        }

        private void CheckAndTriggerEnergyFull(bool fromStart)
        {
            if (_energyFullTriggered) return;
            if (_currentEnergy >= energyFullThreshold)
            {
                _energyFullTriggered = true;
                OnEnergyFull?.Invoke();
                PlayEnergyFullSfx();
            }
        }
    }
}
