using UnityEngine;

namespace DodgeDots.Bullet
{
    /// <summary>
    /// 子弹行为接口
    /// 用于实现不同的子弹特殊机制（反弹、追踪、分裂等）
    /// </summary>
    public interface IBulletBehavior
    {
        /// <summary>
        /// 初始化行为
        /// </summary>
        void Initialize(Bullet bullet);

        /// <summary>
        /// 每帧更新行为
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// 碰撞到边界时调用
        /// </summary>
        /// <param name="normal">边界法线</param>
        /// <returns>是否处理了碰撞（true表示不销毁子弹）</returns>
        bool OnBoundaryHit(Vector2 normal);

        /// <summary>
        /// 碰撞到目标时调用
        /// </summary>
        /// <param name="target">碰撞目标</param>
        /// <returns>是否处理了碰撞（true表示不销毁子弹）</returns>
        bool OnTargetHit(Collider2D target);

        /// <summary>
        /// 重置行为状态
        /// </summary>
        void Reset();
    }
}
