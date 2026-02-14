using UnityEngine;

namespace DodgeDots.WorldMap
{
    /// <summary>
    /// 关卡节点类型
    /// </summary>
    public enum LevelNodeType
    {
        Normal,         // 普通关卡
        Boss,           // Boss关卡
        Special,        // 特殊关卡
        Shop,           // 商店
        Event           // 事件点
    }

    /// <summary>
    /// 关卡节点数据（ScriptableObject）
    /// 定义单个关卡节点的所有信息
    /// </summary>
    [CreateAssetMenu(fileName = "LevelNodeData", menuName = "DodgeDots/World Map/Level Node Data")]
    public class LevelNodeData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("关卡ID（唯一标识）")]
        public string levelId = "level_001";

        [Tooltip("关卡名称")]
        public string levelName = "Level 1";

        [Tooltip("关卡类型")]
        public LevelNodeType nodeType = LevelNodeType.Normal;

        [Header("场景信息")]
        [Tooltip("关卡场景名称")]
        public string sceneName = "";

        [Tooltip("关卡描述")]
        [TextArea(2, 4)]
        public string description = "";

        [Header("难度设置")]
        [Tooltip("关卡难度（1-5星）")]
        [Range(1, 5)]
        public int difficulty = 1;

        [Header("视觉设置")]
        [Tooltip("节点图标")]
        public Sprite nodeIcon;

        [Tooltip("节点颜色")]
        public Color nodeColor = Color.white;
    }
}