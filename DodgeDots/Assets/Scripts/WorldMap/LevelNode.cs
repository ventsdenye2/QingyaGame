using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DodgeDots.WorldMap
{
    public enum LevelNodeState
    {
        Locked,         // 锁定
        Unlocked,       // 已解�?
        Completed,      // 已完�?
        Current         // 当前选中
    }

    public class LevelNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("关卡数据")]
        [SerializeField] private LevelNodeData nodeData;

        [Header("相邻节点")]
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
        private Material _defaultMaterial; // 用来�?Unity 默认材质
        private Transform _playerTransform;

        // 公开属�?
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

            // 初始化时主动同步状�?
            // 无论是由 NPC 解锁还是自动解锁，存档里都会有记录�?
            // 这里必须读取记录，否则节点永远是 Locked，F键就不会响应�?
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
                    // 检查是否配置为默认解锁
                    if (nodeData != null && nodeData.unlockedByDefault)
                        SetState(LevelNodeState.Unlocked);
                    else
                        SetState(LevelNodeState.Locked);
                }
            }

            // 再次强制刷新视觉（双重保险）
            UpdateVisuals();

            var playerController = FindFirstObjectByType<PlayerWorldMapController>();
            if (playerController != null) _playerTransform = playerController.transform;
        }

        private void Update()
        {
            if (_currentState == LevelNodeState.Locked) return;

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

        public void UnlockSelf()
        {
            if (_currentState == LevelNodeState.Locked)
            {
                WorldMapManager.Instance.UnlockLevel(LevelId);
            }
        }

        public void SetState(LevelNodeState newState)
        {
            _currentState = newState;
            UpdateVisuals();
            if (_isPlayerNear) ToggleHighlight(true);
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

            // 删除了所有状态下的颜色切�?
            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = nodeData.nodeColor;
            }

            // 锁定状态依然需要强制关闭提�?
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

            // 只有非锁定状态，才允许切换描边材�?
            if (backgroundRenderer != null && outlineMaterial != null)
            {
                backgroundRenderer.material = show ? outlineMaterial : _defaultMaterial;
            }
        }

        // 鼠标交互
        private void TryUpdatePlayerNearFallback()
        {
            if (_playerTransform == null || backgroundRenderer == null) return;

            Bounds b = backgroundRenderer.bounds;
            b.Expand(interactRangeExpand);

            Vector3 checkPos = _playerTransform.position;
            checkPos.z = b.center.z;

            bool isInside = b.Contains(checkPos);
            if (isInside != _isPlayerNear)
            {
                _isPlayerNear = isInside;
                ToggleHighlight(_isPlayerNear);
            }
        }

        public void OnPointerClick(PointerEventData eventData) => EnterLevel();
        public void OnPointerEnter(PointerEventData eventData) => SetPlayerNear(true);
        public void OnPointerExit(PointerEventData eventData) => SetPlayerNear(false);
    }
}









