using UnityEngine;

namespace DodgeDots.Player
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class PlayerHitboxVisualizer : MonoBehaviour
    {
        [Header("Shape")]
        [SerializeField, Range(12, 128)] private int segments = 48;

        [Header("Visual")]
        [SerializeField] private float lineWidth = 0.02f;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private bool sortingLayerOverride = false;
        [SerializeField] private string sortingLayerName = "";
        [SerializeField] private int sortingOrderOffset = 1;
        [SerializeField] private float zOffset = 0f;

        private CircleCollider2D _collider;
        private LineRenderer _lineRenderer;
        private SpriteRenderer _spriteRenderer;
        private float _lastRadius = -1f;
        private Vector3 _lastScale = Vector3.zero;
        private Vector2 _lastOffset = Vector2.zero;

        private void Awake()
        {
            _collider = GetComponent<CircleCollider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            ConfigureLineRenderer();
            UpdateCircle(true);
        }

        private void OnEnable()
        {
            ConfigureLineRenderer();
            UpdateCircle(true);
        }

        private void LateUpdate()
        {
            if (_collider == null) return;

            if (!Mathf.Approximately(_collider.radius, _lastRadius) ||
                _collider.offset != _lastOffset ||
                transform.lossyScale != _lastScale)
            {
                UpdateCircle(false);
            }
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;

            if (_collider == null)
            {
                _collider = GetComponent<CircleCollider2D>();
            }

            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }

            ConfigureLineRenderer();
            UpdateCircle(true);
        }

        private void ConfigureLineRenderer()
        {
            if (_lineRenderer == null) return;

            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.alignment = LineAlignment.TransformZ;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.startColor = lineColor;
            _lineRenderer.endColor = lineColor;
            _lineRenderer.positionCount = Mathf.Max(segments, 12);

            if (sortingLayerOverride)
            {
                _lineRenderer.sortingLayerName = sortingLayerName;
                _lineRenderer.sortingOrder = sortingOrderOffset;
            }
            else if (_spriteRenderer != null)
            {
                _lineRenderer.sortingLayerName = _spriteRenderer.sortingLayerName;
                _lineRenderer.sortingOrder = _spriteRenderer.sortingOrder + sortingOrderOffset;
            }
        }

        private void UpdateCircle(bool force)
        {
            if (_lineRenderer == null || _collider == null) return;

            int count = Mathf.Max(segments, 12);
            if (_lineRenderer.positionCount != count)
            {
                _lineRenderer.positionCount = count;
            }

            float radius = Mathf.Max(0f, _collider.radius);
            Vector2 center = _collider.offset;
            float step = Mathf.PI * 2f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = step * i;
                float x = Mathf.Cos(angle) * radius + center.x;
                float y = Mathf.Sin(angle) * radius + center.y;
                _lineRenderer.SetPosition(i, new Vector3(x, y, zOffset));
            }

            _lastRadius = radius;
            _lastScale = transform.lossyScale;
            _lastOffset = center;
        }
    }
}
