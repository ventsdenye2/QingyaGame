using UnityEngine;
using DodgeDots.Core;
using System.Collections.Generic;

namespace DodgeDots.Bullet
{
    /// <summary>
    /// 弹幕管理器，负责弹幕的生成、回收和对象池管理
    /// 支持多种子弹类型和配置
    /// </summary>
    public class BulletManager : MonoBehaviour
    {
        [Header("默认弹幕预制体")]
        [SerializeField] private Bullet defaultBulletPrefab;

        [Header("预注册的子弹类型")]
        [SerializeField] private BulletTypeEntry[] bulletTypes;

        [Header("对象池设置")]
        [SerializeField] private int initialPoolSize = 100;
        [SerializeField] private int maxPoolSize = 500;

        [Header("引用")]
        [SerializeField] private GameConfig gameConfig;

        // 多对象池管理：每种配置对应一个对象池
        private Dictionary<BulletConfig, ObjectPool<Bullet>> _bulletPools;
        private ObjectPool<Bullet> _defaultPool; // 默认对象池（无配置时使用）
        private Transform _bulletContainer;

        private static BulletManager _instance;
        public static BulletManager Instance => _instance;

        /// <summary>
        /// 子弹类型条目（用于Inspector配置）
        /// </summary>
        [System.Serializable]
        public class BulletTypeEntry
        {
            public BulletConfig config;
            public Bullet prefab;
        }

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

            // 初始化多对象池字典
            _bulletPools = new Dictionary<BulletConfig, ObjectPool<Bullet>>();

            // 初始化默认对象池
            if (defaultBulletPrefab != null)
            {
                _defaultPool = new ObjectPool<Bullet>(defaultBulletPrefab, initialPoolSize, maxPoolSize, _bulletContainer);
            }

            // 预注册子弹类型
            if (bulletTypes != null)
            {
                foreach (var entry in bulletTypes)
                {
                    if (entry.config != null && entry.prefab != null)
                    {
                        RegisterBulletType(entry.config, entry.prefab);
                    }
                }
            }
        }

        /// <summary>
        /// 注册新的子弹类型
        /// </summary>
        public void RegisterBulletType(BulletConfig config, Bullet prefab)
        {
            if (config == null || prefab == null)
            {
                Debug.LogWarning("无法注册子弹类型：配置或预制体为空");
                return;
            }

            if (_bulletPools.ContainsKey(config))
            {
                Debug.LogWarning($"子弹类型 {config.bulletName} 已经注册");
                return;
            }

            var pool = new ObjectPool<Bullet>(prefab, initialPoolSize / 2, maxPoolSize, _bulletContainer);
            _bulletPools[config] = pool;
            Debug.Log($"注册子弹类型：{config.bulletName}");
        }

        /// <summary>
        /// 获取或创建对象池
        /// </summary>
        private ObjectPool<Bullet> GetOrCreatePool(BulletConfig config)
        {
            // 如果没有指定配置，使用默认池
            if (config == null)
            {
                return _defaultPool;
            }

            // 如果已有对应的池，直接返回
            if (_bulletPools.TryGetValue(config, out var pool))
            {
                return pool;
            }

            // 如果没有，使用默认预制体创建新池
            if (defaultBulletPrefab != null)
            {
                pool = new ObjectPool<Bullet>(defaultBulletPrefab, initialPoolSize / 2, maxPoolSize, _bulletContainer);
                _bulletPools[config] = pool;
                Debug.Log($"为配置 {config.bulletName} 创建新对象池");
                return pool;
            }

            return _defaultPool;
        }

        /// <summary>
        /// 生成单个弹幕（使用配置）
        /// </summary>
        public Bullet SpawnBullet(Vector2 position, Vector2 direction, Team team, BulletConfig config = null)
        {
            var pool = GetOrCreatePool(config);
            if (pool == null) return null;

            var bullet = pool.Get();
            bullet.Initialize(position, direction, team, config);
            return bullet;
        }

        /// <summary>
        /// 生成单个弹幕（兼容旧版本）
        /// </summary>
        public Bullet SpawnBullet(Vector2 position, Vector2 direction, Team team, float speed = -1, float damage = -1)
        {
            if (_defaultPool == null) return null;

            var bullet = _defaultPool.Get();
            bullet.Initialize(position, direction, team, speed, damage);
            return bullet;
        }

        /// <summary>
        /// 生成圆形弹幕（使用配置）
        /// </summary>
        public void SpawnCirclePattern(Vector2 position, int bulletCount, Team team, BulletConfig config = null, float startAngle = 0)
        {
            float angleStep = 360f / bulletCount;

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                SpawnBullet(position, direction, team, config);
            }
        }

        /// <summary>
        /// 生成圆形弹幕（兼容旧版本）
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
        /// 生成扇形弹幕（使用配置）
        /// </summary>
        public void SpawnFanPattern(Vector2 position, Vector2 centerDirection, int bulletCount, float spreadAngle, Team team, BulletConfig config = null)
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

                SpawnBullet(position, direction, team, config);
            }
        }

        /// <summary>
        /// 生成扇形弹幕（兼容旧版本）
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
        /// 生成螺旋弹幕（使用配置）
        /// </summary>
        public void SpawnSpiralPattern(Vector2 position, int bulletCount, float turns, float startAngle, float radiusGrowth, Team team, BulletConfig config = null)
        {
            if (bulletCount <= 0) return;

            float totalAngle = turns * 360f;
            float angleStep = totalAngle / bulletCount;

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle + angleStep * i;
                float radius = radiusGrowth * i;
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                SpawnBullet(position + offset, direction, team, config);
            }
        }

        /// <summary>
        /// 生成花型弹幕（使用配置）
        /// </summary>
        public void SpawnFlowerPattern(Vector2 position, int petals, int bulletsPerPetal, float petalSpread, float startAngle, Team team, BulletConfig config = null)
        {
            if (petals <= 0 || bulletsPerPetal <= 0) return;

            float angleStep = 360f / petals;

            for (int petal = 0; petal < petals; petal++)
            {
                float petalCenterAngle = startAngle + angleStep * petal;
                float petalStartAngle = petalCenterAngle - petalSpread / 2f;
                float bulletAngleStep = bulletsPerPetal > 1 ? petalSpread / (bulletsPerPetal - 1) : 0;

                for (int bullet = 0; bullet < bulletsPerPetal; bullet++)
                {
                    float angle = petalStartAngle + bulletAngleStep * bullet;
                    Vector2 direction = new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad),
                        Mathf.Sin(angle * Mathf.Deg2Rad)
                    );

                    SpawnBullet(position, direction, team, config);
                }
            }
        }

        /// <summary>
        /// 回收弹幕
        /// </summary>
        public void ReturnBullet(Bullet bullet)
        {
            if (bullet == null) return;

            // 根据子弹的配置找到对应的对象池
            var config = bullet.Config;
            ObjectPool<Bullet> pool = null;

            if (config != null && _bulletPools.TryGetValue(config, out pool))
            {
                // 使用配置对应的对象池
                pool.Return(bullet);
            }
            else if (_defaultPool != null)
            {
                // 使用默认对象池
                _defaultPool.Return(bullet);
            }
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
