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
    /// 放置在世界地图上，代表一个可交互的关卡点
    /// </summary>
    public class LevelNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("关卡数据")]
        [SerializeField] private LevelNodeData nodeData;

        [Header("相邻节点")]
        [Tooltip("连接到的下一个关卡节点")]
        [SerializeField] private LevelNode[] nextNodes;

        [Header("视觉组件")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer backgroundRenderer;

        private LevelNodeState _currentState = LevelNodeState.Locked;
        private bool _isInteractable = true;

        public LevelNodeData NodeData => nodeData;
        public LevelNodeState CurrentState => _currentState;
        public LevelNode[] NextNodes => nextNodes;
        public string LevelId => nodeData != null ? nodeData.levelId : "";

        public event Action<LevelNode> OnNodeClicked;
        public event Action<LevelNode> OnNodeHoverEnter;
        public event Action<LevelNode> OnNodeHoverExit;

        private void Start()
        {
            UpdateVisuals();
        }

        /// <summary>
        /// 设置节点状态
        /// </summary>
        public void SetState(LevelNodeState newState)
        {
            _currentState = newState;
            UpdateVisuals();
        }

        /// <summary>
        /// 更新视觉表现
        /// </summary>
        private void UpdateVisuals()
        {
            if (nodeData == null) return;

            // 更新图标
            if (iconRenderer != null && nodeData.nodeIcon != null)
            {
                iconRenderer.sprite = nodeData.nodeIcon;
            }

            // 根据状态更新颜色
            Color targetColor = nodeData.nodeColor;
            switch (_currentState)
            {
                case LevelNodeState.Locked:
                    targetColor = Color.gray;
                    _isInteractable = false;
                    break;
                case LevelNodeState.Unlocked:
                    targetColor = nodeData.nodeColor;
                    _isInteractable = true;
                    break;
                case LevelNodeState.Completed:
                    targetColor = Color.green;
                    _isInteractable = true;
                    break;
                case LevelNodeState.Current:
                    targetColor = Color.yellow;
                    _isInteractable = true;
                    break;
            }

            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = targetColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            OnNodeClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            OnNodeHoverEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            OnNodeHoverExit?.Invoke(this);
        }
    }
}
