using UnityEngine;

namespace DodgeDots.WorldMap
{
    /// <summary>
    /// 世界地图配置（ScriptableObject）
    /// 管理整个世界地图的关卡节点和设置
    /// </summary>
    [CreateAssetMenu(fileName = "WorldMapConfig", menuName = "DodgeDots/World Map/World Map Config")]
    public class WorldMapConfig : ScriptableObject
    {
        [Header("地图信息")]
        [Tooltip("地图名称")]
        public string mapName = "World 1";

        [Tooltip("地图描述")]
        [TextArea(2, 4)]
        public string mapDescription = "";

        [Header("关卡节点")]
        [Tooltip("所有关卡节点数据")]
        public LevelNodeData[] levelNodes;

        [Header("初始设置")]
        [Tooltip("初始解锁的关卡ID")]
        public string initialUnlockedLevelId = "level_001";

        [Header("相机设置")]
        [Tooltip("地图边界（用于限制相机移动）")]
        public Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(115.2f, 115.2f, 0));

        [Tooltip("相机移动速度")]
        public float cameraSpeed = 5f;

        /// <summary>
        /// 根据ID获取关卡数据
        /// </summary>
        public LevelNodeData GetLevelData(string levelId)
        {
            if (levelNodes == null) return null;

            foreach (var node in levelNodes)
            {
                if (node != null && node.levelId == levelId)
                {
                    return node;
                }
            }

            return null;
        }
    }
}
