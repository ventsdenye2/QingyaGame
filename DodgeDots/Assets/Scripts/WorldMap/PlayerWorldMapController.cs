using UnityEngine;

namespace DodgeDots.WorldMap
{
    /// <summary>
    /// 玩家世界地图控制器
    /// 处理玩家在世界地图上的移动和交互
    /// </summary>
    public class PlayerWorldMapController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private bool useMouseInput = true;
        [SerializeField] private bool useKeyboardInput = false;

        [Header("相机设置")]
        [SerializeField] private Camera worldMapCamera;
        [SerializeField] private bool cameraFollowPlayer = true;
        [SerializeField] private float cameraFollowSpeed = 5f;

        [Header("当前节点")]
        [SerializeField] private LevelNode currentNode;

        private Vector3 _targetPosition;
        private bool _isMoving = false;
        private Rigidbody2D _rigidbody2D;

        private void Start()
        {
            // 获取 Rigidbody2D 组件（如果存在）
            _rigidbody2D = GetComponent<Rigidbody2D>();

            if (worldMapCamera == null)
            {
                worldMapCamera = Camera.main;
                if (worldMapCamera == null)
                {
                    Debug.LogError("PlayerWorldMapController: 找不到相机！请在Inspector中指定World Map Camera。");
                }
            }

            // 初始化位置到当前节点
            if (currentNode != null)
            {
                transform.position = currentNode.transform.position;
                _targetPosition = transform.position;
            }

            // 调试信息
            Debug.Log($"PlayerWorldMapController 初始化: useMouseInput={useMouseInput}, useKeyboardInput={useKeyboardInput}, camera={worldMapCamera?.name}, hasRigidbody2D={_rigidbody2D != null}");
        }

        private void Update()
        {
            HandleInput();
            HandleMovement();
            HandleCameraFollow();
        }

        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            // 鼠标输入
            if (useMouseInput)
            {
                if (worldMapCamera == null)
                {
                    Debug.LogWarning("PlayerWorldMapController: worldMapCamera 为 null，无法获取鼠标位置");
                    return;
                }

                Vector3 mouseWorldPos = worldMapCamera.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = transform.position.z; // 保持Z轴不变
                _targetPosition = mouseWorldPos;

                // 调试信息（每60帧输出一次，避免刷屏）
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"鼠标世界坐标: {mouseWorldPos}, 玩家位置: {transform.position}");
                }
            }
            // 键盘输入
            else if (useKeyboardInput && !_isMoving)
            {
                float horizontal = Input.GetAxisRaw("Horizontal");
                float vertical = Input.GetAxisRaw("Vertical");

                if (horizontal != 0 || vertical != 0)
                {
                    Vector3 direction = new Vector3(horizontal, vertical, 0).normalized;
                    _targetPosition = transform.position + direction * moveSpeed * Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// 处理移动
        /// </summary>
        private void HandleMovement()
        {
            if (useMouseInput)
            {
                // 鼠标模式：直接跟随鼠标位置
                float distance = Vector3.Distance(transform.position, _targetPosition);

                // 如果有 Rigidbody2D，使用物理引擎的方式移动
                if (_rigidbody2D != null)
                {
                    _rigidbody2D.position = _targetPosition;
                }
                else
                {
                    transform.position = _targetPosition;
                }

                _isMoving = distance > 0.01f;
            }
            else
            {
                // 键盘模式：以固定速度移动
                if (Vector3.Distance(transform.position, _targetPosition) > 0.01f)
                {
                    Vector3 newPosition = Vector3.MoveTowards(
                        transform.position,
                        _targetPosition,
                        moveSpeed * Time.deltaTime
                    );

                    // 如果有 Rigidbody2D，使用物理引擎的方式移动
                    if (_rigidbody2D != null)
                    {
                        _rigidbody2D.position = newPosition;
                    }
                    else
                    {
                        transform.position = newPosition;
                    }

                    _isMoving = true;
                }
                else
                {
                    _isMoving = false;
                }
            }
        }

        /// <summary>
        /// 处理相机跟随
        /// </summary>
        private void HandleCameraFollow()
        {
            if (!cameraFollowPlayer || worldMapCamera == null) return;

            Vector3 targetCameraPos = new Vector3(
                transform.position.x,
                transform.position.y,
                worldMapCamera.transform.position.z
            );

            worldMapCamera.transform.position = Vector3.Lerp(
                worldMapCamera.transform.position,
                targetCameraPos,
                cameraFollowSpeed * Time.deltaTime
            );
        }

        /// <summary>
        /// 移动到指定节点
        /// </summary>
        public void MoveToNode(LevelNode node)
        {
            if (node == null) return;

            currentNode = node;
            _targetPosition = node.transform.position;
        }

        /// <summary>
        /// 设置当前节点
        /// </summary>
        public void SetCurrentNode(LevelNode node)
        {
            currentNode = node;
            if (node != null)
            {
                transform.position = node.transform.position;
                _targetPosition = transform.position;
            }
        }
    }
}
