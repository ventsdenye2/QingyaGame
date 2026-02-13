using System;
using System.Collections.Generic;

namespace DodgeDots.Save
{
    [Serializable]
    public class SaveData
    {
        // 1. 关卡进度
        public List<string> completedLevels = new List<string>();

        public List<string> unlockedLevels = new List<string>();

        // 2. 对话记录 (记录已读过的 DialogueConfig.name)
        public List<string> finishedDialogues = new List<string>();

        // 交互状态 (记录 NPC 走到第几步分支了)
        public List<string> interactionStates = new List<string>();

        // 通用 Flag 系统
        // 记录游戏中的各种开关，例如 "HasMetBoss", "KeyObtained"
        public List<string> gameFlags = new List<string>();

        public string lastScene = "WorldMap";
        public string savedAtUtc = "";

        // --- 玩家世界地图精确坐标 ---
        public bool hasSavedPosition = false; // 是否有有效的坐标记录
        public float playerPosX;
        public float playerPosY;
        public float playerPosZ;
    }
}
