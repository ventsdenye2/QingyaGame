using DodgeDots.WorldMap;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DodgeDots.Dialogue
{
    // 对话阶段（单个对话包）
    [System.Serializable]
    public class DialogueStage
    {
        [Tooltip("仅用于备注，如 '第一次见面'")]
        public string stageName = "Stage";
        public DialogueConfig dialogueConfig;

        [Header("阶段事件")]
        public UnityEvent onDialogueStart;
        [Tooltip("当这段对话结束时触发。可在此处调用 SetFlag 来解锁后续内容。")]
        public UnityEvent onDialogueFinish;
    }

    // 交互分支（根据条件决定的一套对话流程）
    [System.Serializable]
    public class InteractionBranch
    {
        [Tooltip("仅用于备注，如 '通关后分支'")]
        public string branchName = "Condition Branch";

        [Header("触发条件 (全部满足才触发)")]
        [Tooltip("必须通过的关卡ID")]
        public string requiredLevelId;

        [Tooltip("反转关卡条件：勾选后，只有在【未】通过该关卡时才触发。")]
        public bool triggerIfIncomplete = false;

        [Tooltip("必须满足的自定义条件Flag（例如 'TalkedToBossTwice'）。\n只有当这些Key在存档中都为1时，此分支才会被选中。")]
        public List<string> requiredFlags = new List<string>();

        [Header("流程配置")]
        [Tooltip("该分支下的对话列表（按顺序执行）")]
        public List<DialogueStage> stages = new List<DialogueStage>();

        [Tooltip("当播完最后一个对话后，再次交互是否一直重复最后一个？\n不勾选则不再响应。")]
        public bool loopLastDialogue = true;

        // 运行时索引
        [HideInInspector] public int currentIndex = 0;
    }

    public class GeneralInteraction : MonoBehaviour
    {
        [Header("唯一标识 (用于存档)")]
        public string interactionID;

        [Header("分支系统")]
        [Tooltip("系统会从上到下检查，执行【第一个】满足所有条件的分支。")]
        public List<InteractionBranch> branches = new List<InteractionBranch>();

        [Header("交互设置")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private SpriteRenderer interactionRenderer;
        [SerializeField] private float interactRangeExpand = 0.4f;
        [SerializeField] private LevelNode targetLevelNode;

        private bool _playerInRange = false;
        private DialogueManager _dialogueManager;
        private bool _isInteracting = false;
        private Transform _playerTransform;
        private bool _isPermanentlyDisabled = false;

        // 记录当前正在运行的分支索引
        private int _activeBranchIndex = -1;

        private void Start()
        {
            _dialogueManager = DialogueManager.Instance;
            if (_dialogueManager != null)
            {
                _dialogueManager.OnDialogueEnded -= OnDialogueEnded;
                _dialogueManager.OnDialogueEnded += OnDialogueEnded;
            }

            var playerController = Object.FindFirstObjectByType<DodgeDots.WorldMap.PlayerWorldMapController>();
            if (playerController != null) _playerTransform = playerController.transform;

            if (interactionRenderer == null) interactionRenderer = GetComponent<SpriteRenderer>();

            // --- 读取存档 ---
            if (!string.IsNullOrEmpty(interactionID))
            {
                _isPermanentlyDisabled = PlayerPrefs.GetInt($"Interaction_{interactionID}_Disabled", 0) == 1;

                // 恢复每个分支的对话进度
                for (int i = 0; i < branches.Count; i++)
                {
                    branches[i].currentIndex = PlayerPrefs.GetInt($"Interaction_{interactionID}_Branch_{i}_Index", 0);
                }
            }

            HideInteractionPrompt();
        }

        private void OnEnable() { if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded += OnDialogueEnded; }
        private void OnDisable() { if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= OnDialogueEnded; }

        private void Update()
        {
            if (_isPermanentlyDisabled) return;

            HandleRangeDetection();

            // 修改判断：增加 HasAvailableInteraction() 检查
            if (_playerInRange && !_dialogueManager.IsDialogueActive && Input.GetKeyDown(interactionKey))
            {
                // 这里再次检查以防万一
                if (HasAvailableInteraction())
                {
                    StartInteraction();
                }
            }
        }

        /// <summary>
        /// 核心判断逻辑：当前是否有可用的交互内容
        /// </summary>
        private bool HasAvailableInteraction()
        {
            if (_isPermanentlyDisabled) return false;

            // 获取当前符合条件的分支
            InteractionBranch branch = GetActiveBranch(out int index);

            // 1. 如果没有任何分支满足条件，则无交互
            if (branch == null) return false;

            // 2. 如果分支内没有配置对话，则视为无交互（或者你可以根据需求视为无动作交互）
            if (branch.stages.Count == 0) return false;

            // 3. 检查进度：
            // 如果索引小于总数，说明还有新话要说 -> 返回 true
            if (branch.currentIndex < branch.stages.Count) return true;

            // 4. 如果索引已达上限，检查是否允许循环
            // 允许循环 -> 返回 true，不允许 -> 返回 false
            return branch.loopLastDialogue;
        }

        /// <summary>
        /// 统一更新提示图标的状态
        /// </summary>
        private void UpdatePromptState()
        {
            // 只有当玩家在范围内 AND 有话可说时，才显示图标
            if (_playerInRange && HasAvailableInteraction())
            {
                ShowInteractionPrompt();
            }
            else
            {
                HideInteractionPrompt();
            }
        }

        public void SetFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return;
            PlayerPrefs.SetInt(flagKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"[GeneralInteraction] Flag 已设置: {flagKey} = 1");
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

                // 状态改变时，更新图标显示逻辑
                UpdatePromptState();

                if (targetLevelNode != null) targetLevelNode.SetPlayerNear(_playerInRange);
            }
        }

        private InteractionBranch GetActiveBranch(out int branchListIndex)
        {
            branchListIndex = -1;
            if (branches == null) return null;

            for (int i = 0; i < branches.Count; i++)
            {
                var branch = branches[i];
                bool conditionMet = true;

                if (!string.IsNullOrEmpty(branch.requiredLevelId) && WorldMapManager.Instance != null)
                {
                    bool isLevelCompleted = WorldMapManager.Instance.IsLevelCompleted(branch.requiredLevelId);
                    if (branch.triggerIfIncomplete) conditionMet = !isLevelCompleted;
                    else conditionMet = isLevelCompleted;
                }

                if (conditionMet && branch.requiredFlags != null && branch.requiredFlags.Count > 0)
                {
                    foreach (var flag in branch.requiredFlags)
                    {
                        if (PlayerPrefs.GetInt(flag, 0) == 0)
                        {
                            conditionMet = false;
                            break;
                        }
                    }
                }

                if (conditionMet)
                {
                    branchListIndex = i;
                    return branch;
                }
            }
            return null;
        }

        private void StartInteraction()
        {
            InteractionBranch activeBranch = GetActiveBranch(out int branchIndex);

            // 双重保险，虽然 HasAvailableInteraction 已经查过了
            if (activeBranch == null || activeBranch.stages.Count == 0) return;

            int stageIndex = activeBranch.currentIndex;
            if (stageIndex >= activeBranch.stages.Count)
            {
                if (activeBranch.loopLastDialogue) stageIndex = activeBranch.stages.Count - 1;
                else return;
            }

            _activeBranchIndex = branchIndex;
            DialogueStage currentStage = activeBranch.stages[stageIndex];

            currentStage.onDialogueStart?.Invoke();

            if (currentStage.dialogueConfig != null)
            {
                _isInteracting = true;
                _dialogueManager.StartDialogue(currentStage.dialogueConfig);
                HideInteractionPrompt(); // 交互开始时隐藏
            }
            else
            {
                ExecuteStageFinish(activeBranch, currentStage);
            }
        }

        private void OnDialogueEnded()
        {
            if (_isInteracting)
            {
                _isInteracting = false;

                if (_activeBranchIndex >= 0 && _activeBranchIndex < branches.Count)
                {
                    var branch = branches[_activeBranchIndex];

                    int stageIndex = branch.currentIndex;
                    if (stageIndex >= branch.stages.Count) stageIndex = branch.stages.Count - 1;

                    var completedStage = branch.stages[stageIndex];
                    ExecuteStageFinish(branch, completedStage);
                }

                _activeBranchIndex = -1;
            }
        }

        private void ExecuteStageFinish(InteractionBranch branch, DialogueStage stage)
        {
            stage.onDialogueFinish?.Invoke();

            if (_isPermanentlyDisabled) return;

            branch.currentIndex++;
            if (!string.IsNullOrEmpty(interactionID))
            {
                PlayerPrefs.SetInt($"Interaction_{interactionID}_Branch_{branches.IndexOf(branch)}_Index", branch.currentIndex);
                PlayerPrefs.Save();
            }

            // 对话结束后，重新计算是否还需要显示图标
            // 如果刚刚播的是最后一句且 loop=false，这里就会隐藏图标
            UpdatePromptState();
        }

        private void ShowInteractionPrompt() { if (interactionPrompt) interactionPrompt.SetActive(true); }
        private void HideInteractionPrompt() { if (interactionPrompt) interactionPrompt.SetActive(false); }
    }
}