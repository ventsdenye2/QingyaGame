using UnityEngine;
using DodgeDots.Core;

namespace DodgeDots.Bullet
{
    /// <summary>
    /// 基础弹幕类
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Bullet : MonoBehaviour
    {
        [Header("弹幕设置")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float lifetime = 10f; // 生命周期，超时自动回收

        private Rigidbody2D _rb;
        private Vector2 _direction;
        private float _currentLifetime;
        private bool _isActive;
        private Team _team; // 弹幕所属阵营

        public float Damage => damage;
        public bool IsActive => _isActive;
        public Team Team => _team;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0; // 弹幕不受重力影响
        }

        private void Update()
        {
            if (!_isActive) return;

            // 更新生命周期
            _currentLifetime -= Time.deltaTime;
            if (_currentLifetime <= 0)
            {
                Deactivate();
            }
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;

            // 移动弹幕
            _rb.velocity = _direction * speed;
        }

        /// <summary>
        /// 初始化弹幕
        /// </summary>
        public void Initialize(Vector2 position, Vector2 direction, Team team, float speed = -1, float damage = -1)
        {
            transform.position = position;
            _direction = direction.normalized;
            _team = team;

            if (speed > 0) this.speed = speed;
            if (damage > 0) this.damage = damage;

            _currentLifetime = lifetime;
            _isActive = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 停用弹幕（回收到对象池）
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            _rb.velocity = Vector2.zero;
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;

            // 阵营检测：避免误伤
            bool isEnemy = other.CompareTag("Enemy") || other.CompareTag("Boss");
            bool isPlayer = other.CompareTag("Player");

            // 玩家阵营的弹幕只能伤害敌人
            if (_team == Team.Player && !isEnemy) return;
            // 敌人阵营的弹幕只能伤害玩家
            if (_team == Team.Enemy && !isPlayer) return;

            // 检测碰撞目标是否可受伤
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.CanTakeDamage)
            {
                damageable.TakeDamage(damage, gameObject);
                Deactivate();
            }
        }

        /// <summary>
        /// 设置弹幕速度
        /// </summary>
        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        /// <summary>
        /// 设置弹幕方向
        /// </summary>
        public void SetDirection(Vector2 newDirection)
        {
            _direction = newDirection.normalized;
        }

        /// <summary>
        /// 设置弹幕伤害
        /// </summary>
        public void SetDamage(float newDamage)
        {
            damage = newDamage;
        }
    }
}
