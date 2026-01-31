using UnityEngine;

namespace DodgeDots.Core
{
    /// <summary>
    /// 可受伤接口，所有可以受到伤害的对象都应实现此接口
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="source">伤害来源</param>
        void TakeDamage(float damage, GameObject source = null);

        /// <summary>
        /// 是否可以受到伤害（例如无敌状态时返回false）
        /// </summary>
        bool CanTakeDamage { get; }
    }
}
