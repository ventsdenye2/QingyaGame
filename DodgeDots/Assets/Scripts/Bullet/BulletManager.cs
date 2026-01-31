using UnityEngine;
using DodgeDots.Core;

namespace DodgeDots.Bullet
{
    /// <summary>
    /// 弹幕管理器，负责弹幕的生成、回收和对象池管理
    /// </summary>
    public class BulletManager : MonoBehaviour
    {
        [Header("弹幕预制体")]
        [SerializeField] private Bullet bulletPrefab;

        [Header("对象池设置")]
        [SerializeField] private int initialPoolSize = 100;
        [SerializeField] private int maxPoolSize = 500;

        [Header("引用")]
        [SerializeField] private GameConfig gameConfig;

        private ObjectPool<Bullet> _bulletPool;
        private Transform _bulletContainer;

        private static BulletManager _instance;
        public static BulletManager Instance => _instance;

        private void Awake()
        {
            // 单例模式
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // 从配置加载设置
            if (gameConfig != null)
            {
                initialPoolSize = gameConfig.bulletPoolInitialSize;
                maxPoolSize = gameConfig.objectPoolMaxCapacity;
            }

            // 创建弹幕容器
            _bulletContainer = new GameObject("BulletContainer").transform;
            _bulletContainer.SetParent(transform);

            // 初始化对象池
            if (bulletPrefab != null)
            {
                _bulletPool = new ObjectPool<Bullet>(bulletPrefab, initialPoolSize, maxPoolSize, _bulletContainer);
            }
        }

        /// <summary>
        /// 生成单个弹幕
        /// </summary>
        public Bullet SpawnBullet(Vector2 position, Vector2 direction, Team team, float speed = -1, float damage = -1)
        {
            if (_bulletPool == null) return null;

            var bullet = _bulletPool.Get();
            bullet.Initialize(position, direction, team, speed, damage);
            return bullet;
        }

        /// <summary>
        /// 生成圆形弹幕
        /// </summary>
        public void SpawnCirclePattern(Vector2 position, int bulletCount, Team team, float speed = -1, float damage = -1, float startAngle = 0)
        {
            float angleStep = 360f / bulletCount;

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                SpawnBullet(position, direction, team, speed, damage);
            }
        }

        /// <summary>
        /// 生成扇形弹幕
        /// </summary>
        public void SpawnFanPattern(Vector2 position, Vector2 centerDirection, int bulletCount, float spreadAngle, Team team, float speed = -1, float damage = -1)
        {
            if (bulletCount <= 0) return;

            float centerAngle = Mathf.Atan2(centerDirection.y, centerDirection.x) * Mathf.Rad2Deg;
            float startAngle = centerAngle - spreadAngle / 2f;
            float angleStep = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0;

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                SpawnBullet(position, direction, team, speed, damage);
            }
        }

        /// <summary>
        /// 回收弹幕
        /// </summary>
        public void ReturnBullet(Bullet bullet)
        {
            if (_bulletPool == null || bullet == null) return;
            _bulletPool.Return(bullet);
        }

        /// <summary>
        /// 清空所有弹幕
        /// </summary>
        public void ClearAllBullets()
        {
            if (_bulletContainer == null) return;

            foreach (Transform child in _bulletContainer)
            {
                var bullet = child.GetComponent<Bullet>();
                if (bullet != null && bullet.IsActive)
                {
                    bullet.Deactivate();
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
