using System.Collections.Generic;
using UnityEngine;
using DodgeDots.Core;

namespace DodgeDots.Bullet
{
    /// <summary>
    /// 基础弹幕类
    /// 支持配置系统和行为扩展
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Bullet : MonoBehaviour
    {
        [Header("弹幕配置")]
        [SerializeField] private BulletConfig config;

        [Header("运行时设置（可覆盖配置）")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float lifetime = 10f;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private CircleCollider2D _collider;
        private TrailRenderer _trailRenderer;
        private TrailRenderer _secondaryTrailRenderer;
        private Vector2 _direction;
        private float _afterimageTimer;
        private float _currentLifetime;
        private bool _isActive;
        private Team _team;
        private IBulletBehavior[] _behaviors;
        private int _pierceCount = 0; // 当前穿透次数
        private static Material _defaultTrailMaterial;
        private static readonly Dictionary<int, Material> _trailMaterialCache = new Dictionary<int, Material>();

        // 性能优化：缓存配置属性
        private bool _hasRotation;
        private bool _hasFaceDirection;
        private bool _hasAfterimage;
        private bool _hasBehaviors;
        private bool _hasLifetime;
        private bool _usePhysicsMovement;

        // 公共属性
        public float Damage => damage;
        public float Speed => speed;
        public Vector2 Direction => _direction;
        public bool IsActive => _isActive;
        public Team Team => _team;
        public BulletConfig Config => config;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0; // 弹幕不受重力影响
            _rb.isKinematic = true; // 使用运动学模式以减少物理计算

            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            _trailRenderer = GetComponent<TrailRenderer>();
            _secondaryTrailRenderer = GetSecondaryTrailRenderer();

            // 获取所有行为组件
            _behaviors = GetComponents<IBulletBehavior>();
            _hasBehaviors = _behaviors != null && _behaviors.Length > 0;

            // 默认使用Transform移动（性能更好）
            _usePhysicsMovement = false;

            // 如果有配置，应用配置
            if (config != null)
            {
                ApplyConfig(config);
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            // 更新生命周期
            if (_hasLifetime)
            {
                _currentLifetime -= Time.deltaTime;
                if (_currentLifetime <= 0)
                {
                    Deactivate();
                    return;
                }
            }

            // 调用所有行为的Update
            if (_hasBehaviors)
            {
                foreach (var behavior in _behaviors)
                {
                    behavior.OnUpdate();
                }
            }

            // 更新视觉效果（仅在需要时）
            if (_hasRotation || _hasFaceDirection)
            {
                UpdateVisuals();
            }

            // 残影效果
            if (_hasAfterimage)
            {
                UpdateAfterimage();
            }
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;

            // 移动弹幕 - 使用Transform移动以减少物理计算开销
            if (_usePhysicsMovement)
            {
                _rb.velocity = _direction * speed;
            }
            else
            {
                transform.position += (Vector3)(_direction * speed * Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// 初始化弹幕（使用配置）
        /// </summary>
        public void Initialize(Vector2 position, Vector2 direction, Team team, BulletConfig bulletConfig = null)
        {
            transform.position = position;
            _direction = direction.normalized;
            _team = team;

            // 应用配置
            if (bulletConfig != null)
            {
                ApplyConfig(bulletConfig);
            }
            else if (config != null)
            {
                ApplyConfig(config);
            }

            _currentLifetime = lifetime;
            _hasLifetime = lifetime > 0;
            _pierceCount = 0;
            _isActive = true;
            gameObject.SetActive(true);
            _afterimageTimer = 0f;

            // 初始化所有行为
            if (_hasBehaviors)
            {
                foreach (var behavior in _behaviors)
                {
                    behavior.Initialize(this);
                }
            }
        }

        /// <summary>
        /// 初始化弹幕（兼容旧版本，使用参数覆盖）
        /// </summary>
        public void Initialize(Vector2 position, Vector2 direction, Team team, float speed = -1, float damage = -1)
        {
            transform.position = position;
            _direction = direction.normalized;
            _team = team;

            if (speed > 0) this.speed = speed;
            if (damage > 0) this.damage = damage;

            _currentLifetime = lifetime;
            _hasLifetime = lifetime > 0;
            _pierceCount = 0;
            _isActive = true;
            gameObject.SetActive(true);
            _afterimageTimer = 0f;

            // 初始化所有行为
            if (_hasBehaviors)
            {
                foreach (var behavior in _behaviors)
                {
                    behavior.Initialize(this);
                }
            }
        }

        /// <summary>
        /// 停用弹幕（回收到对象池）
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            if (_usePhysicsMovement)
            {
                _rb.velocity = Vector2.zero;
            }
            _pierceCount = 0;
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }
            if (_secondaryTrailRenderer != null)
            {
                _secondaryTrailRenderer.Clear();
            }
            _afterimageTimer = 0f;

            // 重置所有行为
            if (_hasBehaviors)
            {
                foreach (var behavior in _behaviors)
                {
                    behavior.Reset();
                }
            }

            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;

            // 检查是否是边界
            if (other.CompareTag("Boundary"))
            {
                HandleBoundaryCollision(other);
                return;
            }

            // 阵营检测：避免误伤
            bool isEnemy = other.CompareTag("Boss"); // 暂时只检查Boss标签，避免Enemy标签未定义错误
            bool isPlayer = other.CompareTag("Player");

            // 玩家阵营的弹幕只能伤害敌人
            if (_team == Team.Player && !isEnemy) return;
            // 敌人阵营的弹幕只能伤害玩家
            if (_team == Team.Enemy && !isPlayer) return;

            // 检测碰撞目标是否可受伤
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.CanTakeDamage)
            {
                // 先让行为系统处理碰撞
                bool behaviorHandled = false;
                if (_hasBehaviors)
                {
                    foreach (var behavior in _behaviors)
                    {
                        if (behavior.OnTargetHit(other))
                        {
                            behaviorHandled = true;
                        }
                    }
                }

                // 造成伤害
                damageable.TakeDamage(damage, gameObject);

                // 如果行为系统处理了碰撞，不销毁子弹
                if (behaviorHandled) return;

                // 检查穿透
                if (config != null && config.isPiercing)
                {
                    _pierceCount++;
                    if (config.maxPierceCount > 0 && _pierceCount >= config.maxPierceCount)
                    {
                        Deactivate();
                    }
                }
                else
                {
                    Deactivate();
                }
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

        /// <summary>
        /// 应用配置到子弹
        /// </summary>
        private void ApplyConfig(BulletConfig bulletConfig)
        {
            if (bulletConfig == null) return;

            config = bulletConfig;

            // 应用基础属性
            speed = bulletConfig.defaultSpeed;
            damage = bulletConfig.defaultDamage;
            lifetime = bulletConfig.lifetime;

            // 缓存配置标志以优化Update性能
            _hasLifetime = lifetime > 0;
            _hasRotation = bulletConfig.rotationSpeed != 0;
            _hasFaceDirection = bulletConfig.faceDirection;
            _hasAfterimage = bulletConfig.enableAfterimage;

            // 应用视觉效果
            if (_spriteRenderer != null)
            {
                if (bulletConfig.sprite != null)
                    _spriteRenderer.sprite = bulletConfig.sprite;

                _spriteRenderer.color = bulletConfig.color;
                _spriteRenderer.sortingLayerName = bulletConfig.sortingLayer;
                _spriteRenderer.sortingOrder = bulletConfig.sortingOrder;
            }

            // 应用缩放
            transform.localScale = new Vector3(bulletConfig.scale.x, bulletConfig.scale.y, 1f);

            // 应用碰撞器大小
            if (_collider != null)
            {
                _collider.radius = bulletConfig.colliderRadius;
            }

            // 应用拖尾效果
            if (bulletConfig.enableTrail)
            {
                if (_trailRenderer == null)
                {
                    _trailRenderer = GetComponent<TrailRenderer>();
                    if (_trailRenderer == null)
                    {
                        _trailRenderer = gameObject.AddComponent<TrailRenderer>();
                    }
                }

                ConfigureTrail(_trailRenderer, bulletConfig, false);

                if (bulletConfig.enableSecondTrail)
                {
                    if (_secondaryTrailRenderer == null)
                    {
                        _secondaryTrailRenderer = gameObject.AddComponent<TrailRenderer>();
                    }
                    ConfigureTrail(_secondaryTrailRenderer, bulletConfig, true);
                }
                else if (_secondaryTrailRenderer != null)
                {
                    _secondaryTrailRenderer.Clear();
                    _secondaryTrailRenderer.enabled = false;
                }
            }
            else if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.enabled = false;
                if (_secondaryTrailRenderer != null)
                {
                    _secondaryTrailRenderer.Clear();
                    _secondaryTrailRenderer.enabled = false;
                }
            }
        }

        private void ConfigureTrail(TrailRenderer trailRenderer, BulletConfig bulletConfig, bool isSecondary)
        {
            if (trailRenderer == null) return;

            trailRenderer.enabled = true;
            trailRenderer.time = isSecondary ? bulletConfig.secondTrailTime : bulletConfig.trailTime;
            trailRenderer.startWidth = isSecondary ? bulletConfig.secondTrailStartWidth : bulletConfig.trailStartWidth;
            trailRenderer.endWidth = isSecondary ? bulletConfig.secondTrailEndWidth : bulletConfig.trailEndWidth;
            trailRenderer.minVertexDistance = isSecondary ? bulletConfig.secondTrailMinVertexDistance : bulletConfig.trailMinVertexDistance;
            trailRenderer.textureMode = LineTextureMode.DistributePerSegment;

            Color startColor;
            Color endColor;
            if (bulletConfig.trailUseBulletColor)
            {
                Color baseColor = bulletConfig.color;
                float lighten = isSecondary ? bulletConfig.secondTrailColorLighten : bulletConfig.trailColorLighten;
                Color lightColor = Color.Lerp(baseColor, Color.white, Mathf.Clamp01(lighten));
                float startAlpha = isSecondary ? bulletConfig.secondTrailStartAlpha : bulletConfig.trailStartAlpha;
                float endAlpha = isSecondary ? bulletConfig.secondTrailEndAlpha : bulletConfig.trailEndAlpha;
                startColor = new Color(lightColor.r, lightColor.g, lightColor.b, startAlpha);
                endColor = new Color(lightColor.r, lightColor.g, lightColor.b, endAlpha);
            }
            else
            {
                startColor = bulletConfig.trailStartColor;
                endColor = bulletConfig.trailEndColor;
            }

            trailRenderer.startColor = startColor;
            trailRenderer.endColor = endColor;

            Material trailMaterial = GetTrailMaterial(bulletConfig, isSecondary);
            if (trailMaterial != null)
            {
                trailRenderer.sharedMaterial = trailMaterial;
            }

            trailRenderer.sortingLayerName = bulletConfig.sortingLayer;
            trailRenderer.sortingOrder = bulletConfig.sortingOrder;
            trailRenderer.Clear();
        }

        private Material GetTrailMaterial(BulletConfig bulletConfig, bool isSecondary)
        {
            Material baseMaterial = isSecondary ? (bulletConfig.secondTrailMaterial ?? bulletConfig.trailMaterial) : bulletConfig.trailMaterial;
            if (baseMaterial == null)
            {
                baseMaterial = GetDefaultTrailMaterial();
            }

            if (baseMaterial == null) return null;
            if (!bulletConfig.trailUseBulletSprite || bulletConfig.sprite == null) return baseMaterial;

            Texture texture = bulletConfig.sprite.texture;
            if (texture == null) return baseMaterial;

            int key = (baseMaterial.GetInstanceID() * 397) ^ texture.GetInstanceID();
            if (_trailMaterialCache.TryGetValue(key, out var cachedMaterial))
            {
                return cachedMaterial;
            }

            var material = new Material(baseMaterial);
            material.name = $"{baseMaterial.name}_Trail_{texture.name}";
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            _trailMaterialCache[key] = material;
            return material;
        }

        private static Material GetDefaultTrailMaterial()
        {
            if (_defaultTrailMaterial != null) return _defaultTrailMaterial;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Particles/Standard Unlit");

            if (shader == null)
            {
                Debug.LogWarning("Bullet: 未找到可用的拖尾Shader，拖尾可能显示为紫色。");
                return null;
            }

            _defaultTrailMaterial = new Material(shader);
            _defaultTrailMaterial.name = "DefaultTrailMaterial_Runtime";
            return _defaultTrailMaterial;
        }

        private TrailRenderer GetSecondaryTrailRenderer()
        {
            var trails = GetComponents<TrailRenderer>();
            if (trails == null || trails.Length < 2) return null;
            for (int i = 0; i < trails.Length; i++)
            {
                if (trails[i] != _trailRenderer)
                {
                    return trails[i];
                }
            }
            return null;
        }

        private void UpdateAfterimage()
        {
            _afterimageTimer += Time.deltaTime;
            if (_afterimageTimer < config.afterimageInterval) return;
            _afterimageTimer = 0f;

            Sprite sprite = config.sprite != null ? config.sprite : (_spriteRenderer != null ? _spriteRenderer.sprite : null);
            if (sprite == null) return;

            Color baseColor = config.color;
            Color lightColor = Color.Lerp(baseColor, Color.white, config.afterimageColorLighten);
            Color startColor = new Color(lightColor.r, lightColor.g, lightColor.b, config.afterimageStartAlpha);
            Color endColor = new Color(lightColor.r, lightColor.g, lightColor.b, config.afterimageEndAlpha);

            var afterimage = BulletAfterimage.Get(transform.parent);
            afterimage.Play(
                sprite,
                transform.position,
                transform.rotation,
                transform.localScale,
                startColor,
                endColor,
                config.afterimageLifetime,
                config.sortingLayer,
                config.sortingOrder
            );
        }

        /// <summary>
        /// 更新视觉效果
        /// </summary>
        private void UpdateVisuals()
        {
            // 旋转效果
            if (_hasRotation)
            {
                transform.Rotate(0, 0, config.rotationSpeed * Time.deltaTime);
            }

            // 朝向移动方向
            if (_hasFaceDirection && _direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        /// <summary>
        /// 处理边界碰撞
        /// </summary>
        private void HandleBoundaryCollision(Collider2D boundary)
        {
            // 让行为系统处理边界碰撞
            bool behaviorHandled = false;
            if (_hasBehaviors)
            {
                // 计算边界法线
                Vector2 normal = CalculateBoundaryNormal(boundary);

                foreach (var behavior in _behaviors)
                {
                    if (behavior.OnBoundaryHit(normal))
                    {
                        behaviorHandled = true;
                    }
                }
            }

            // 如果行为系统没有处理，默认销毁子弹
            if (!behaviorHandled)
            {
                Deactivate();
            }
        }

        /// <summary>
        /// 计算边界法线
        /// </summary>
        private Vector2 CalculateBoundaryNormal(Collider2D boundary)
        {
            // ?? Physics2D.Distance ?????????????????
            if (_collider != null)
            {
                var dist = Physics2D.Distance(_collider, boundary);
                if (dist.isOverlapped && dist.normal != Vector2.zero)
                {
                    Vector2 normal = dist.normal.normalized;
                    // ?????????????????????
                    if (Vector2.Dot(normal, _direction) > 0f)
                    {
                        normal = -normal;
                    }
                    return normal;
                }
            }

            // fallback????????????
            Vector2 bulletPos = transform.position;
            Vector2 closest = boundary.ClosestPoint(bulletPos);
            Vector2 fallbackNormal = (bulletPos - closest);
            if (fallbackNormal.sqrMagnitude < 0.0001f)
            {
                fallbackNormal = -_direction;
            }
            else
            {
                fallbackNormal = fallbackNormal.normalized;
            }

            if (Vector2.Dot(fallbackNormal, _direction) > 0f)
            {
                fallbackNormal = -fallbackNormal;
            }

            return fallbackNormal;
        }
    }
}
