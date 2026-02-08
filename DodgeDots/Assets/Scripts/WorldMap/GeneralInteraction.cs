using DodgeDots.WorldMap;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DodgeDots.Dialogue
{
    /// <summary>
    /// 交互阶段配置
    /// 定义某一个阶段的对话内容和触发事件
    /// </summary>
    [System.Serializable]
    public class DialogueStage
    {
        [Tooltip("阶段描述（仅用于编辑器标记，如'初次见面'）")]
        public string stageName = "Stage Description";

        [Tooltip("该阶段播放的对话配置")]
        public DialogueConfig dialogueConfig;

        [Header("阶段事件")]
        [Tooltip("该阶段对话**开始**时触发")]
        public UnityEvent onDialogueStart;

        [Tooltip("该阶段对话**结束**时触发")]
        public UnityEvent onDialogueFinish;
    }

    /// <summary>
    /// 通用交互组件 (增强版)
    /// 采用渲染器边界检测逻辑（移植自 LevelNode）
    /// </summary>
    public class GeneralInteraction : MonoBehaviour
    {
        [Header("唯一标识 (用于存档)")]
        [Tooltip("每个交互物体必须唯一，例如 Boss_Level1_Talk")]
        public string interactionID;

        [Header("对话阶段列表")]
        [Tooltip("按顺序执行对话。执行完最后一个后，后续交互将一直重复最后一个。")]
        public List<DialogueStage> dialogueStages = new List<DialogueStage>();

        [Header("交互设置")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt; // UI提示

        [Header("边界检测设置 (移植自 LevelNode)")]
        [Tooltip("用于判定边界的 SpriteRenderer (通常是 NPC 或 Boss 的贴图)")]
        [SerializeField] private SpriteRenderer interactionRenderer;
        [Tooltip("感应区向外扩大的额外边界")]
        [SerializeField] private float interactRangeExpand = 0.4f;

        [Header("关联设置")]
        [Tooltip("当玩家进入范围时，会将此节点设为'玩家附近'状态")]
        [SerializeField] private LevelNode targetLevelNode;


        private int _currentStageIndex = 0;
        private bool _playerInRange = false;
        private DialogueManager _dialogueManager;
        private bool _isInteracting = false;
        private Transform _playerTransform;

        private void Start()
        {
            _dialogueManager = DialogueManager.Instance;

            //如果 OnEnable 执行时 Manager 还没准备好，这里需要补订阅
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueEnded -= OnDialogueEnded;
                _dialogueManager.OnDialogueEnded += OnDialogueEnded;
            }

            // 通过控制器查找玩家
            var playerController = Object.FindFirstObjectByType<DodgeDots.WorldMap.PlayerWorldMapController>();
            if (playerController != null) _playerTransform = playerController.transform;

            // 如果没手动指定渲染器，尝试从自身获取
            if (interactionRenderer == null) interactionRenderer = GetComponent<SpriteRenderer>();

            // 读取存档进度
            if (!string.IsNullOrEmpty(interactionID))
            {
                _currentStageIndex = PlayerPrefs.GetInt($"Interaction_{interactionID}_Index", 0);
            }

            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }

        private void OnEnable()
        {
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded;
        }

        private void OnDisable()
        {
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded;
        }

        private void Update()
        {
            // 2. 实时检测玩家是否进入边界 (移植自 LevelNode)
            HandleRangeDetection();

            // 3. 交互触发
            if (_playerInRange && !_dialogueManager.IsDialogueActive)
            {
                if (Input.GetKeyDown(interactionKey))
                {
                    StartInteraction();
                }
            }
        }

        /// <summary>
        /// 核心边界检测逻辑
        /// </summary>
        private void HandleRangeDetection()
        {
            if (_playerTransform == null || interactionRenderer == null) return;

            // 获取渲染器边界
            Bounds spriteBounds = interactionRenderer.bounds;
            // 扩大边界感应区
            spriteBounds.Expand(interactRangeExpand);

            // 检测玩家坐标是否在矩形范围内
            bool isInside = spriteBounds.Contains(_playerTransform.position);

            if (isInside != _playerInRange)
            {
                _playerInRange = isInside;
                if (_playerInRange) ShowInteractionPrompt();
                else HideInteractionPrompt();
                // 同步通知 LevelNode 玩家是否在附近
                if (targetLevelNode != null)
                {
                    targetLevelNode.SetPlayerNear(_playerInRange);
                }
            }
        }

        private void StartInteraction()
        {
            if (dialogueStages == null || dialogueStages.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] GeneralInteraction: 对话列表为空！");
                return;
            }

            int actualIndex = Mathf.Clamp(_currentStageIndex, 0, dialogueStages.Count - 1);
            DialogueStage currentStage = dialogueStages[actualIndex];

            if (currentStage.dialogueConfig != null)
            {
                _isInteracting = true;
                currentStage.onDialogueStart?.Invoke();
                _dialogueManager.StartDialogue(currentStage.dialogueConfig);
                HideInteractionPrompt();
            }
        }

        private void OnDialogueEnded()
        {
            if (_isInteracting)
            {
                _isInteracting = false;

                int actualIndex = Mathf.Clamp(_currentStageIndex, 0, dialogueStages.Count - 1);
                DialogueStage completedStage = dialogueStages[actualIndex];

                completedStage.onDialogueFinish?.Invoke();
                _currentStageIndex++;

                if (!string.IsNullOrEmpty(interactionID))
                {
                    PlayerPrefs.SetInt($"Interaction_{interactionID}_Index", _currentStageIndex);
                }

                // 只有在检测到玩家仍在范围内时才重新显示 E 提示
                if (_playerInRange) ShowInteractionPrompt();
            }
        }

        private void ShowInteractionPrompt() { if (interactionPrompt) interactionPrompt.SetActive(true); }
        private void HideInteractionPrompt() { if (interactionPrompt) interactionPrompt.SetActive(false); }

        // 在场景视图画出检测框，方便调整边界大小
        private void OnDrawGizmosSelected()
        {
            if (interactionRenderer != null)
            {
                Gizmos.color = Color.cyan;
                Bounds b = interactionRenderer.bounds;
                b.Expand(interactRangeExpand);
                Gizmos.DrawWireCube(b.center, b.size);
            }
        }
    }
}