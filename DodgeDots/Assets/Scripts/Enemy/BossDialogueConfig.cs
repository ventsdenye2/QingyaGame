using UnityEngine;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// Boss阶段文案数据
    /// 定义Boss进入某个阶段时的文案内容
    /// </summary>
    [System.Serializable]
    public class BossPhaseDialogue
    {
        [Header("阶段信息")]
        [Tooltip("阶段ID（0=初始阶段，1=第一阶段，2=第二阶段...）")]
        public int phaseId = 0;

        [Tooltip("阶段名称")]
        public string phaseName = "Phase";

        [Header("文案设置")]
        [Tooltip("文案内容")]
        [TextArea(3, 5)]
        public string dialogueText = "";

        [Tooltip("文案显示时长（秒，0表示需要手动关闭）")]
        public float dialogueDuration = 3f;

        [Tooltip("是否暂停游戏显示文案")]
        public bool pauseGameForDialogue = false;
    }

    /// <summary>
    /// Boss文案配置（ScriptableObject）
    /// 管理Boss所有阶段的文案内容
    /// </summary>
    [CreateAssetMenu(fileName = "BossDialogueConfig", menuName = "DodgeDots/Boss Dialogue Config")]
    public class BossDialogueConfig : ScriptableObject
    {
        [Header("配置信息")]
        [Tooltip("配置名称")]
        public string configName = "Boss Dialogue Config";

        [Header("文案列表")]
        [Tooltip("Boss各阶段的文案配置")]
        public BossPhaseDialogue[] phaseDialogues;

        /// <summary>
        /// 根据阶段ID获取文案
        /// </summary>
        public BossPhaseDialogue GetDialogueByPhase(int phaseId)
        {
            if (phaseDialogues == null) return null;

            foreach (var dialogue in phaseDialogues)
            {
                if (dialogue.phaseId == phaseId)
                {
                    return dialogue;
                }
            }

            return null;
        }
    }
}
