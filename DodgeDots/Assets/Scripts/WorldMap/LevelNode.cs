using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DodgeDots.WorldMap
{
    public enum LevelNodeState
    {
        Locked,         // 锁定
        Unlocked,       // 已解锁
        Completed,      // 已完成
        Current         // 当前选中
    }

    public class LevelNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("关卡数据")]
        [SerializeField] private LevelNodeData nodeData;

        [Header("相邻节点 (仅作数据引用)")]
        [SerializeField] private LevelNode[] nextNodes;

        [Header("视觉组件")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer backgroundRenderer;

        [Header("交互增强")]
        [SerializeField] private GameObject enterHint;
        [SerializeField] private Material outlineMaterial; // 描边材质
        [SerializeField] private float interactRangeExpand = 0.4f;

        private LevelNodeState _currentState = LevelNodeState.Locked;
        private bool _isPlayerNear = false;
        private Material _defaultMaterial; // 用来存Unity默认材质
        private Transform _playerTransform;
        private bool _isForceDisabled = false;

        // 公开属性
        public LevelNodeData NodeData => nodeData;
        public LevelNode[] NextNodes => nextNodes;
        public string LevelId => nodeData != null ? nodeData.levelId : "";
        public LevelNodeState CurrentState => _currentState;
        public bool IsPlayerNear => _isPlayerNear;

        // 事件
        public event Action<LevelNode> OnNodeClicked;

        private void Start()
        {
            // 备份默认材质
            if (backgroundRenderer != null) _defaultMaterial = backgroundRenderer.sharedMaterial;
            if (enterHint != null) enterHint.SetActive(false);

            // 初始化状态逻辑：完全依赖 Manager (存档) 的状态
            if (WorldMapManager.Instance != null)
            {
                bool isCompleted = WorldMapManager.Instance.IsLevelCompleted(LevelId);
                bool isUnlocked = WorldMapManager.Instance.IsLevelUnlocked(LevelId);

                if (isCompleted)
                {
                    SetState(LevelNodeState.Completed);
                }
                else if (isUnlocked)
                {
                    SetState(LevelNodeState.Unlocked);
                }
                else
                {
                    // 默认为锁定，必须通过 UnlockSelf() 或读取到已解锁存档才能打开
                    SetState(LevelNodeState.Locked);
                }
            }
            else
            {
                // 没有 Manager 时默认为锁定
                SetState(LevelNodeState.Locked);
            }

            // 再次强制刷新视觉（双重保险）
            UpdateVisuals();

            var playerController = FindFirstObjectByType<PlayerWorldMapController>();
            if (playerController != null) _playerTransform = playerController.transform;
        }

        private void Update()
        {
            if (_isForceDisabled || _currentState == LevelNodeState.Locked) return;

            if (!_isPlayerNear)
            {
                TryUpdatePlayerNearFallback();
            }

            if (!_isPlayerNear) return;
            if (Input.GetKeyDown(KeyCode.F)) EnterLevel();
        }

        public void SetPlayerNear(bool isNear)
        {
            if (_isPlayerNear == isNear) return;
            _isPlayerNear = isNear;
            ToggleHighlight(_isPlayerNear);
        }

        /// <summary>
        /// 外部调用此方法解锁关卡
        /// 这是目前唯一的解锁入口
        /// </summary>
        public void UnlockSelf()
        {
            // 只有当前是锁定状态才执行解锁
            if (_currentState == LevelNodeState.Locked)
            {
                // 1. 通知数据层解锁 (写入存档)
                if (WorldMapManager.Instance != null)
                {
                    WorldMapManager.Instance.UnlockLevel(LevelId);
                }

                // 2. 立即更新视觉状态为解锁
                SetState(LevelNodeState.Unlocked);
                Debug.Log($"[LevelNode] 关卡 {LevelId} 已手动解锁");
            }
        }

        public void SetState(LevelNodeState newState)
        {
            _currentState = newState;
            UpdateVisuals();
            // 如果玩家刚好在旁边，状态改变后需要刷新一下高亮
            if (_isPlayerNear && _currentState != LevelNodeState.Locked)
            {
                ToggleHighlight(true);
            }
            else if (_currentState == LevelNodeState.Locked)
            {
                ToggleHighlight(false);
            }
        }

        private void EnterLevel()
        {
            if (_currentState == LevelNodeState.Locked) return;
            OnNodeClicked?.Invoke(this);
        }

        private void UpdateVisuals()
        {
            if (nodeData == null) return;

            // 更新图标
            if (iconRenderer != null && nodeData.nodeIcon != null)
                iconRenderer.sprite = nodeData.nodeIcon;

            // 更新背景颜色
            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = nodeData.nodeColor;
            }

            // 锁定状态依然需要强制关闭提示
            if (_currentState == LevelNodeState.Locked)
            {
                if (enterHint != null) enterHint.SetActive(false);
            }
        }

        private void ToggleHighlight(bool show)
        {
            // 锁定状态下，严禁开启高亮，直接返回
            if (_currentState == LevelNodeState.Locked) return;

            if (enterHint != null) enterHint.SetActive(show);

            // 只有非锁定状态，才允许切换描边材质
            if (backgroundRenderer != null && outlineMaterial != null)
            {
                backgroundRenderer.material = show ? outlineMaterial : _defaultMaterial;
            }
        }

        // 鼠标交互 & 范围检测
        private void TryUpdatePlayerNearFallback()
        {
            if (_playerTransform == null) return;

            var renderer = iconRenderer != null ? iconRenderer : backgroundRenderer;
            if (renderer != null)
            {
                Bounds b = renderer.bounds;
                b.Expand(interactRangeExpand);

                // 如果范围异常大（可能是放大的图或地图），改用半径判断
                if (b.extents.x > 5f || b.extents.y > 5f)
                {
                    float radius = 1.5f + interactRangeExpand;
                    bool nearByRadius = Vector2.Distance(_playerTransform.position, transform.position) <= radius;
                    if (nearByRadius != _isPlayerNear)
                    {
                        _isPlayerNear = nearByRadius;
                        ToggleHighlight(_isPlayerNear);
                    }
                    return;
                }

                Vector3 checkPos = _playerTransform.position;
                checkPos.z = b.center.z;

                bool isInside = b.Contains(checkPos);
                if (isInside != _isPlayerNear)
                {
                    _isPlayerNear = isInside;
                    ToggleHighlight(_isPlayerNear);
                }
                return;
            }

            float fallbackRadius = 1.5f + interactRangeExpand;
            bool isNear = Vector2.Distance(_playerTransform.position, transform.position) <= fallbackRadius;
            if (isNear != _isPlayerNear)
            {
                _isPlayerNear = isNear;
                ToggleHighlight(_isPlayerNear);
            }
        }

        public void ForceDisableNode()
        {
            _isForceDisabled = true;
            _isPlayerNear = false;
            if (enterHint != null) enterHint.SetActive(false);
            ToggleHighlight(false);
            _currentState = LevelNodeState.Locked;
        }

        public void OnPointerClick(PointerEventData eventData) => EnterLevel();
        public void OnPointerEnter(PointerEventData eventData) => SetPlayerNear(true);
        public void OnPointerExit(PointerEventData eventData) => SetPlayerNear(false);
    }
}