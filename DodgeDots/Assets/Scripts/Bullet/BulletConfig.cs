using UnityEngine;

namespace DodgeDots.Bullet
{
    /// <summary>
    /// 子弹配置数据（ScriptableObject）
    /// 用于定义不同类型子弹的属性
    /// </summary>
    [CreateAssetMenu(fileName = "BulletConfig", menuName = "DodgeDots/Bullet Config")]
    public class BulletConfig : ScriptableObject
    {
        [Header("基础属性")]
        [Tooltip("子弹类型名称")]
        public string bulletName = "Normal Bullet";

        [Tooltip("子弹精灵图")]
        public Sprite sprite;

        [Tooltip("子弹颜色")]
        public Color color = Color.white;

        [Tooltip("子弹缩放")]
        public Vector2 scale = Vector2.one;

        [Header("战斗属性")]
        [Tooltip("默认速度")]
        public float defaultSpeed = 5f;

        [Tooltip("默认伤害")]
        public float defaultDamage = 10f;

        [Tooltip("生命周期（秒，0表示无限）")]
        public float lifetime = 10f;

        [Header("碰撞属性")]
        [Tooltip("碰撞器大小")]
        public float colliderRadius = 0.1f;

        [Tooltip("是否穿透（不会在碰撞后消失）")]
        public bool isPiercing = false;

        [Tooltip("最大穿透次数（仅当isPiercing为true时有效，0表示无限）")]
        public int maxPierceCount = 0;

        [Header("视觉效果")]
        [Tooltip("旋转速度（度/秒）")]
        public float rotationSpeed = 0f;

        [Tooltip("是否朝向移动方向")]
        public bool faceDirection = true;

        [Tooltip("排序层")]
        public string sortingLayer = "Default";

        [Tooltip("排序顺序")]
        public int sortingOrder = 0;
    }
}
