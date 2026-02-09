using System.Collections.Generic;
using UnityEngine;

namespace DodgeDots.Bullet
{
    public class BulletAfterimage : MonoBehaviour
    {
        private static readonly Stack<BulletAfterimage> Pool = new Stack<BulletAfterimage>(64);

        private SpriteRenderer _spriteRenderer;
        private float _lifetime;
        private float _elapsed;
        private Color _startColor;
        private Color _endColor;

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
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                {
                    _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = scale;
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.sortingLayerName = sortingLayer;
            _spriteRenderer.sortingOrder = sortingOrder - 1;

            _startColor = startColor;
            _endColor = endColor;
            _lifetime = Mathf.Max(0.01f, lifetime);
            _elapsed = 0f;
            _spriteRenderer.color = _startColor;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifetime);
            _spriteRenderer.color = Color.Lerp(_startColor, _endColor, t);
            if (_elapsed >= _lifetime)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            gameObject.SetActive(false);
            Pool.Push(this);
        }
    }
}
