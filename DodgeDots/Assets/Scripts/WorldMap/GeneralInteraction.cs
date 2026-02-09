using DodgeDots.WorldMap;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DodgeDots.Dialogue
{
    [System.Serializable]
    public class DialogueStage
    {
        public string stageName = "Stage";
        public DialogueConfig dialogueConfig;
        [Header("阶段事件")]
        public UnityEvent onDialogueStart;
        public UnityEvent onDialogueFinish;
    }

    public class GeneralInteraction : MonoBehaviour
    {
        [Header("唯一标识 (用于存档)")]
        public string interactionID;

        [Header("对话配置")]
        public List<DialogueStage> dialogueStages = new List<DialogueStage>();

        [Header("简易解锁判断")]
        public string checkLevelId;
        public DialogueConfig fallbackDialogue;

        [Header("交互设置")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private SpriteRenderer interactionRenderer;
        [SerializeField] private float interactRangeExpand = 0.4f;
        [SerializeField] private LevelNode targetLevelNode;

        private int _currentStageIndex = 0;
        private bool _playerInRange = false;
        private DialogueManager _dialogueManager;
        private bool _isInteracting = false;
        private Transform _playerTransform;

        private bool _isPermanentlyDisabled = false;
        private bool _isPlayingFallback = false;

        private void Start()
        {
            _dialogueManager = DialogueManager.Instance;
            // 订阅事件保持不变
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueEnded -= OnDialogueEnded;
                _dialogueManager.OnDialogueEnded += OnDialogueEnded;
            }

            var playerController = Object.FindFirstObjectByType<DodgeDots.WorldMap.PlayerWorldMapController>();
            if (playerController != null) _playerTransform = playerController.transform;

            if (interactionRenderer == null) interactionRenderer = GetComponent<SpriteRenderer>();

            if (!string.IsNullOrEmpty(interactionID))
            {
                _currentStageIndex = PlayerPrefs.GetInt($"Interaction_{interactionID}_Index", 0);
                _isPermanentlyDisabled = PlayerPrefs.GetInt($"Interaction_{interactionID}_Disabled", 0) == 1;
            }

            if (_isPermanentlyDisabled) HideInteractionPrompt();
            else HideInteractionPrompt();
        }

        private void OnEnable() { if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded; }
        private void OnDisable() { if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded; }

        private void Update()
        {
            if (_isPermanentlyDisabled) return;

            HandleRangeDetection();

            if (_playerInRange && !_dialogueManager.IsDialogueActive && Input.GetKeyDown(interactionKey))
            {
                StartInteraction();
            }
        }

        public void DisableInteractionForever()
        {
            _isPermanentlyDisabled = true;
            _playerInRange = false;
            HideInteractionPrompt();

            if (!string.IsNullOrEmpty(interactionID))
            {
                PlayerPrefs.SetInt($"Interaction_{interactionID}_Disabled", 1);
                PlayerPrefs.Save();
            }
        }

        private void HandleRangeDetection()
        {
            if (_playerTransform == null || interactionRenderer == null) return;
            Bounds b = interactionRenderer.bounds;
            b.Expand(interactRangeExpand);
            bool isInside = b.Contains(_playerTransform.position);
            if (isInside != _playerInRange)
            {
                _playerInRange = isInside;
                if (_playerInRange) ShowInteractionPrompt(); else HideInteractionPrompt();
                if (targetLevelNode != null) targetLevelNode.SetPlayerNear(_playerInRange);
            }
        }

        private void StartInteraction()
        {
            if (dialogueStages == null || dialogueStages.Count == 0) return;

            // 检查条件
            if (!string.IsNullOrEmpty(checkLevelId) && WorldMapManager.Instance != null)
            {
                if (!WorldMapManager.Instance.IsLevelCompleted(checkLevelId))
                {
                    if (fallbackDialogue != null)
                    {
                        _isInteracting = true;
                        _isPlayingFallback = true;
                        _dialogueManager.StartDialogue(fallbackDialogue);
                        HideInteractionPrompt();
                    }
                    return;
                }
            }

            // 正常流程
            _isPlayingFallback = false;
            int actualIndex = Mathf.Clamp(_currentStageIndex, 0, dialogueStages.Count - 1);
            DialogueStage currentStage = dialogueStages[actualIndex];

            // 触发开始事件
            currentStage.onDialogueStart?.Invoke();

            if (currentStage.dialogueConfig != null)
            {
                // 有对话配置，交给 Manager 处理
                _isInteracting = true;
                _dialogueManager.StartDialogue(currentStage.dialogueConfig);
                HideInteractionPrompt();
            }
            else
            {
                // 没有配置对话 (None)，直接执行完成逻辑
                ExecuteStageFinish(currentStage);
            }
        }

        // 监听 Manager 的对话结束事件
        private void OnDialogueEnded()
        {
            if (_isInteracting)
            {
                _isInteracting = false;

                // 如果是 Fallback 对话结束，只恢复提示，不推进进度
                if (_isPlayingFallback)
                {
                    if (_playerInRange && !_isPermanentlyDisabled) ShowInteractionPrompt();
                    return;
                }

                // 正常对话结束，执行完成逻辑
                int actualIndex = Mathf.Clamp(_currentStageIndex, 0, dialogueStages.Count - 1);
                DialogueStage completedStage = dialogueStages[actualIndex];

                ExecuteStageFinish(completedStage);
            }
        }

        /// <summary>
        /// 通用完成逻辑
        /// </summary>
        private void ExecuteStageFinish(DialogueStage stage)
        {
            // 触发事件
            stage.onDialogueFinish?.Invoke();

            // 永久禁用了，就直接退出
            if (_isPermanentlyDisabled) return;

            // 推进进度
            _currentStageIndex++;
            if (!string.IsNullOrEmpty(interactionID))
            {
                PlayerPrefs.SetInt($"Interaction_{interactionID}_Index", _currentStageIndex);
            }

            // 如果还在范围内，恢复提示
            if (_playerInRange) ShowInteractionPrompt();
        }

        private void ShowInteractionPrompt() { if (interactionPrompt) interactionPrompt.SetActive(true); }
        private void HideInteractionPrompt() { if (interactionPrompt) interactionPrompt.SetActive(false); }
    }
}