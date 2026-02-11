using UnityEngine;
using DodgeDots.Core;
using System.Collections.Generic;
using DodgeDots.Enemy;

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
        [SerializeField] private int initialPoolSize = 200;
        [SerializeField] private int maxPoolSize = 2000;

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
        public void SpawnCirclePattern(Vector2 position, int bulletCount, Team team, BulletConfig config = null)
        {
            if (bulletCount <= 0) return;

            float startAngle = Random.Range(0f, 360f);
            float angleStep = 360f / bulletCount;
            float angleRad = startAngle * Mathf.Deg2Rad;
            float angleStepRad = angleStep * Mathf.Deg2Rad;

            for (int i = 0; i < bulletCount; i++)
            {
                // 使用弧度计算以减少转换开销
                Vector2 direction = new Vector2(
                    Mathf.Cos(angleRad),
                    Mathf.Sin(angleRad)
                );

                SpawnBullet(position, direction, team, config);
                angleRad += angleStepRad;
            }
        }

        /// <summary>
        /// 生成圆形弹幕（兼容旧版本）
        /// </summary>
        public void SpawnCirclePattern(Vector2 position, int bulletCount, Team team, float speed = -1, float damage = -1)
        {
            if (bulletCount <= 0) return;

            float startAngle = Random.Range(0f, 360f);
            float angleStep = 360f / bulletCount;
            float angleRad = startAngle * Mathf.Deg2Rad;
            float angleStepRad = angleStep * Mathf.Deg2Rad;

            for (int i = 0; i < bulletCount; i++)
            {
                Vector2 direction = new Vector2(
                    Mathf.Cos(angleRad),
                    Mathf.Sin(angleRad)
                );

                SpawnBullet(position, direction, team, speed, damage);
                angleRad += angleStepRad;
            }
        }

        /// <summary>
        /// 生成扇形弹幕（使用配置）
        /// </summary>
        public void SpawnFanPattern(Vector2 position, Vector2 centerDirection, int bulletCount, float spreadAngle, Team team, BulletConfig config = null)
        {
            if (bulletCount <= 0) return;

            float centerAngle = Mathf.Atan2(centerDirection.y, centerDirection.x);
            float spreadAngleRad = spreadAngle * Mathf.Deg2Rad;
            float startAngleRad = centerAngle - spreadAngleRad * 0.5f;
            float angleStepRad = bulletCount > 1 ? spreadAngleRad / (bulletCount - 1) : 0;

            for (int i = 0; i < bulletCount; i++)
            {
                float angleRad = startAngleRad + angleStepRad * i;
                Vector2 direction = new Vector2(
                    Mathf.Cos(angleRad),
                    Mathf.Sin(angleRad)
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

            float centerAngle = Mathf.Atan2(centerDirection.y, centerDirection.x);
            float spreadAngleRad = spreadAngle * Mathf.Deg2Rad;
            float startAngleRad = centerAngle - spreadAngleRad * 0.5f;
            float angleStepRad = bulletCount > 1 ? spreadAngleRad / (bulletCount - 1) : 0;

            for (int i = 0; i < bulletCount; i++)
            {
                float angleRad = startAngleRad + angleStepRad * i;
                Vector2 direction = new Vector2(
                    Mathf.Cos(angleRad),
                    Mathf.Sin(angleRad)
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
            float startAngleRad = startAngle * Mathf.Deg2Rad;
            float angleStepRad = angleStep * Mathf.Deg2Rad;

            for (int i = 0; i < bulletCount; i++)
            {
                float angleRad = startAngleRad + angleStepRad * i;
                float radius = radiusGrowth * i;
                float cosAngle = Mathf.Cos(angleRad);
                float sinAngle = Mathf.Sin(angleRad);

                Vector2 offset = new Vector2(cosAngle * radius, sinAngle * radius);
                Vector2 direction = new Vector2(cosAngle, sinAngle);

                SpawnBullet(position + offset, direction, team, config);
            }
        }

        /// <summary>
        /// 生成花型弹幕（使用配置）
        /// </summary>
        public void SpawnFlowerPattern(Vector2 position, int petals, int bulletsPerPetal, float petalSpread, Team team, BulletConfig config = null)
        {
            if (petals <= 0 || bulletsPerPetal <= 0) return;

            float startAngle = Random.Range(0f, 360f);
            float angleStep = 360f / petals;
            float angleStepRad = angleStep * Mathf.Deg2Rad;
            float startAngleRad = startAngle * Mathf.Deg2Rad;
            float petalSpreadRad = petalSpread * Mathf.Deg2Rad;
            float bulletAngleStepRad = bulletsPerPetal > 1 ? petalSpreadRad / (bulletsPerPetal - 1) : 0;

            for (int petal = 0; petal < petals; petal++)
            {
                float petalCenterAngleRad = startAngleRad + angleStepRad * petal;
                float petalStartAngleRad = petalCenterAngleRad - petalSpreadRad * 0.5f;

                for (int bullet = 0; bullet < bulletsPerPetal; bullet++)
                {
                    float angleRad = petalStartAngleRad + bulletAngleStepRad * bullet;
                    Vector2 direction = new Vector2(
                        Mathf.Cos(angleRad),
                        Mathf.Sin(angleRad)
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
        /// 预加载指定配置的弹幕到对象池
        /// </summary>
        public void PreloadBullets(BulletConfig config, int count)
        {
            if (config == null || count <= 0) return;

            var pool = GetOrCreatePool(config);
            if (pool == null) return;

            // 预创建并立即回收，填充对象池
            for (int i = 0; i < count; i++)
            {
                var bullet = pool.Get();
                bullet.gameObject.SetActive(false);
                pool.Return(bullet);
            }

            Debug.Log($"预加载了 {count} 个 {config.bulletName} 弹幕");
        }

        /// <summary>
        /// 预热所有已注册的弹幕类型
        /// </summary>
        public void WarmUpAllPools(int bulletsPerType = 50)
        {
            // 预热默认池
            if (_defaultPool != null)
            {
                for (int i = 0; i < bulletsPerType; i++)
                {
                    var bullet = _defaultPool.Get();
                    bullet.gameObject.SetActive(false);
                    _defaultPool.Return(bullet);
                }
                Debug.Log($"预热默认弹幕池: {bulletsPerType} 个弹幕");
            }

            // 预热所有已注册的配置池
            foreach (var kvp in _bulletPools)
            {
                var config = kvp.Key;
                var pool = kvp.Value;

                for (int i = 0; i < bulletsPerType; i++)
                {
                    var bullet = pool.Get();
                    bullet.gameObject.SetActive(false);
                    pool.Return(bullet);
                }
                Debug.Log($"预热弹幕池 {config.bulletName}: {bulletsPerType} 个弹幕");
            }
        }

        /// <summary>
        /// 根据序列配置预加载前N秒的弹幕
        /// </summary>
        public void PreloadForSequence(BossSequenceConfig sequenceConfig, float seconds, float bpm)
        {
            if (sequenceConfig == null || sequenceConfig.attackSequence == null) return;

            // 计算前N秒有多少个节拍
            float beatsPerSecond = bpm / 60f;
            int totalBeats = Mathf.CeilToInt(seconds * beatsPerSecond);

            // 统计每种弹幕配置需要的数量
            Dictionary<BulletConfig, int> bulletCounts = new Dictionary<BulletConfig, int>();

            for (int i = 0; i < totalBeats && i < sequenceConfig.attackSequence.Length; i++)
            {
                var attack = sequenceConfig.attackSequence[i];
                if (attack.bulletConfig == null) continue;

                int bulletCount = EstimateBulletCount(attack);

                if (bulletCounts.ContainsKey(attack.bulletConfig))
                {
                    bulletCounts[attack.bulletConfig] += bulletCount;
                }
                else
                {
                    bulletCounts[attack.bulletConfig] = bulletCount;
                }
            }

            // 预加载每种弹幕
            foreach (var kvp in bulletCounts)
            {
                PreloadBullets(kvp.Key, kvp.Value);
            }

            Debug.Log($"预加载完成：前 {seconds} 秒（{totalBeats} 个节拍）的弹幕");
        }

        /// <summary>
        /// 估算单次攻击的弹幕数量
        /// </summary>
        private int EstimateBulletCount(BossAttackAction attack)
        {
            int multiplier = attack.useMultipleEmitters && attack.multipleEmitters != null
                ? attack.multipleEmitters.Length
                : 1;

            switch (attack.attackType)
            {
                case BossAttackType.Circle:
                    return attack.circleCount * multiplier;

                case BossAttackType.Fan:
                    return attack.fanCount * multiplier;

                case BossAttackType.Single:
                    return 1 * multiplier;

                case BossAttackType.Spiral:
                    return attack.spiralBulletCount * multiplier;

                case BossAttackType.Flower:
                    return attack.flowerPetals * attack.flowerBulletsPerPetal * multiplier;

                case BossAttackType.Aiming:
                    return attack.aimingBulletCount * multiplier;

                case BossAttackType.Character:
                    if (attack.characterPattern != null && attack.characterPattern.bulletPositions != null)
                    {
                        return attack.characterPattern.bulletPositions.Length * multiplier;
                    }
                    return 0;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// 清空所有弹幕
        /// </summary>
        public void ClearAllBullets()
        {
            if (_bulletContainer == null) return;

            // 使用数组缓存以避免在迭代时修改集合
            int childCount = _bulletContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = _bulletContainer.GetChild(i);
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
