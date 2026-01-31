using System;
using UnityEngine;
using DodgeDots.Core;

namespace DodgeDots.Player
{
    /// <summary>
    /// 玩家武器系统，支持自动射击和手动技能
    /// </summary>
    public class PlayerWeapon : MonoBehaviour
    {
        [Header("自动射击设置")]
        [SerializeField] private bool autoShootEnabled = true;
        [SerializeField] private float autoShootInterval = 0.2f;
        [SerializeField] private Transform firePoint;

        [Header("技能设置")]
        [SerializeField] private float skillCooldown = 5f;

        [Header("引用")]
        [SerializeField] private GameConfig gameConfig;

        private float _autoShootTimer;
        private float _skillCooldownTimer;
        private Transform _target; // 当前目标（Boss）

        public event Action OnAutoShoot;
        public event Action OnSkillUsed;
        public event Action<float, float> OnSkillCooldownChanged;

        public bool CanUseSkill => _skillCooldownTimer <= 0;
        public float SkillCooldownProgress => Mathf.Clamp01(_skillCooldownTimer / skillCooldown);

        private void Awake()
        {
            if (gameConfig != null)
            {
                autoShootInterval = gameConfig.playerAutoShootInterval;
            }

            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        private void Update()
        {
            // 更新自动射击计时器
            if (autoShootEnabled)
            {
                _autoShootTimer -= Time.deltaTime;
                if (_autoShootTimer <= 0)
                {
                    AutoShoot();
                    _autoShootTimer = autoShootInterval;
                }
            }

            // 更新技能冷却
            if (_skillCooldownTimer > 0)
            {
                _skillCooldownTimer -= Time.deltaTime;
                OnSkillCooldownChanged?.Invoke(_skillCooldownTimer, skillCooldown);
            }

            // 检测技能输入
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                TryUseSkill();
            }
        }

        /// <summary>
        /// 自动射击
        /// </summary>
        private void AutoShoot()
        {
            if (_target == null) return;

            // 计算射击方向
            Vector2 direction = (_target.position - firePoint.position).normalized;

            // 触发射击事件（具体的子弹生成由外部处理）
            OnAutoShoot?.Invoke();

            // TODO: 这里可以通过事件或者直接调用弹幕管理器来生成子弹
        }

        /// <summary>
        /// 尝试使用技能
        /// </summary>
        public void TryUseSkill()
        {
            if (!CanUseSkill) return;

            UseSkill();
            _skillCooldownTimer = skillCooldown;
            OnSkillCooldownChanged?.Invoke(_skillCooldownTimer, skillCooldown);
        }

        /// <summary>
        /// 使用技能（可被子类重写以实现不同技能）
        /// </summary>
        protected virtual void UseSkill()
        {
            OnSkillUsed?.Invoke();
            // TODO: 实现具体的技能效果
        }

        /// <summary>
        /// 设置目标
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// 启用/禁用自动射击
        /// </summary>
        public void SetAutoShoot(bool enabled)
        {
            autoShootEnabled = enabled;
        }

        /// <summary>
        /// 获取射击点位置
        /// </summary>
        public Vector3 GetFirePointPosition()
        {
            return firePoint.position;
        }

        /// <summary>
        /// 获取朝向目标的方向
        /// </summary>
        public Vector2 GetDirectionToTarget()
        {
            if (_target == null) return Vector2.up;
            return (_target.position - firePoint.position).normalized;
        }
    }
}
