using UnityEngine;

namespace DodgeDots.WorldMap
{
    [RequireComponent(typeof(Animator))]
    public class CharacterVisualController : MonoBehaviour
    {
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        private Vector3 _lastPosition;
        private bool _isMoving;

        // 【新增】停止计时器
        private float _stopWaitTimer = 0f;

        // 【新增】设置一个缓冲时间，比如 0.15秒
        // 如果 0.15秒内没有检测到移动，才切换回 Idle
        [SerializeField] private float stopDelay = 0.15f;

        // 阈值可以稍微调小一点点
        private const float MoveThreshold = 0.001f;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            _lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            CheckMovement();
        }

        private void CheckMovement()
        {
            // 1. 计算这一帧移动了多远
            float distanceMoved = Vector3.Distance(transform.position, _lastPosition);

            // 2. 判断当前帧是否有显著移动
            bool framesMoved = distanceMoved > MoveThreshold;

            if (framesMoved)
            {
                // A. 如果动了：
                // 立即重置停止计时器
                _stopWaitTimer = 0f;

                // 如果之前是静止状态，立即切换为走路
                if (!_isMoving)
                {
                    _isMoving = true;
                    _animator.SetBool("IsMoving", true);
                }

                // 处理翻转 (Flip)
                HandleFlip(transform.position.x - _lastPosition.x);
            }
            else
            {
                // B. 如果没动 (或动得很微小)：
                // 不要立即设为 false，而是开始计时
                _stopWaitTimer += Time.deltaTime;

                // 只有当“没动”的状态持续超过了设定时间 (stopDelay)，才真的切回 Idle
                if (_isMoving && _stopWaitTimer > stopDelay)
                {
                    _isMoving = false;
                    _animator.SetBool("IsMoving", false);
                }
            }

            // 更新历史位置
            _lastPosition = transform.position;
        }

        private void HandleFlip(float deltaX)
        {
            // 防止微小的浮点数误差导致频繁翻转，加个小判断
            if (Mathf.Abs(deltaX) > 0.001f)
            {
                // 如果 deltaX < 0 (向左)，则 flipX = true
                // 如果 deltaX > 0 (向右)，则 flipX = false
                _spriteRenderer.flipX = deltaX < 0;
            }
        }
    }
}