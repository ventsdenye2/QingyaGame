using System;
using UnityEngine;

namespace DodgeDots.Dialogue
{
    /// <summary>
    /// 对话管理器
    /// 管理对话流程和状态
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [Header("当前对话")]
        [SerializeField] private DialogueConfig currentDialogue;

        private DialogueNode _currentNode;
        private bool _isDialogueActive = false;

        private static DialogueManager _instance;
        public static DialogueManager Instance => _instance;

        public bool IsDialogueActive => _isDialogueActive;
        public DialogueNode CurrentNode => _currentNode;

        public event Action<DialogueNode> OnDialogueNodeChanged;
        public event Action<DialogueConfig> OnDialogueStarted;
        public event Action OnDialogueEnded;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// 开始对话
        /// </summary>
        public void StartDialogue(DialogueConfig dialogue)
        {
            if (dialogue == null)
            {
                Debug.LogWarning("对话配置为空");
                return;
            }

            currentDialogue = dialogue;
            _currentNode = dialogue.GetStartNode();
            _isDialogueActive = true;

            OnDialogueStarted?.Invoke(dialogue);
            OnDialogueNodeChanged?.Invoke(_currentNode);
        }

        /// <summary>
        /// 前进到下一个对话节点
        /// </summary>
        public void AdvanceDialogue()
        {
            if (!_isDialogueActive || _currentNode == null) return;

            if (_currentNode.nodeType == DialogueNodeType.Choice)
            {
                Debug.LogWarning("当前节点是选择节点，请使用SelectChoice方法");
                return;
            }

            MoveToNode(_currentNode.nextNodeId);
        }

        /// <summary>
        /// 选择对话选项
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            if (!_isDialogueActive || _currentNode == null) return;
            if (_currentNode.nodeType != DialogueNodeType.Choice) return;
            if (_currentNode.choices == null || choiceIndex >= _currentNode.choices.Length) return;

            int nextNodeId = _currentNode.choices[choiceIndex].nextNodeId;
            MoveToNode(nextNodeId);
        }

        /// <summary>
        /// 移动到指定节点
        /// </summary>
        private void MoveToNode(int nodeId)
        {
            if (nodeId == -1 || currentDialogue == null)
            {
                EndDialogue();
                return;
            }

            _currentNode = currentDialogue.GetNodeById(nodeId);
            if (_currentNode == null)
            {
                Debug.LogWarning($"找不到节点ID: {nodeId}");
                EndDialogue();
                return;
            }

            if (_currentNode.nodeType == DialogueNodeType.End)
            {
                EndDialogue();
                return;
            }

            OnDialogueNodeChanged?.Invoke(_currentNode);
        }

        /// <summary>
        /// 结束对话
        /// </summary>
        public void EndDialogue()
        {
            _isDialogueActive = false;
            _currentNode = null;
            currentDialogue = null;
            OnDialogueEnded?.Invoke();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
