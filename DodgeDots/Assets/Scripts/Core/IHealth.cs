using System;
using UnityEngine;

namespace DodgeDots.Core
{
    /// <summary>
    /// 生命值接口，所有具有生命值的对象都应实现此接口
    /// </summary>
    public interface IHealth
    {
        /// <summary>
        /// 当前生命值
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// 最大生命值
        /// </summary>
        float MaxHealth { get; }

        /// <summary>
        /// 是否存活
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// 生命值变化事件 (当前生命值, 最大生命值)
        /// </summary>
        event Action<float, float> OnHealthChanged;

        /// <summary>
        /// 死亡事件
        /// </summary>
        event Action OnDeath;

        /// <summary>
        /// 恢复生命值
        /// </summary>
        void Heal(float amount);

        /// <summary>
        /// 重置生命值到最大值
        /// </summary>
        void ResetHealth();
    }
}
