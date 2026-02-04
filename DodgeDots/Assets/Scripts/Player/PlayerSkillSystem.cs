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
        private Vector2 _lastFramePosition;  // 上一帧的位置
        private Collider2D _lastContactBoss;     // 上一帧接触的Boss，用于判断是否新接触

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

            Debug.Log("技能激活！");
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

            // 记录初始位置
            _lastFramePosition = transform.position;
            _lastContactBoss = null;  // 重置上一帧接触记录

            float elapsedTime = 0f;

            while (elapsedTime < skillDuration)
            {
                elapsedTime += Time.deltaTime;

                // 每帧检查一次与Boss的碰撞（改为更频繁）
                CheckCollisionWithBoss();

                // 更新上一帧位置
                _lastFramePosition = transform.position;

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
        /// 检测玩家碰撞体是否真正与Boss碰撞体接触
        /// 每次从"未接触"变为"接触"时造成一次伤害
        /// </summary>
        private void CheckCollisionWithBoss()
        {
            if (_collider == null)
            {
                Debug.LogWarning("[技能检测] 玩家没有CircleCollider2D组件");
                return;
            }

            Debug.Log($"[技能检测] 检测玩家碰撞体与Boss的接触");

            // 使用OverlapCircle检测玩家周围是否有Boss
            // 范围使用碰撞体半径作为基础
            float detectionRadius = _collider.radius;
            Vector2 playerPos = transform.position;
            
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, detectionRadius);

            Debug.Log($"[技能检测] 玩家位置: {playerPos}, 检测范围: {detectionRadius}, 找到 {hits.Length} 个碰撞体");

            // 本帧是否接触到Boss
            Collider2D currentContactBoss = null;

            foreach (Collider2D col in hits)
            {
                if (col == null || col == _collider)
                {
                    continue;
                }

                Debug.Log($"[技能检测] 检测到碰撞体: {col.gameObject.name}, 标签: {col.tag}");

                // 检查是否是Boss
                Enemy.BossBase boss = col.GetComponent<Enemy.BossBase>();
                if (boss != null)
                {
                    Debug.Log($"[技能检测] 找到Boss! Boss当前状态: {boss.CurrentState}, CanTakeDamage: {boss.CanTakeDamage}");
                    
                    currentContactBoss = col;
                    
                    // 只有当从"未接触"变为"接触"时才造成伤害（新的接触）
                    if (col != _lastContactBoss)
                    {
                        Debug.Log($"[技能检测] 新接触Boss，造成伤害: {skillDamage}");
                        
                        // 对Boss造成伤害
                        boss.TakeDamage(skillDamage, gameObject);
                        Debug.Log($"技能命中Boss！造成 {skillDamage} 点伤害，Boss血量: {boss.CurrentHealth}/{boss.MaxHealth}");
                    }
                    else
                    {
                        Debug.Log($"[技能检测] 继续接触同一个Boss，本帧不造成伤害");
                    }
                    
                    break; // 找到Boss后退出循环
                }
            }

            // 更新上一帧的接触状态
            _lastContactBoss = currentContactBoss;
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
