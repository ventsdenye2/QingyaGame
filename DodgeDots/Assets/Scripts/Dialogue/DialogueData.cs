using UnityEngine;

namespace DodgeDots.Dialogue
{
    /// <summary>
    /// 对话节点类型
    /// </summary>
    public enum DialogueNodeType
    {
        Normal,         // 普通对话
        Choice,         // 选择分支
        End             // 对话结束
    }

    /// <summary>
    /// 对话选项数据
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        [Tooltip("选项文本")]
        public string choiceText = "";

        [Tooltip("选择后跳转到的对话节点ID")]
        public int nextNodeId = -1;
    }

    /// <summary>
    /// 单个对话节点数据
    /// </summary>
    [System.Serializable]
    public class DialogueNode
    {
        [Header("节点信息")]
        [Tooltip("节点ID（用于跳转）")]
        public int nodeId = 0;

        [Tooltip("节点类型")]
        public DialogueNodeType nodeType = DialogueNodeType.Normal;

        [Header("说话者信息")]
        [Tooltip("说话者名称")]
        public string speakerName = "";

        [Tooltip("说话者头像/立绘")]
        public Sprite speakerSprite;

        [Header("对话内容")]
        [Tooltip("对话文本")]
        [TextArea(3, 6)]
        public string dialogueText = "";

        [Header("跳转设置")]
        [Tooltip("下一个对话节点ID（-1表示结束）")]
        public int nextNodeId = -1;

        [Header("选项设置（仅当nodeType为Choice时有效）")]
        [Tooltip("对话选项列表")]
        public DialogueChoice[] choices;
    }
}
