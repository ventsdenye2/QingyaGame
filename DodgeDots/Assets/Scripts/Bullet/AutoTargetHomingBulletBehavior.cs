using UnityEngine;
using DodgeDots.Core;

namespace DodgeDots.Bullet
{
    /// <summary>
    /// 自动追踪子弹行为：
    /// - 配合 BulletConfig 中的“自动追踪设置”使用
    /// - 勾选对应开关后，才会追踪指定 Tag 的目标
    /// - 玩家阵营子弹一般追踪 Boss / Enemy，敌人阵营子弹一般追踪 Player
    /// </summary>
    public class AutoTargetHomingBulletBehavior : MonoBehaviour, IBulletBehavior
    {
        [Header("追踪参数（可被 BulletConfig 覆盖）")]
        [Tooltip("最大转向速度（度/秒），值越大转弯越快、越“黏”目标")]
        [SerializeField] private float turnSpeed = 360f;

        [Tooltip("搜索目标的半径（世界单位），设大一点可以更容易锁定敌人")]
        [SerializeField] private float searchRadius = 20f;

        [Tooltip("重新锁定目标的时间间隔（秒），避免每帧都全图搜索造成开销")]
        [SerializeField] private float retargetInterval = 0.2f;

        [Header("是否追踪对应标签的目标")]
        [Tooltip("是否可以追踪 Boss（需要场景中物体 Tag 为 Boss）")]
        [SerializeField] private bool trackBoss = true;

        [Tooltip("是否可以追踪普通敌人（需要场景中物体 Tag 为 Enemy）")]
        [SerializeField] private bool trackEnemy = false;

        [Tooltip("是否可以追踪玩家（需要场景中物体 Tag 为 Player）")]
        [SerializeField] private bool trackPlayer = false;

        [Header("标签设置")]
        [Tooltip("Boss 的 Tag")]
        [SerializeField] private string bossTag = "Boss";

        [Tooltip("普通敌人的 Tag（如果你以后加 Enemy 敌人，可以复用）")]
        [SerializeField] private string enemyTag = "Enemy";

        [Tooltip("玩家的 Tag")]
        [SerializeField] private string playerTag = "Player";

        private Bullet _bullet;
        private Transform _target;
        private float _retargetTimer;
        private bool _isActive;

        public void Initialize(Bullet bullet)
        {
            _bullet = bullet;
            _isActive = bullet != null && bullet.IsActive;
            _retargetTimer = 0f;

            // 根据子弹配置覆盖本地参数（是否启用追踪、追踪哪些目标等）
            ApplyConfig();

            // 初始化时先尝试锁定一次目标
            AcquireTarget(force: true);
        }

        public void OnUpdate()
        {
            if (!_isActive || _bullet == null || !_bullet.IsActive)
                return;

            // 定时尝试重新锁定 / 刷新目标（目标死亡或超出范围时）
            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f)
            {
                AcquireTarget(force: false);
                _retargetTimer = retargetInterval;
            }

            if (_target == null)
                return;

            Vector2 currentDir = _bullet.Direction;
            Vector2 desiredDir = ((Vector2)_target.position - (Vector2)_bullet.transform.position).normalized;

            if (desiredDir.sqrMagnitude < 0.0001f)
                return;

            if (currentDir.sqrMagnitude < 0.0001f)
            {
                // 初次锁定：直接朝向目标
                _bullet.SetDirection(desiredDir);
                return;
            }

            // 按最大转向角度缓慢转向目标，实现“拐弯追踪”效果
            float maxRadians = turnSpeed * Mathf.Deg2Rad * Time.deltaTime;
            Vector2 newDir = Vector3.RotateTowards(currentDir, desiredDir, maxRadians, 0f);
            _bullet.SetDirection(newDir);
        }

        public bool OnBoundaryHit(Vector2 normal)
        {
            // 不处理边界碰撞，交给 Bullet 默认逻辑即可
            return false;
        }

        public bool OnTargetHit(Collider2D target)
        {
            // 命中目标后，这个行为不拦截销毁逻辑
            return false;
        }

        public void Reset()
        {
            _isActive = false;
            _target = null;
            _retargetTimer = 0f;
        }

        /// <summary>
        /// 根据子弹阵营和“是否追踪”开关自动选择要追踪的目标。
        /// 只有当 BulletConfig.enableAutoHoming 为 true 且对应 track* 开关勾选时才会生效。
        /// </summary>
        private void AcquireTarget(bool force)
        {
            if (_bullet == null)
                return;

            // 如果已有有效目标，且不是强制重新获取，则直接使用原目标
            if (!force && _target != null)
            {
                // 简单判断目标是否还在可追踪范围内
                float sqrDist = ((Vector2)_target.position - (Vector2)_bullet.transform.position).sqrMagnitude;
                if (sqrDist <= searchRadius * searchRadius)
                    return;
            }

            // 根据阵营和开关构造要搜索的 Tag 列表
            // 玩家子弹：通常追踪 Boss / Enemy
            if (_bullet.Team == Team.Player)
            {
                // 没有勾选任何追踪目标时，直接不追踪
                if (!trackBoss && !trackEnemy)
                {
                    _target = null;
                    return;
                }

                // 根据开关决定追踪哪些标签
                if (trackBoss && trackEnemy)
                {
                    _target = FindNearestWithTags(_bullet.transform.position, searchRadius, bossTag, enemyTag);
                }
                else if (trackBoss)
                {
                    _target = FindNearestWithTags(_bullet.transform.position, searchRadius, bossTag);
                }
                else // 只追踪 Enemy
                {
                    _target = FindNearestWithTags(_bullet.transform.position, searchRadius, enemyTag);
                }
            }
            // 敌人子弹：通常追踪 Player
            else if (_bullet.Team == Team.Enemy)
            {
                if (!trackPlayer)
                {
                    _target = null;
                    return;
                }

                _target = FindNearestWithTags(_bullet.transform.position, searchRadius, playerTag);
            }
        }

        /// <summary>
        /// 从 Bullet.Config 中读取自动追踪相关参数，覆盖当前行为上的配置。
        /// 只有当 config.enableAutoHoming == true 时才会开启追踪逻辑。
        /// </summary>
        private void ApplyConfig()
        {
            if (_bullet == null)
            {
                _isActive = false;
                return;
            }

            var config = _bullet.Config;
            if (config == null)
            {
                // 没有配置则保留 Inspector 上的默认参数，但不强制关闭
                return;
            }

            if (!config.enableAutoHoming)
            {
                // 配置里没勾选“启用自动追踪”，则整个行为关闭
                _isActive = false;
                _target = null;
                return;
            }

            // 使用配置覆盖参数
            turnSpeed = config.homingTurnSpeed;
            searchRadius = config.homingSearchRadius;
            retargetInterval = config.homingRetargetInterval;
            trackBoss = config.homingTrackBoss;
            trackEnemy = config.homingTrackEnemy;
            trackPlayer = config.homingTrackPlayer;

            _isActive = true;
        }

        /// <summary>
        /// 在一定半径内查找最近、且匹配任意给定 Tag 的目标
        /// </summary>
        private Transform FindNearestWithTags(Vector2 origin, float radius, params string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return null;

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius);
            Transform best = null;
            float bestSqrDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit == null || hit.attachedRigidbody == null)
                    continue;

                string hitTag = hit.tag;
                bool tagMatched = false;
                for (int i = 0; i < tags.Length; i++)
                {
                    if (!string.IsNullOrEmpty(tags[i]) && hitTag == tags[i])
                    {
                        tagMatched = true;
                        break;
                    }
                }

                if (!tagMatched)
                    continue;

                float sqrDist = ((Vector2)hit.transform.position - origin).sqrMagnitude;
                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    best = hit.transform;
                }
            }

            return best;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 在编辑器里画出搜索范围，方便调试
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
#endif
    }
}


