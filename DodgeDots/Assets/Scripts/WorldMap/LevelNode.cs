using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DodgeDots.WorldMap
{
    /// <summary>
    /// 关卡节点状态
    /// </summary>
    public enum LevelNodeState
    {
        Locked,         // 锁定
        Unlocked,       // 已解锁
        Completed,      // 已完成
        Current         // 当前选中
    }

    /// <summary>
    /// 关卡节点组件
    /// </summary>
    public class LevelNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("关卡数据")]
        [SerializeField] private LevelNodeData nodeData;

        [Header("相邻节点")]
        [SerializeField] private LevelNode[] nextNodes;

        [Header("视觉组件")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer backgroundRenderer;

        [Header("交互增强 (New)")]
        [SerializeField] private float interactRange = 15.0f;
        [SerializeField] private GameObject interactHint;
        [SerializeField] private Material outlineMaterial;

        private LevelNodeState _currentState = LevelNodeState.Locked;
        private bool _isInteractable = true;
        private Transform _playerTransform;
        private bool _isPlayerNear = false;
        private Material _defaultMaterial;

        public string LevelId => nodeData != null ? nodeData.levelId : "";
        public LevelNodeData NodeData => nodeData;
        public LevelNode[] NextNodes => nextNodes;
        public LevelNodeState CurrentState => _currentState;

        public event Action<LevelNode> OnNodeClicked;
        public event Action<LevelNode> OnNodeHoverEnter;
        public event Action<LevelNode> OnNodeHoverExit;

        private void Start()
        {
            UpdateVisuals();

            // 缓存默认材质
            if (backgroundRenderer != null) _defaultMaterial = backgroundRenderer.material;

            // 通过你的控制器查找玩家
            var playerController = FindFirstObjectByType<PlayerWorldMapController>();
            if (playerController != null) _playerTransform = playerController.transform;

            if (interactHint != null) interactHint.SetActive(false);
        }

        private void Update()
        {
            if (_playerTransform == null || _currentState == LevelNodeState.Locked) return;

            // 获取背景贴图的边界（世界坐标）
            if (backgroundRenderer != null)
            {
                Bounds spriteBounds = backgroundRenderer.bounds;

                // 稍微扩大一点边界作为“感应区”（例如扩大 0.2 个单位），防止贴得太死
                spriteBounds.Expand(0.4f);

                // 检测玩家位置是否在矩形边界内
                bool isInside = spriteBounds.Contains(_playerTransform.position);

                if (isInside != _isPlayerNear)
                {
                    _isPlayerNear = isInside;
                    ToggleInteractionState(_isPlayerNear);
                }
            }

            // F键交互
            if (_isPlayerNear && Input.GetKeyDown(KeyCode.F))
            {
                TriggerNodeAction();
            }
        }

        private void ToggleInteractionState(bool show)
        {
            if (interactHint != null) interactHint.SetActive(show);

            if (backgroundRenderer != null && outlineMaterial != null)
            {
                backgroundRenderer.material = show ? outlineMaterial : _defaultMaterial;
            }
        }

        private void TriggerNodeAction()
        {
            if (!_isInteractable) return;
            // 触发 WorldMapManager 监听的事件
            OnNodeClicked?.Invoke(this);
        }

        public void SetState(LevelNodeState newState)
        {
            _currentState = newState;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (nodeData == null) return;
            if (iconRenderer != null && nodeData.nodeIcon != null)
                iconRenderer.sprite = nodeData.nodeIcon;

            Color targetColor = nodeData.nodeColor;
            switch (_currentState)
            {
                case LevelNodeState.Locked: targetColor = Color.gray; _isInteractable = false; break;
                case LevelNodeState.Unlocked: targetColor = nodeData.nodeColor; _isInteractable = true; break;
                case LevelNodeState.Completed: targetColor = Color.green; _isInteractable = true; break;
                case LevelNodeState.Current: targetColor = Color.yellow; _isInteractable = true; break;
            }
            if (backgroundRenderer != null) backgroundRenderer.color = targetColor;
        }

        public void OnPointerClick(PointerEventData eventData) => TriggerNodeAction();
        public void OnPointerEnter(PointerEventData eventData) => OnNodeHoverEnter?.Invoke(this);
        public void OnPointerExit(PointerEventData eventData) => OnNodeHoverExit?.Invoke(this);
    }
}