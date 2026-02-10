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

        [Header("拖尾效果")]
        [Tooltip("是否启用拖尾")]
        public bool enableTrail = false;

        [Tooltip("拖尾持续时间")]
        public float trailTime = 0.2f;

        [Tooltip("拖尾起始宽度")]
        public float trailStartWidth = 0.1f;

        [Tooltip("拖尾结束宽度")]
        public float trailEndWidth = 0f;

        [Tooltip("拖尾最小顶点间距")]
        public float trailMinVertexDistance = 0.05f;

        [Tooltip("拖尾起始颜色")]
        public Color trailStartColor = new Color(1f, 1f, 1f, 0.6f);

        [Tooltip("拖尾结束颜色")]
        public Color trailEndColor = new Color(1f, 1f, 1f, 0f);

        [Tooltip("拖尾材质（可选）")]
        public Material trailMaterial;

        [Tooltip("拖尾使用子弹Sprite纹理")]
        public bool trailUseBulletSprite = true;

        [Tooltip("拖尾颜色跟随子弹颜色")]
        public bool trailUseBulletColor = true;

        [Tooltip("拖尾颜色变亮（0~1）")]
        [Range(0f, 1f)]
        public float trailColorLighten = 0.3f;

        [Tooltip("拖尾起始透明度（0~1）")]
        [Range(0f, 1f)]
        public float trailStartAlpha = 0.45f;

        [Tooltip("拖尾结束透明度（0~1）")]
        [Range(0f, 1f)]
        public float trailEndAlpha = 0f;

        [Header("第二拖尾")]
        [Tooltip("是否启用第二拖尾")]
        public bool enableSecondTrail = true;

        [Tooltip("第二拖尾持续时间")]
        public float secondTrailTime = 0.16f;

        [Tooltip("第二拖尾起始宽度")]
        public float secondTrailStartWidth = 0.08f;

        [Tooltip("第二拖尾结束宽度")]
        public float secondTrailEndWidth = 0f;

        [Tooltip("第二拖尾最小顶点间距")]
        public float secondTrailMinVertexDistance = 0.05f;

        [Tooltip("第二拖尾颜色变亮（0~1）")]
        [Range(0f, 1f)]
        public float secondTrailColorLighten = 0.5f;

        [Tooltip("第二拖尾起始透明度（0~1）")]
        [Range(0f, 1f)]
        public float secondTrailStartAlpha = 0.25f;

        [Tooltip("第二拖尾结束透明度（0~1）")]
        [Range(0f, 1f)]
        public float secondTrailEndAlpha = 0f;

        [Tooltip("第二拖尾材质（可选）")]
        public Material secondTrailMaterial;

        [Header("残影效果")]
        [Tooltip("是否启用残影")]
        public bool enableAfterimage = true;

        [Tooltip("残影生成间隔（秒）")]
        public float afterimageInterval = 0.04f;

        [Tooltip("残影持续时间（秒）")]
        public float afterimageLifetime = 0.22f;

        [Tooltip("残影颜色变亮（0~1）")]
        [Range(0f, 1f)]
        public float afterimageColorLighten = 0.4f;

        [Tooltip("残影起始透明度（0~1）")]
        [Range(0f, 1f)]
        public float afterimageStartAlpha = 0.35f;

        [Tooltip("残影结束透明度（0~1）")]
        [Range(0f, 1f)]
        public float afterimageEndAlpha = 0f;

        [Header("自动追踪设置")]
        [Tooltip("是否启用自动追踪行为（需要子弹预制体上挂有 AutoTargetHomingBulletBehavior）")]
        public bool enableAutoHoming = false;

        [Tooltip("追踪时的最大转向速度（度/秒），越大拐弯越快")]
        public float homingTurnSpeed = 360f;

        [Tooltip("自动索敌的搜索半径")]
        public float homingSearchRadius = 20f;

        [Tooltip("重新锁定目标的时间间隔（秒）")]
        public float homingRetargetInterval = 0.2f;

        [Tooltip("是否可以追踪 Boss（需要场景中物体 Tag 为 Boss）")]
        public bool homingTrackBoss = true;

        [Tooltip("是否可以追踪普通敌人（需要场景中物体 Tag 为 Enemy）")]
        public bool homingTrackEnemy = false;

        [Tooltip("是否可以追踪玩家（需要场景中物体 Tag 为 Player）")]
        public bool homingTrackPlayer = false;
    }
}
