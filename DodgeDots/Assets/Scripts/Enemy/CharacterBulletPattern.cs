using UnityEngine;
using DodgeDots.Bullet;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// 汉字弹幕运动模式
    /// </summary>
    public enum CharacterBulletMovementMode
    {
        Static,             // 静止不动
        UniformDirection,   // 统一方向移动
        ExpandOutward       // 朝外扩散
    }

    /// <summary>
    /// 汉字弹幕模式配置
    /// 存储汉字的弹幕位置数据
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterBulletPattern", menuName = "DodgeDots/Character Bullet Pattern")]
    public class CharacterBulletPattern : ScriptableObject
    {
        [Header("汉字信息")]
        [Tooltip("汉字内容")]
        public string character = "弹";

        [Tooltip("汉字描述")]
        public string description = "";

        [Header("弹幕配置")]
        [Tooltip("使用的子弹配置")]
        public BulletConfig bulletConfig;

        [Tooltip("弹幕位置数据（相对坐标，中心为(0,0)）")]
        public Vector2[] bulletPositions = new Vector2[0];

        [Header("显示设置")]
        [Tooltip("整体缩放比例")]
        [Range(0.1f, 10f)]
        public float scale = 1f;

        [Tooltip("生成延迟（秒，0表示同时生成所有弹幕）")]
        [Range(0f, 5f)]
        public float spawnDelay = 0f;

        [Header("运动设置")]
        [Tooltip("弹幕运动模式")]
        public CharacterBulletMovementMode movementMode = CharacterBulletMovementMode.Static;

        [Tooltip("统一运动方向（角度，0=右，90=上，180=左，270=下）")]
        [Range(0f, 360f)]
        public float uniformDirection = 270f;

        [Tooltip("统一运动速度")]
        public float uniformSpeed = 3f;

        [Tooltip("扩散速度（朝外扩散模式使用）")]
        public float expandSpeed = 2f;

        [Header("生成信息")]
        [Tooltip("弹幕总数")]
        public int bulletCount = 0;

        [Tooltip("生成时间戳")]
        public string generatedTime = "";

        /// <summary>
        /// 获取指定索引的弹幕位置（应用缩放）
        /// </summary>
        public Vector2 GetScaledPosition(int index)
        {
            if (index < 0 || index >= bulletPositions.Length)
                return Vector2.zero;

            return bulletPositions[index] * scale;
        }

        /// <summary>
        /// 获取所有弹幕位置（应用缩放）
        /// </summary>
        public Vector2[] GetAllScaledPositions()
        {
            Vector2[] scaledPositions = new Vector2[bulletPositions.Length];
            for (int i = 0; i < bulletPositions.Length; i++)
            {
                scaledPositions[i] = bulletPositions[i] * scale;
            }
            return scaledPositions;
        }
    }
}
