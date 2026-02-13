using DodgeDots.Save;
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
        [Tooltip("当这段对话结束时触发。可在此处调用 SetGameFlag 来解锁后续内容。")]
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

        [Tooltip("必须满足的自定义条件Flag（例如 'HasMetBoss'）。\n只有当这些Key在存档中都存在时，此分支才会被选中。")]
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
        [Tooltip("必须设置唯一的ID，用于记录该NPC的对话进度")]
        public string interactionID;

        [Header("分支系统")]
        [Tooltip("系统会从上到下检查，执行【第一个】满足所有条件的分支。")]
        public List<InteractionBranch> branches = new List<InteractionBranch>();

        [Header("交互设置")]
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;

        [Header("视觉组件")] // 新增分类
        [SerializeField] private SpriteRenderer interactionRenderer;
        [SerializeField] private Material outlineMaterial; // 新增：描边材质

        [SerializeField] private float interactRangeExpand = 0.4f;
        [SerializeField] private LevelNode targetLevelNode;

        private bool _playerInRange = false;
        private DialogueManager _dialogueManager;
        private bool _isInteracting = false;
        private Transform _playerTransform;
        private bool _isPermanentlyDisabled = false;
        private Material _defaultMaterial;

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

            // 尝试查找玩家
            var playerController = Object.FindFirstObjectByType<DodgeDots.WorldMap.PlayerWorldMapController>();
            if (playerController != null) _playerTransform = playerController.transform;

            if (interactionRenderer == null) interactionRenderer = GetComponent<SpriteRenderer>();

            if (interactionRenderer != null)
            {
                _defaultMaterial = interactionRenderer.sharedMaterial;
            }
            // --- 统一从 SaveSystem 加载所有状态 ---
            if (!string.IsNullOrEmpty(interactionID))
            {
                LoadStateFromSaveSystem();
            }

            HideInteractionPrompt();
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
            if (_isPermanentlyDisabled) return;

            HandleRangeDetection();

            if (_playerInRange && !_dialogueManager.IsDialogueActive)
            {
                UpdatePromptState();
            }
            // 不在范围内或者正在对话，隐藏
            else
            {
                HideInteractionPrompt();
            }
            // 处理交互输入
            if (_playerInRange && !_dialogueManager.IsDialogueActive && Input.GetKeyDown(interactionKey))
            {
                if (HasAvailableInteraction())
                {
                    StartInteraction();
                }
            }
        }

        /// <summary>
        /// 切换高亮描边状态
        /// </summary>
        private void ToggleHighlight(bool show)
        {
            if (interactionRenderer == null) return;

            // 如果有描边材质，则切换；否则保持原样
            if (outlineMaterial != null && _defaultMaterial != null)
            {
                interactionRenderer.material = show ? outlineMaterial : _defaultMaterial;
            }
        }

        /// <summary>
        /// 判断当前是否有可用的交互内容
        /// </summary>
        private bool HasAvailableInteraction()
        {
            if (_isPermanentlyDisabled) return false;

            // 获取当前符合条件的分支
            InteractionBranch branch = GetActiveBranch(out int index);

            // 没有任何分支满足条件，则无交互
            if (branch == null) return false;

            // 分支内没有配置对话，则视为无交互
            if (branch.stages.Count == 0) return false;

            // 索引小于总数，返回 true
            if (branch.currentIndex < branch.stages.Count) return true;

            // 索引已达上限，检查是否允许循环
            return branch.loopLastDialogue;
        }

        /// <summary>
        /// 统一更新提示图标的状态
        /// </summary>
        private void UpdatePromptState()
        {
            // 只有当玩家在范围内且有话可说时，才显示图标
            if (_playerInRange && HasAvailableInteraction())
            {
                ShowInteractionPrompt();
            }
            else
            {
                HideInteractionPrompt();
            }
        }

        /// <summary>
        /// 设置游戏Flag (供 UnityEvent 调用)
        /// </summary>
        public void SetGameFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return;

            SaveSystem.SetFlag(flagKey);

            Debug.Log($"[GeneralInteraction] Flag 已设置: {flagKey}");
        }

        /// <summary>
        /// 移除游戏Flag
        /// </summary>
        public void RemoveGameFlag(string flagKey)
        {
            if (string.IsNullOrEmpty(flagKey)) return;
            SaveSystem.RemoveFlag(flagKey);
        }

        /// <summary>
        /// 永久禁用此交互点
        /// </summary>
        public void DisableInteractionForever()
        {
            _isPermanentlyDisabled = true;
            _playerInRange = false;

            // 修复代码，最后一次手动通知 LevelNode：玩家“离开”了
            // 由于 _isPermanentlyDisabled 为 true 导致 Update 不再更新状态
            if (targetLevelNode != null)
            {
                targetLevelNode.SetPlayerNear(false);
                targetLevelNode.ForceDisableNode();
            }

            HideInteractionPrompt();

            if (!string.IsNullOrEmpty(interactionID))
            {
                if (SaveSystem.Current == null) SaveSystem.LoadOrCreate();

                string disabledKey = $"{interactionID}_DISABLED";
                if (!SaveSystem.Current.interactionStates.Contains(disabledKey))
                {
                    SaveSystem.Current.interactionStates.Add(disabledKey);
                    SaveSystem.Save();
                }
            }
        }

        private void HandleRangeDetection()
        {
            if (_playerTransform == null || interactionRenderer == null) return;

            // 简单的距离/范围检测
            Bounds b = interactionRenderer.bounds;
            b.Expand(interactRangeExpand);
            // 构造一个检测点，强制把玩家的 Z 轴“移动”到包围盒的中心 Z 轴上
            // 等于完全忽略了 Z 轴的深度差，只判断 X 和 Y 是否在框内
            Vector3 checkPos = _playerTransform.position;
            checkPos.z = b.center.z;

            bool isInside = b.Contains(checkPos);

            if (isInside != _playerInRange)
            {
                _playerInRange = isInside;

                // 状态改变时，更新图标显示逻辑
                UpdatePromptState();
                // 应用描边效果
                ToggleHighlight(_playerInRange);

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

                // 检查关卡条件
                if (!string.IsNullOrEmpty(branch.requiredLevelId))
                {
                    // 确保 WorldMapManager 存在
                    if (WorldMapManager.Instance != null)
                    {
                        bool isLevelCompleted = WorldMapManager.Instance.IsLevelCompleted(branch.requiredLevelId);
                        if (branch.triggerIfIncomplete) conditionMet = !isLevelCompleted;
                        else conditionMet = isLevelCompleted;
                    }
                }

                // 检查 Flag 条件
                if (conditionMet && branch.requiredFlags != null && branch.requiredFlags.Count > 0)
                {
                    foreach (var flag in branch.requiredFlags)
                    {
                        // 如果 flag 不存在，条件不满足
                        if (!SaveSystem.HasFlag(flag))
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

            if (activeBranch == null || activeBranch.stages.Count == 0) return;

            int stageIndex = activeBranch.currentIndex;

            // 检查索引越界
            if (stageIndex >= activeBranch.stages.Count)
            {
                if (activeBranch.loopLastDialogue) stageIndex = activeBranch.stages.Count - 1;
                else return;
            }

            _activeBranchIndex = branchIndex;
            DialogueStage currentStage = activeBranch.stages[stageIndex];

            // 触发开始事件
            currentStage.onDialogueStart?.Invoke();

            if (currentStage.dialogueConfig != null)
            {
                _isInteracting = true;
                _dialogueManager.StartDialogue(currentStage.dialogueConfig);
                HideInteractionPrompt();
            }
            else
            {
                // 如果没有配置对话文件，直接视为完成（可能是纯触发事件的节点）
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

        // 保存自身进度到 SaveSystem
        private void ExecuteStageFinish(InteractionBranch branch, DialogueStage stage)
        {
            stage.onDialogueFinish?.Invoke();

            if (_isPermanentlyDisabled) return;

            // 进度 +1
            branch.currentIndex++;

            // 保存到 SaveSystem
            int branchIdx = branches.IndexOf(branch);
            SaveStateToSystem(branchIdx, branch.currentIndex);

            // 重新检查是否还有话要说，更新图标
            UpdatePromptState();
        }

        private void SaveStateToSystem(int branchIndex, int stageIndex)
        {
            if (string.IsNullOrEmpty(interactionID)) return;

            if (SaveSystem.Current == null) SaveSystem.LoadOrCreate();

            // 移除旧的状态记录 (简单覆盖)
            SaveSystem.Current.interactionStates.RemoveAll(s => s.StartsWith($"{interactionID}:"));

            // 添加新记录: "ID:BranchIndex:StageIndex"
            string newState = $"{interactionID}:{branchIndex}:{stageIndex}";
            SaveSystem.Current.interactionStates.Add(newState);

            // 立即写入硬盘
            SaveSystem.Save();
        }

        // 从 SaveSystem 恢复状态
        private void LoadStateFromSaveSystem()
        {
            if (SaveSystem.Current == null) SaveSystem.LoadOrCreate();

            // 1. 检查是否被禁用
            if (SaveSystem.Current.interactionStates.Contains($"{interactionID}_DISABLED"))
            {
                _isPermanentlyDisabled = true;
                return;
            }

            // 2. 恢复分支进度
            foreach (var state in SaveSystem.Current.interactionStates)
            {
                if (state.StartsWith($"{interactionID}:"))
                {
                    string[] parts = state.Split(':');
                    if (parts.Length >= 3)
                    {
                        if (int.TryParse(parts[1], out int bIndex) && int.TryParse(parts[2], out int sIndex))
                        {
                            if (bIndex >= 0 && bIndex < branches.Count)
                            {
                                branches[bIndex].currentIndex = sIndex;
                            }
                        }
                    }
                }
            }
        }

        private void ShowInteractionPrompt() { if (interactionPrompt) interactionPrompt.SetActive(true); }
        private void HideInteractionPrompt() { if (interactionPrompt) interactionPrompt.SetActive(false); }
    }
}