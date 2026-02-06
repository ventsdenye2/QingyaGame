using UnityEngine;

namespace DodgeDots.Bullet
{
    /// <summary>
    /// 追踪弹幕行为：持续转向目标
    /// </summary>
    public class HomingBulletBehavior : MonoBehaviour, IBulletBehavior
    {
        [SerializeField] private float turnSpeed = 360f; // 度/秒

        private Bullet _bullet;
        private Transform _target;
        private bool _isActive;

        public void Configure(Transform target, float newTurnSpeed)
        {
            _target = target;
            if (newTurnSpeed > 0f)
            {
                turnSpeed = newTurnSpeed;
            }
            _isActive = _target != null;
        }

        public void Initialize(Bullet bullet)
        {
            _bullet = bullet;
        }

        public void OnUpdate()
        {
            if (!_isActive || _bullet == null || _target == null) return;

            Vector2 currentDir = _bullet.Direction;
            Vector2 desiredDir = ((Vector2)_target.position - (Vector2)_bullet.transform.position).normalized;

            if (desiredDir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (currentDir.sqrMagnitude < 0.0001f)
            {
                _bullet.SetDirection(desiredDir);
                return;
            }

            float maxRadians = turnSpeed * Mathf.Deg2Rad * Time.deltaTime;
            Vector2 newDir = Vector3.RotateTowards(currentDir, desiredDir, maxRadians, 0f);
            _bullet.SetDirection(newDir);
        }

        public bool OnBoundaryHit(Vector2 normal)
        {
            return false;
        }

        public bool OnTargetHit(Collider2D target)
        {
            return false;
        }

        public void Reset()
        {
            _isActive = false;
            _target = null;
        }
    }
}
