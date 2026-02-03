using System;
using System.Collections;
using UnityEngine;

namespace DodgeDots.Player
{
    /// <summary>
    /// 玩家技能系统
    /// 处理技能的释放、持续时间和伤害检测
    /// </summary>
    public class PlayerSkillSystem : MonoBehaviour
    {
        [Header("技能设置")]
        [SerializeField] private float skillDuration = 3f;             // 技能持续时间（秒）
        [SerializeField] private float skillDamage = 30f;              // 技能对Boss的伤害
        [SerializeField] private float damageCheckInterval = 0.1f;     // 每隔多久检查一次碰撞（秒）
        [SerializeField] private float skillActivationRadius = 1f;     // 技能激活范围（与Boss碰撞范围）

        [Header("视觉效果")]
        [SerializeField] private SpriteRenderer spriteRenderer;        // 用于显示技能激活的视觉效果
        [SerializeField] private Color skillActiveColor = Color.yellow;
        [SerializeField] private Color skillInactiveColor = Color.white;

        private PlayerEnergy _playerEnergy;
        private Rigidbody2D _rigidbody;
        private CircleCollider2D _collider;
        private bool _isSkillActive = false;
        private Coroutine _skillCoroutine;
        private GameObject _bossGameObject;

        public bool IsSkillActive => _isSkillActive;

        public event Action OnSkillStarted;
        public event Action OnSkillEnded;

        private void Start()
        {
            _playerEnergy = GetComponent<PlayerEnergy>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            // 初始化UI颜色
            if (spriteRenderer != null)
            {
                spriteRenderer.color = skillInactiveColor;
            }
        }

        private void Update()
        {
            // 监听鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                TryActivateSkill();
            }
        }

        /// <summary>
        /// 尝试激活技能
        /// </summary>
        private void TryActivateSkill()
        {
            if (_isSkillActive)
            {
                return; // 技能正在进行中，无法再次激活
            }

            if (!_playerEnergy.TryConsumeEnergy(_playerEnergy.MaxEnergy))
            {
                Debug.Log("能量不足，无法释放技能！");
                return;
            }

            ActivateSkill();
        }

        /// <summary>
        /// 激活技能
        /// </summary>
        private void ActivateSkill()
        {
            if (_skillCoroutine != null)
            {
                StopCoroutine(_skillCoroutine);
            }

            _skillCoroutine = StartCoroutine(SkillActiveCoroutine());
        }

        /// <summary>
        /// 技能激活协程
        /// </summary>
        private IEnumerator SkillActiveCoroutine()
        {
            _isSkillActive = true;
            OnSkillStarted?.Invoke();

            // 改变视觉效果
            if (spriteRenderer != null)
            {
                spriteRenderer.color = skillActiveColor;
            }

            float elapsedTime = 0f;

            while (elapsedTime < skillDuration)
            {
                elapsedTime += Time.deltaTime;

                // 每隔damageCheckInterval秒检查一次与Boss的碰撞
                if (Mathf.FloorToInt(elapsedTime / damageCheckInterval) !=
                    Mathf.FloorToInt((elapsedTime - Time.deltaTime) / damageCheckInterval))
                {
                    CheckCollisionWithBoss();
                }

                yield return null;
            }

            _isSkillActive = false;
            OnSkillEnded?.Invoke();

            // 恢复视觉效果
            if (spriteRenderer != null)
            {
                spriteRenderer.color = skillInactiveColor;
            }
        }

        /// <summary>
        /// 检查与Boss的碰撞
        /// </summary>
        private void CheckCollisionWithBoss()
        {
            // 使用物理射线或圆形碰撞体检测
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, skillActivationRadius);

            foreach (Collider2D col in colliders)
            {
                // 检查是否是Boss
                Enemy.BossBase boss = col.GetComponent<Enemy.BossBase>();
                if (boss != null)
                {
                    // 对Boss造成伤害
                    boss.TakeDamage(skillDamage, gameObject);
                    Debug.Log($"技能命中Boss！造成 {skillDamage} 点伤害");
                }
            }
        }

        /// <summary>
        /// 强制结束技能（用于调试）
        /// </summary>
        public void EndSkill()
        {
            if (_skillCoroutine != null)
            {
                StopCoroutine(_skillCoroutine);
                _skillCoroutine = null;
            }

            _isSkillActive = false;
            OnSkillEnded?.Invoke();

            if (spriteRenderer != null)
            {
                spriteRenderer.color = skillInactiveColor;
            }
        }

        /// <summary>
        /// 设置技能持续时间
        /// </summary>
        public void SetSkillDuration(float duration)
        {
            skillDuration = duration;
        }

        /// <summary>
        /// 设置技能伤害
        /// </summary>
        public void SetSkillDamage(float damage)
        {
            skillDamage = damage;
        }
    }
}
