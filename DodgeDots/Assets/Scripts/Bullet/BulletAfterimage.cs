using System.Collections.Generic;
using UnityEngine;

namespace DodgeDots.Bullet
{
    public class BulletAfterimage : MonoBehaviour
    {
        private static readonly Stack<BulletAfterimage> Pool = new Stack<BulletAfterimage>(256);
        private static readonly int MaxPoolSize = 1024;

        private SpriteRenderer _spriteRenderer;
        private float _lifetime;
        private float _elapsed;
        private Color _startColor;
        private Color _endColor;
        private float _invLifetime; // 缓存1/lifetime以避免除法运算

        public static BulletAfterimage Get(Transform parent)
        {
            BulletAfterimage instance;
            if (Pool.Count > 0)
            {
                instance = Pool.Pop();
            }
            else
            {
                var go = new GameObject("BulletAfterimage");
                instance = go.AddComponent<BulletAfterimage>();
                instance._spriteRenderer = go.AddComponent<SpriteRenderer>();
            }

            instance.gameObject.SetActive(true);
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }
            return instance;
        }

        public void Play(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale, Color startColor, Color endColor, float lifetime, string sortingLayer, int sortingOrder)
        {
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = scale;
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.sortingLayerName = sortingLayer;
            _spriteRenderer.sortingOrder = sortingOrder - 1;

            _startColor = startColor;
            _endColor = endColor;
            _lifetime = Mathf.Max(0.01f, lifetime);
            _invLifetime = 1f / _lifetime; // 预计算倒数
            _elapsed = 0f;
            _spriteRenderer.color = _startColor;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // 使用预计算的倒数避免除法
            float t = _elapsed * _invLifetime;

            if (t >= 1f)
            {
                ReturnToPool();
            }
            else
            {
                // 使用LerpUnclamped避免额外的Clamp操作
                _spriteRenderer.color = Color.LerpUnclamped(_startColor, _endColor, t);
            }
        }

        private void ReturnToPool()
        {
            gameObject.SetActive(false);

            // 限制对象池大小以避免内存泄漏
            if (Pool.Count < MaxPoolSize)
            {
                Pool.Push(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
