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

        [Header("相邻节点")]
        [SerializeField] private LevelNode[] nextNodes;

        [Header("视觉组件")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer backgroundRenderer;

        [Header("交互增强")]
        [SerializeField] private GameObject enterHint;
        [SerializeField] private Material outlineMaterial; // 描边材质

        private LevelNodeState _currentState = LevelNodeState.Locked;
        private bool _isPlayerNear = false;
        private Material _defaultMaterial; // 用来存 Unity 默认材质

        // 公开属性
        public LevelNodeData NodeData => nodeData;
        public LevelNode[] NextNodes => nextNodes;
        public string LevelId => nodeData != null ? nodeData.levelId : "";
        public LevelNodeState CurrentState => _currentState;

        // 事件
        public event Action<LevelNode> OnNodeClicked;

        private void Start()
        {
            // 备份默认材质
            if (backgroundRenderer != null) _defaultMaterial = backgroundRenderer.sharedMaterial;
            if (enterHint != null) enterHint.SetActive(false);

            // 初始化时主动同步状态
            // 无论是由 NPC 解锁还是自动解锁，存档里都会有记录。
            // 这里必须读取记录，否则节点永远是 Locked，F键就不会响应。
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
        }

        private void Update()
        {
            if (_currentState == LevelNodeState.Locked || !_isPlayerNear) return;
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

            // 准备颜色变量
            Color targetColor = nodeData.nodeColor; // 默认为配置的颜色

            switch (_currentState)
            {
                case LevelNodeState.Locked:
                    // 锁定状态下，强制把材质换回默认
                    if (backgroundRenderer != null && _defaultMaterial != null)
                    {
                        backgroundRenderer.material = _defaultMaterial;
                    }

                    targetColor = Color.gray; // 设置为灰色
                    if (enterHint != null) enterHint.SetActive(false); // 强制关闭提示
                    break;

                case LevelNodeState.Unlocked:
                    targetColor = nodeData.nodeColor; // 恢复原色
                    break;

                case LevelNodeState.Completed:
                    targetColor = Color.green; // 完成变绿
                    break;

                case LevelNodeState.Current:
                    targetColor = Color.yellow; // 当前变黄
                    break;
            }

            // 4. 应用颜色
            if (backgroundRenderer != null) backgroundRenderer.color = targetColor;
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

        // 鼠标交互
        public void OnPointerClick(PointerEventData eventData) => EnterLevel();
        public void OnPointerEnter(PointerEventData eventData) => SetPlayerNear(true);
        public void OnPointerExit(PointerEventData eventData) => SetPlayerNear(false);
    }
}