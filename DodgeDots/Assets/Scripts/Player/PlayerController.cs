using UnityEngine;
using DodgeDots.Core;

namespace DodgeDots.Player
{
    /// <summary>
    /// 玩家控制模式
    /// </summary>
    public enum ControlMode
    {
        Keyboard,  // WASD/方向键控制
        Mouse      // 鼠标控制
    }

    /// <summary>
    /// 玩家控制器，负责处理玩家移动和输入
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private bool restrictToBounds = true;
        [SerializeField] private ControlMode controlMode = ControlMode.Keyboard;

        [Header("引用")]
        [SerializeField] private GameConfig gameConfig;

        private Rigidbody2D _rigidbody;
        private Vector2 _moveInput;
        private Vector2 _bounds;
        private Camera _mainCamera;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _mainCamera = Camera.main;

            // 如果配置了GameConfig，使用配置的速度和边界
            if (gameConfig != null)
            {
                moveSpeed = gameConfig.playerMoveSpeed;
                _bounds = gameConfig.bossBattleBounds / 2f;
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void FixedUpdate()
        {
            HandleMovement();
        }

        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            switch (controlMode)
            {
                case ControlMode.Keyboard:
                    HandleKeyboardInput();
                    break;
                case ControlMode.Mouse:
                    HandleMouseInput();
                    break;
            }
        }

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            // 获取WASD或方向键输入
            _moveInput.x = Input.GetAxisRaw("Horizontal");
            _moveInput.y = Input.GetAxisRaw("Vertical");

            // 归一化移动向量，防止斜向移动速度过快
            _moveInput = _moveInput.normalized;
        }

        /// <summary>
        /// 处理鼠标输入
        /// </summary>
        private void HandleMouseInput()
        {
            if (_mainCamera == null) return;

            // 获取鼠标世界坐标
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            // 计算从玩家到鼠标的方向
            Vector2 direction = (mouseWorldPos - transform.position).normalized;

            // 计算距离，如果距离很近就停止移动
            float distance = Vector2.Distance(transform.position, mouseWorldPos);
            if (distance < 0.1f)
            {
                _moveInput = Vector2.zero;
            }
            else
            {
                _moveInput = direction;
            }
        }

        /// <summary>
        /// 处理移动
        /// </summary>
        private void HandleMovement()
        {
            Vector2 velocity = _moveInput * moveSpeed;
            _rigidbody.velocity = velocity;

            // 如果启用边界限制，限制玩家位置
            if (restrictToBounds)
            {
                Vector2 clampedPosition = _rigidbody.position;
                clampedPosition.x = Mathf.Clamp(clampedPosition.x, -_bounds.x, _bounds.x);
                clampedPosition.y = Mathf.Clamp(clampedPosition.y, -_bounds.y, _bounds.y);
                _rigidbody.position = clampedPosition;
            }
        }

        /// <summary>
        /// 设置移动速度
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }

        /// <summary>
        /// 获取当前移动方向
        /// </summary>
        public Vector2 GetMoveDirection()
        {
            return _moveInput;
        }

        /// <summary>
        /// 设置是否限制在边界内（用于切换关卡类型）
        /// </summary>
        public void SetBoundsRestriction(bool restrict)
        {
            restrictToBounds = restrict;
        }

        /// <summary>
        /// 设置边界大小
        /// </summary>
        public void SetBounds(Vector2 bounds)
        {
            _bounds = bounds / 2f;
        }

        /// <summary>
        /// 切换控制模式
        /// </summary>
        public void SetControlMode(ControlMode mode)
        {
            controlMode = mode;
        }

        /// <summary>
        /// 获取当前控制模式
        /// </summary>
        public ControlMode GetControlMode()
        {
            return controlMode;
        }
    }
}
