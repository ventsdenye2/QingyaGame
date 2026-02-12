using DodgeDots.Save;
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
        private Vector2 _keyboardMovement = Vector2.zero;

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
            bool positionRestored = false;

            // 1. 尝试从存档恢复精确坐标
            // 确保 SaveSystem 已加载
            if (SaveSystem.Current == null) SaveSystem.LoadOrCreate();

            if (SaveSystem.Current.hasSavedPosition)
            {
                Vector3 savedPos = new Vector3(
                    SaveSystem.Current.playerPosX,
                    SaveSystem.Current.playerPosY,
                    SaveSystem.Current.playerPosZ
                );

                transform.position = savedPos;
                _targetPosition = savedPos; // 重要：同步更新目标位置，防止滑步
                positionRestored = true;

                Debug.Log($"[Player] 已恢复精确坐标: {savedPos}");
            }

            // 2. 如果没有存档坐标（第一次进入游戏），则使用 CurrentNode
            if (!positionRestored && currentNode != null)
            {
                transform.position = currentNode.transform.position;
                _targetPosition = transform.position;
            }

            // 调试信息
            Debug.Log($"PlayerWorldMapController 初始化: PosRestored={positionRestored}, Node={currentNode?.name}");
        }

        private void Update()
        {
            // 如果对话正在进行，禁止移动输入
            if (DodgeDots.Dialogue.DialogueManager.Instance != null &&
                DodgeDots.Dialogue.DialogueManager.Instance.IsDialogueActive)
            {
                // 停止物理移动 (如果有刚体)
                if (_rigidbody2D != null) _rigidbody2D.velocity = Vector2.zero;
                return;
            }

            HandleInput();
            HandleCameraFollow();
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
            // 键盘输入 - 只在Update中读取输入，不直接移动
            else if (useKeyboardInput)
            {
                // 1. 在 Update 中只读取输入（保证手感流畅）
                _keyboardMovement.x = Input.GetAxisRaw("Horizontal");
                _keyboardMovement.y = Input.GetAxisRaw("Vertical");
                
                // 标准化向量，防止斜向移动变快
                _keyboardMovement = _keyboardMovement.normalized;
            }
        }

        /// <summary>
        /// 处理移动
        /// </summary>
        private void HandleMovement()
        {
            if (useMouseInput)
            {
                // 鼠标模式：直接跟随鼠标位置（保留原有方式）
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
            else if (useKeyboardInput)
            {
                // 键盘模式：使用 velocity 移动
                if (_rigidbody2D != null)
                {
                    // 2. 在 FixedUpdate 中应用物理移动
                    // 直接修改速度是 Dynamic 刚体最稳健的方法，它会自动处理滑墙
                    _rigidbody2D.velocity = _keyboardMovement * moveSpeed;
                }
                else
                {
                    // 如果没有 Rigidbody2D，使用 transform 直接移动
                    Vector3 newPosition = transform.position + (Vector3)_keyboardMovement * moveSpeed * Time.fixedDeltaTime;
                    transform.position = newPosition;
                }

                _isMoving = _keyboardMovement.magnitude > 0.01f;
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
