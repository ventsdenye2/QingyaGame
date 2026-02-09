using UnityEngine;
using DodgeDots.Bullet;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// Boss攻击类型枚举
    /// </summary>
    public enum BossAttackType
    {
        Circle,         // 圆形弹幕
        Fan,            // 扇形弹幕
        Single,         // 单发子弹
        Spiral,         // 螺旋弹幕
        Flower,         // 花型弹幕
        Aiming,         // 自机狙（瞄准玩家）
        Custom          // 自定义攻击（需要在Boss子类中实现）
    }

    /// <summary>
    /// Boss移动类型枚举
    /// </summary>
    public enum BossMoveType
    {
        None,           // 不移动
        ToPosition,     // 移动到目标位置
        ByDirection,    // 沿方向移动
        Circle,         // 圆形移动
        TwoPointLoop,   // 两点循环移动
        Custom          // 自定义移动（需要在Boss子类中实现）
    }

    /// <summary>
    /// Boss阶段配置数据
    /// 定义Boss进入某个阶段时的文案和其他配置
    /// </summary>
    [System.Serializable]
    public class BossPhaseData
    {
        [Header("阶段信息")]
        [Tooltip("阶段ID（0=初始阶段，1=第一阶段，2=第二阶段...）")]
        public int phaseId = 0;

        [Tooltip("阶段名称")]
        public string phaseName = "Phase";

        [Header("文案设置")]
        [Tooltip("阶段文案内容")]
        [TextArea(3, 5)]
        public string dialogueText = "";

        [Tooltip("文案显示时长（秒，0表示需要手动关闭）")]
        public float dialogueDuration = 3f;

        [Tooltip("是否暂停游戏显示文案")]
        public bool pauseGameForDialogue = false;
    }

    /// <summary>
    /// 子攻击数据
    /// 定义单个攻击模式的具体参数
    /// </summary>
    [System.Serializable]
    public class SubAttackData
    {
        [Header("攻击类型")]
        [Tooltip("攻击类型")]
        public BossAttackType attackType = BossAttackType.Circle;

        [Tooltip("使用的子弹配置")]
        public BulletConfig bulletConfig;

        [Header("圆形弹幕参数")]
        [Tooltip("圆形弹幕的子弹数量")]
        public int circleCount = 12;

        [Tooltip("圆形弹幕的起始角度")]
        public float circleStartAngle = 0f;

        [Header("扇形弹幕参数")]
        [Tooltip("扇形弹幕的子弹数量")]
        public int fanCount = 5;

        [Tooltip("扇形弹幕的扩散角度")]
        public float fanSpreadAngle = 60f;

        [Tooltip("扇形弹幕的中心方向（0=右，90=上，180=左，270=下）")]
        public float fanCenterAngle = 270f;

        [Header("单发子弹参数")]
        [Tooltip("单发子弹的方向角度（0=右，90=上，180=左，270=下）")]
        public float singleDirection = 270f;

        [Header("螺旋弹幕参数")]
        [Tooltip("螺旋的总子弹数量")]
        public int spiralBulletCount = 36;

        [Tooltip("螺旋的圈数")]
        public float spiralTurns = 3f;

        [Tooltip("螺旋起始角度")]
        public float spiralStartAngle = 0f;

        [Tooltip("螺旋半径增长速度")]
        public float spiralRadiusGrowth = 0.5f;

        [Header("花型弹幕参数")]
        [Tooltip("花瓣数量")]
        public int flowerPetals = 5;

        [Tooltip("每个花瓣的子弹数量")]
        public int flowerBulletsPerPetal = 3;

        [Tooltip("花瓣扩散角度")]
        public float flowerPetalSpread = 30f;

        [Tooltip("花型起始角度")]
        public float flowerStartAngle = 0f;

        [Header("自机狙参数")]
        [Tooltip("自机狙的子弹数量")]
        public int aimingBulletCount = 1;

        [Tooltip("自机狙的扩散角度（0=精确瞄准）")]
        public float aimingSpreadAngle = 0f;

        [Tooltip("是否预判玩家移动")]
        public bool aimingPredictMovement = false;

        [Header("自定义攻击参数")]
        [Tooltip("自定义攻击的ID（用于在Boss子类中识别）")]
        public int customAttackId = 0;
    }

    /// <summary>
    /// 发射源移动数据
    /// 定义单个发射源的移动行为
    /// </summary>
    [System.Serializable]
    public class EmitterMoveData
    {
        [Header("发射源")]
        [Tooltip("要移动的发射源")]
        public EmitterType emitterType = EmitterType.MainCore;

        [Tooltip("是否同时移动多个发射源")]
        public bool useMultipleEmitters = false;

        [Tooltip("多发射源列表（仅当useMultipleEmitters为true时有效）")]
        public EmitterType[] multipleEmitters = new EmitterType[0];

        [Header("移动设置")]
        [Tooltip("移动类型")]
        public BossMoveType moveType = BossMoveType.None;

        [Tooltip("移动持续时间（秒）")]
        public float moveDuration = 1f;

        [Tooltip("移动速度")]
        public float moveSpeed = 5f;

        [Tooltip("目标位置（仅当moveType为ToPosition时有效）")]
        public Vector2 targetPosition = Vector2.zero;

        [Tooltip("移动方向（仅当moveType为ByDirection时有效，0=右，90=上，180=左，270=下）")]
        public float moveDirection = 0f;

        [Tooltip("移动距离（仅当moveType为ByDirection时有效）")]
        public float moveDistance = 5f;

        [Tooltip("自定义移动ID（仅当moveType为Custom时有效）")]
        public int customMoveId = 0;

        [Header("坐标空间")]
        [Tooltip("是否使用相对Boss的局部坐标（仅对ToPosition/Circle/TwoPointLoop有效）")]
        public bool useLocalSpace = true;

        [Header("圆形移动参数")]
        [Tooltip("圆心位置（世界坐标）")]
        public Vector2 circleCenter = Vector2.zero;

        [Tooltip("圆形半径")]
        public float circleRadius = 3f;

        [Tooltip("角速度（度/秒）")]
        public float circleAngularSpeed = 180f;

        [Tooltip("起始角度（度）")]
        public float circleStartAngle = 0f;

        [Tooltip("是否顺时针")]
        public bool circleClockwise = true;

        [Header("两点循环参数")]
        [Tooltip("点A（世界坐标）")]
        public Vector2 pointA = new Vector2(-3f, 0f);

        [Tooltip("点B（世界坐标）")]
        public Vector2 pointB = new Vector2(3f, 0f);

        [Tooltip("循环速度（单位/秒）")]
        public float loopSpeed = 3f;

        [Tooltip("是否从点A开始")]
        public bool startFromA = true;
    }

    /// <summary>
    /// Boss单个攻击数据
    /// 定义了Boss的一次攻击行为
    /// </summary>
    [System.Serializable]
    public class BossAttackData
    {
        [Header("基础设置")]
        [Tooltip("攻击模式名称（用于调试）")]
        public string attackName = "Attack";

        [Tooltip("执行此攻击前的延迟时间（秒）")]
        public float delayBeforeAttack = 1f;

        [Header("发射源设置")]
        [Tooltip("从哪个发射源发射弹幕")]
        public EmitterType emitterType = EmitterType.MainCore;

        [Tooltip("是否同时从多个发射源发射")]
        public bool useMultipleEmitters = false;

        [Tooltip("多发射源列表（仅当useMultipleEmitters为true时有效）")]
        public EmitterType[] multipleEmitters = new EmitterType[0];

        [Header("组合攻击设置")]
        [Tooltip("是否使用组合攻击（同时发射多种弹幕模式）")]
        public bool useComboAttack = false;

        [Tooltip("子攻击列表（仅当useComboAttack为true时有效）")]
        public SubAttackData[] subAttacks = new SubAttackData[0];

        [Header("发射源移动设置")]
        [Tooltip("是否移动发射源（而不是Boss主体）")]
        public bool moveEmitters = false;

        [Tooltip("发射源移动配置列表（仅当moveEmitters为true时有效）")]
        public EmitterMoveData[] emitterMoves = new EmitterMoveData[0];

        [Header("攻击类型（单一攻击模式）")]
        [Tooltip("攻击类型")]
        public BossAttackType attackType = BossAttackType.Circle;

        [Tooltip("使用的子弹配置")]
        public BulletConfig bulletConfig;

        [Header("圆形弹幕参数")]
        [Tooltip("圆形弹幕的子弹数量")]
        public int circleCount = 12;

        [Tooltip("圆形弹幕的起始角度")]
        public float circleStartAngle = 0f;

        [Header("扇形弹幕参数")]
        [Tooltip("扇形弹幕的子弹数量")]
        public int fanCount = 5;

        [Tooltip("扇形弹幕的扩散角度")]
        public float fanSpreadAngle = 60f;

        [Tooltip("扇形弹幕的中心方向（0=右，90=上，180=左，270=下）")]
        public float fanCenterAngle = 270f;

        [Header("单发子弹参数")]
        [Tooltip("单发子弹的方向角度（0=右，90=上，180=左，270=下）")]
        public float singleDirection = 270f;

        [Header("螺旋弹幕参数")]
        [Tooltip("螺旋的总子弹数量")]
        public int spiralBulletCount = 36;

        [Tooltip("螺旋的圈数")]
        public float spiralTurns = 3f;

        [Tooltip("螺旋起始角度")]
        public float spiralStartAngle = 0f;

        [Tooltip("螺旋半径增长速度")]
        public float spiralRadiusGrowth = 0.5f;

        [Header("花型弹幕参数")]
        [Tooltip("花瓣数量")]
        public int flowerPetals = 5;

        [Tooltip("每个花瓣的子弹数量")]
        public int flowerBulletsPerPetal = 3;

        [Tooltip("花瓣扩散角度")]
        public float flowerPetalSpread = 30f;

        [Tooltip("花型起始角度")]
        public float flowerStartAngle = 0f;

        [Header("自机狙参数")]
        [Tooltip("自机狙的子弹数量")]
        public int aimingBulletCount = 1;

        [Tooltip("自机狙的扩散角度（0=精确瞄准）")]
        public float aimingSpreadAngle = 0f;

        [Tooltip("是否预判玩家移动")]
        public bool aimingPredictMovement = false;

        [Header("自定义攻击参数")]
        [Tooltip("自定义攻击的ID（用于在Boss子类中识别）")]
        public int customAttackId = 0;

        [Header("移动设置")]
        [Tooltip("移动类型")]
        public BossMoveType moveType = BossMoveType.None;

        [Tooltip("移动持续时间（秒）")]
        public float moveDuration = 1f;

        [Tooltip("移动速度")]
        public float moveSpeed = 5f;

        [Tooltip("目标位置（仅当moveType为ToPosition时有效）")]
        public Vector2 targetPosition = Vector2.zero;

        [Tooltip("移动方向（仅当moveType为ByDirection时有效，0=右，90=上，180=左，270=下）")]
        public float moveDirection = 0f;

        [Tooltip("移动距离（仅当moveType为ByDirection时有效）")]
        public float moveDistance = 5f;

        [Tooltip("自定义移动ID（仅当moveType为Custom时有效）")]
        public int customMoveId = 0;
    }

    /// <summary>
    /// Boss攻击配置（ScriptableObject）
    /// 定义Boss的完整攻击序列，Boss会循环执行这些攻击
    /// </summary>
    [CreateAssetMenu(fileName = "BossAttackConfig", menuName = "DodgeDots/Boss Attack Config")]
    public class BossAttackConfig : ScriptableObject
    {
        [Header("配置信息")]
        [Tooltip("配置名称")]
        public string configName = "Boss Attack Config";

        [Header("音频设置")]
        [Tooltip("背景音乐（在攻击循环开始时播放）")]
        public AudioClip backgroundMusic;

        [Tooltip("音乐音量（0-1）")]
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;

        [Tooltip("是否循环播放音乐")]
        public bool loopMusic = true;

        [Header("攻击序列")]
        [Tooltip("Boss的攻击序列，会按顺序循环执行")]
        public BossAttackData[] attackSequence;

        [Header("循环设置")]
        [Tooltip("是否循环执行攻击序列")]
        public bool loopSequence = true;

        [Tooltip("完成一轮攻击序列后的延迟时间（秒）")]
        public float delayAfterLoop = 2f;
    }
}
