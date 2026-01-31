using UnityEngine;

namespace DodgeDots.Core
{
    /// <summary>
    /// 游戏全局配置
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "DodgeDots/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("玩家设置")]
        [Tooltip("玩家移动速度")]
        public float playerMoveSpeed = 5f;

        [Tooltip("玩家自动射击间隔（秒）")]
        public float playerAutoShootInterval = 0.2f;

        [Tooltip("玩家初始生命值")]
        public float playerMaxHealth = 100f;

        [Header("Boss战设置")]
        [Tooltip("Boss战场景边界大小")]
        public Vector2 bossBattleBounds = new Vector2(10f, 10f);

        [Tooltip("是否显示边界线")]
        public bool showBoundaryGizmos = true;

        [Header("弹幕设置")]
        [Tooltip("弹幕默认速度")]
        public float bulletDefaultSpeed = 3f;

        [Tooltip("弹幕对象池初始大小")]
        public int bulletPoolInitialSize = 100;

        [Header("性能设置")]
        [Tooltip("对象池最大容量")]
        public int objectPoolMaxCapacity = 500;
    }
}
