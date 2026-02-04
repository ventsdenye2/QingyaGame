using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DodgeDots.Bullet;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// 示例Boss实现，展示如何使用Boss框架和多发射源系统
    /// 使用BossAttackConfig配置攻击序列，支持多发射源
    /// </summary>
    public class ExampleBoss : BossBase
    {
        [Header("阶段配置")]
        [Tooltip("第二阶段的攻击配置（可选）")]
        [SerializeField] private BossAttackConfig phase1AttackConfig;

        [Tooltip("第三阶段的攻击配置（可选）")]
        [SerializeField] private BossAttackConfig phase2AttackConfig;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            // 攻击逻辑现在由BossAttackConfig配置驱动
        }

        protected override void OnBattleStart()
        {
            Debug.Log($"{bossName} 战斗开始！使用配置驱动的攻击系统");
            // 攻击循环由BossBase自动启动，使用attackConfig配置
        }

        protected override void OnPhaseEnter(int phase)
        {
            Debug.Log($"{bossName} 进入阶段 {phase}");

            // 根据阶段切换攻击配置
            switch (phase)
            {
                case 1:
                    // 第二阶段：使用phase1AttackConfig（如果配置了）
                    if (phase1AttackConfig != null)
                    {
                        attackConfig = phase1AttackConfig;
                        RestartAttackLoop();
                        Debug.Log("切换到第二阶段攻击配置");
                    }
                    break;

                case 2:
                    // 第三阶段：使用phase2AttackConfig（如果配置了）
                    if (phase2AttackConfig != null)
                    {
                        attackConfig = phase2AttackConfig;
                        RestartAttackLoop();
                        Debug.Log("切换到第三阶段攻击配置");
                    }
                    break;
            }
        }

        /// <summary>
        /// 重启攻击循环（用于切换攻击配置）
        /// </summary>
        private void RestartAttackLoop()
        {
            StopAttackLoop();
            _currentAttackIndex = 0;
            if (attackConfig != null && _currentState == BossState.Fighting)
            {
                _attackCoroutine = StartCoroutine(AttackLoopCoroutine());
            }
        }

        protected override void OnBossDefeated()
        {
            base.OnBossDefeated();
            Debug.Log($"{bossName} 被击败！");
            // 攻击循环已由base.OnBossDefeated()自动停止
        }
    }
}
