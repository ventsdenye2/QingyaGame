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
        [SerializeField] private bool useCustomBounds = false; // 是否使用自定义边界
        [SerializeField] private Vector2 customBounds = new Vector2(960f, 540f); // 自定义边界（1920x1080关卡的半边界）
        [SerializeField] private float boundsScale = 0.95f; // 边界缩放因子（仅在自动计算时使用）
        [SerializeField] private ControlMode controlMode = ControlMode.Keyboard;

        [Header("引用")]
        [SerializeField] private GameConfig gameConfig;

        private Rigidbody2D _rigidbody;
        private Vector2 _moveInput;
        private Vector2 _bounds;
        private Camera _mainCamera;
        private Vector2 _mouseTargetPosition; // 鼠标模式下的目标位置

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _mainCamera = Camera.main;

            // 确定边界大小
            if (useCustomBounds)
            {
                // 使用自定义边界
                _bounds = customBounds;
            }
            else if (gameConfig != null)
            {
                // 使用GameConfig配置的边界
                moveSpeed = gameConfig.playerMoveSpeed;
                _bounds = gameConfig.bossBattleBounds / 2f;
            }
            else
            {
                // 自动计算边界（基于相机视口）
                if (_mainCamera != null && restrictToBounds)
                {
                    float height = _mainCamera.orthographicSize;
                    float width = height * _mainCamera.aspect;
                    _bounds = new Vector2(width, height) * boundsScale;
                }
                else
                {
                    // 如果没有相机或不限制边界，使用一个大的默认值
                    _bounds = new Vector2(100f, 100f);
                }
            }

            // 调试日志
            Debug.Log($"PlayerController 初始化: controlMode={controlMode}, moveSpeed={moveSpeed}, " +
                      $"restrictToBounds={restrictToBounds}, bounds={_bounds}, " +
                      $"camera={((_mainCamera != null) ? _mainCamera.name : "null")}, " +
                      $"rigidbodyType={_rigidbody.bodyType}");

            // 检查Rigidbody2D设置
            if (controlMode == ControlMode.Mouse && _rigidbody.bodyType != RigidbodyType2D.Kinematic)
            {
                Debug.LogWarning("PlayerController: 鼠标控制模式建议将Rigidbody2D的Body Type设置为Kinematic，" +
                                 "否则物理引擎可能会干扰移动。当前类型: " + _rigidbody.bodyType);
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
            if (_mainCamera == null)
            {
                Debug.LogWarning("PlayerController: 相机为null，无法处理鼠标输入");
                return;
            }

            // 获取鼠标世界坐标
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            // 计算目标位置
            _mouseTargetPosition = mouseWorldPos;

            // 如果启用边界限制，限制目标位置
            if (restrictToBounds)
            {
                _mouseTargetPosition.x = Mathf.Clamp(_mouseTargetPosition.x, -_bounds.x, _bounds.x);
                _mouseTargetPosition.y = Mathf.Clamp(_mouseTargetPosition.y, -_bounds.y, _bounds.y);
            }

            // 清空移动输入，因为鼠标模式不使用方向输入
            _moveInput = Vector2.zero;
        }

        /// <summary>
        /// 处理移动
        /// </summary>
        private void HandleMovement()
        {
            if (controlMode == ControlMode.Mouse)
            {
                // 鼠标模式：直接设置位置
                _rigidbody.MovePosition(_mouseTargetPosition);
            }
            else
            {
                // 键盘模式：使用速度移动
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

        /// <summary>
        /// 获取是否使用自定义边界
        /// </summary>
        public bool GetUseCustomBounds()
        {
            return useCustomBounds;
        }
    }
}
