namespace DodgeDots.Enemy
{
    /// <summary>
    /// Boss发射源类型枚举
    /// 用于标识Boss身上不同的弹幕发射点
    /// </summary>
    public enum EmitterType
    {
        MainCore,       // 核心/身体中心
        LeftHand,       // 左手
        RightHand,      // 右手
        LeftWing,       // 左翼
        RightWing,      // 右翼
        Head,           // 头部
        Tail,           // 尾部
        Custom1,        // 自定义发射点1
        Custom2,        // 自定义发射点2
        Custom3,        // 自定义发射点3
        Custom4,        // 自定义发射点4
        Custom5         // 自定义发射点5
    }
}
