using UnityEngine;

namespace DodgeDots.Dialogue
{
    /// <summary>
    /// 对话配置（ScriptableObject）
    /// 管理一段完整的对话内容
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueConfig", menuName = "DodgeDots/Dialogue/Dialogue Config")]
    public class DialogueConfig : ScriptableObject
    {
        [Header("对话信息")]
        [Tooltip("对话ID（唯一标识）")]
        public string dialogueId = "dialogue_001";

        [Tooltip("对话名称")]
        public string dialogueName = "Dialogue";

        [Header("对话节点")]
        [Tooltip("对话节点列表")]
        public DialogueNode[] dialogueNodes;

        [Header("初始设置")]
        [Tooltip("起始节点ID")]
        public int startNodeId = 0;

        /// <summary>
        /// 根据节点ID获取对话节点
        /// </summary>
        public DialogueNode GetNodeById(int nodeId)
        {
            if (dialogueNodes == null) return null;

            foreach (var node in dialogueNodes)
            {
                if (node != null && node.nodeId == nodeId)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取起始节点
        /// </summary>
        public DialogueNode GetStartNode()
        {
            return GetNodeById(startNodeId);
        }
    }
}
