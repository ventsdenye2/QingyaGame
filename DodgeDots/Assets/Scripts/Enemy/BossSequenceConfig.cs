using UnityEngine;
using DodgeDots.Bullet;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// 简化的Boss攻击数据（不包含移动参数）
    /// 定义单个攻击行为
    /// </summary>
    [System.Serializable]
    public class BossAttackAction
    {
        [Header("基础设置")]
        [Tooltip("攻击名称（用于调试）")]
        public string attackName = "Attack";

        [Header("发射源设置")]
        [Tooltip("从哪个发射源发射弹幕")]
        public EmitterType emitterType = EmitterType.MainCore;

        [Tooltip("是否同时从多个发射源发射")]
        public bool useMultipleEmitters = false;

        [Tooltip("多发射源列表")]
        public EmitterType[] multipleEmitters = new EmitterType[0];

        [Header("攻击类型")]
        [Tooltip("攻击类型")]
        public BossAttackType attackType = BossAttackType.Circle;

        [Tooltip("使用的子弹配置")]
        public BulletConfig bulletConfig;

        [Header("圆形弹幕参数")]
        [Tooltip("圆形弹幕的子弹数量")]
        public int circleCount = 12;

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

        [Tooltip("螺旋弹幕发射总时间（秒，0表示立即全部发射）")]
        public float spiralFireDuration = 0f;

        [Header("花型弹幕参数")]
        [Tooltip("花瓣数量")]
        public int flowerPetals = 5;

        [Tooltip("每个花瓣的子弹数量")]
        public int flowerBulletsPerPetal = 3;

        [Tooltip("花瓣扩散角度")]
        public float flowerPetalSpread = 30f;

        [Header("自机狙参数")]
        [Tooltip("自机狙的子弹数量")]
        public int aimingBulletCount = 1;

        [Tooltip("自机狙的扩散角度（0=精确瞄准）")]
        public float aimingSpreadAngle = 0f;

        [Tooltip("是否预判玩家移动")]
        public bool aimingPredictMovement = false;

        [Header("汉字弹幕参数")]
        [Tooltip("汉字弹幕模式配置")]
        public CharacterBulletPattern characterPattern;

        [Header("自定义攻击参数")]
        [Tooltip("自定义攻击的ID（用于在Boss子类中识别）")]
        public int customAttackId = 0;
    }

    /// <summary>
    /// Boss序列配置（ScriptableObject）
    /// 包含攻击序列和移动序列
    /// 由 BossSequenceController 订阅 BeatMapPlayer 的节拍事件来驱动
    /// 每个节拍执行下一个攻击和下一个移动
    /// </summary>
    [CreateAssetMenu(fileName = "BossSequenceConfig", menuName = "DodgeDots/Boss Sequence Config")]
    public class BossSequenceConfig : ScriptableObject
    {
        [Header("配置信息")]
        [Tooltip("配置名称")]
        public string configName = "Boss Sequence Config";

        [Header("攻击序列")]
        [Tooltip("攻击序列，每个节拍执行下一个攻击")]
        public BossAttackAction[] attackSequence;

        [Tooltip("是否循环执行攻击序列")]
        public bool loopAttackSequence = true;

        [Header("移动序列")]
        [Tooltip("移动序列，每个节拍执行下一个移动")]
        public EmitterMoveData[] moveSequence;

        [Tooltip("是否循环执行移动序列")]
        public bool loopMoveSequence = true;
    }
}
