using UnityEngine;

namespace DodgeDots.Bullet
{
    /// <summary>
    /// 子弹反弹行为
    /// 碰到边界时会反弹而不是消失
    /// </summary>
    public class BulletBounceBehavior : MonoBehaviour, IBulletBehavior
    {
        [Header("反弹设置")]
        [Tooltip("最大反弹次数（0表示无限）")]
        [SerializeField] private int maxBounces = 3;

        [Tooltip("每次反弹后的速度衰减系数")]
        [SerializeField] private float speedDecay = 0.9f;

        [Tooltip("是否在反弹时改变颜色")]
        [SerializeField] private bool changeColorOnBounce = true;

        [Tooltip("反弹颜色渐变")]
        [SerializeField] private Gradient bounceColorGradient;

        private Bullet _bullet;
        private int _bounceCount = 0;
        private SpriteRenderer _spriteRenderer;

        public void Initialize(Bullet bullet)
        {
            _bullet = bullet;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _bounceCount = 0;

            // 初始化默认颜色渐变
            if (bounceColorGradient == null)
            {
                bounceColorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[3];
                colorKeys[0] = new GradientColorKey(Color.white, 0f);
                colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
                colorKeys[2] = new GradientColorKey(Color.red, 1f);

                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);

                bounceColorGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        public void OnUpdate()
        {
            // 反弹行为不需要每帧更新
        }

        public bool OnBoundaryHit(Vector2 normal)
        {
            // 检查是否还能反弹
            if (maxBounces > 0 && _bounceCount >= maxBounces)
            {
                return false; // 达到最大反弹次数，销毁子弹
            }

            // 反弹
            _bounceCount++;

            // 计算反弹方向
            Vector2 currentDirection = _bullet.Direction;
            Vector2 reflectedDirection = Vector2.Reflect(currentDirection, normal);
            _bullet.SetDirection(reflectedDirection);

            // 关键修复：将子弹位置推回到边界内部
            // 沿着法线方向向内推一小段距离，防止子弹穿过边界
            float pushDistance = 0.1f; // 推回距离
            Vector2 currentPos = _bullet.transform.position;
            Vector2 newPos = currentPos + normal * pushDistance;
            _bullet.transform.position = newPos;

            // 速度衰减
            if (speedDecay < 1f)
            {
                float newSpeed = _bullet.Speed * speedDecay;
                _bullet.SetSpeed(newSpeed);
            }

            // 改变颜色
            if (changeColorOnBounce && _spriteRenderer != null && maxBounces > 0)
            {
                float t = (float)_bounceCount / maxBounces;
                _spriteRenderer.color = bounceColorGradient.Evaluate(t);
            }

            Debug.Log($"子弹反弹！第 {_bounceCount} 次反弹，法线: {normal}");
            return true; // 处理了碰撞，不销毁子弹
        }

        public bool OnTargetHit(Collider2D target)
        {
            // 反弹子弹碰到目标时正常销毁
            return false;
        }

        public void Reset()
        {
            _bounceCount = 0;
            if (_spriteRenderer != null && bounceColorGradient != null)
            {
                _spriteRenderer.color = bounceColorGradient.Evaluate(0f);
            }
        }
    }
}
