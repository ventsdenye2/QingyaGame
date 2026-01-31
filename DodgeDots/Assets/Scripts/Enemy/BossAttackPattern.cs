using System.Collections;
using UnityEngine;
using DodgeDots.Bullet;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// Boss攻击模式基类，用于定义可复用的攻击行为
    /// </summary>
    public abstract class BossAttackPattern : MonoBehaviour
    {
        [Header("攻击模式设置")]
        [SerializeField] protected float cooldown = 2f;
        [SerializeField] protected float duration = 1f;

        protected BossBase _boss;
        protected BulletManager _bulletManager;
        protected bool _isExecuting;
        protected float _cooldownTimer;

        public bool IsExecuting => _isExecuting;
        public bool CanExecute => !_isExecuting && _cooldownTimer <= 0;

        protected virtual void Awake()
        {
            _boss = GetComponent<BossBase>();
            _bulletManager = BulletManager.Instance;
        }

        protected virtual void Update()
        {
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// 执行攻击模式
        /// </summary>
        public virtual void Execute()
        {
            if (!CanExecute) return;

            _isExecuting = true;
            StartCoroutine(ExecutePattern());
        }

        /// <summary>
        /// 攻击模式协程（由子类实现具体逻辑）
        /// </summary>
        protected abstract IEnumerator ExecutePattern();

        /// <summary>
        /// 完成攻击模式
        /// </summary>
        protected virtual void FinishPattern()
        {
            _isExecuting = false;
            _cooldownTimer = cooldown;
        }

        /// <summary>
        /// 设置冷却时间
        /// </summary>
        public void SetCooldown(float newCooldown)
        {
            cooldown = newCooldown;
        }

        /// <summary>
        /// 重置冷却
        /// </summary>
        public void ResetCooldown()
        {
            _cooldownTimer = 0;
        }
    }
}
