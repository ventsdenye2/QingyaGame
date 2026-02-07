using System.Collections.Generic;
using UnityEngine;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// 示例Boss实现，展示如何使用新的Boss序列系统
    /// 使用BossSequenceController和BossSequenceConfig配置攻击和移动
    /// 支持多个序列控制器同时运行，实现复杂的Boss行为
    /// </summary>
    public class ExampleBoss : BossBase
    {
        [Header("阶段序列控制器配置")]
        [Tooltip("阶段0（初始阶段）的序列控制器列表")]
        [SerializeField] private List<BossSequenceController> phase0Controllers = new List<BossSequenceController>();

        [Tooltip("阶段1的序列控制器列表")]
        [SerializeField] private List<BossSequenceController> phase1Controllers = new List<BossSequenceController>();

        [Tooltip("阶段2的序列控制器列表")]
        [SerializeField] private List<BossSequenceController> phase2Controllers = new List<BossSequenceController>();

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            // 攻击和移动现在由BossSequenceController驱动
            // 每个Controller订阅BeatMapPlayer的节拍事件
        }

        protected override void OnBattleStart()
        {
            Debug.Log($"{bossName} 战斗开始！使用新的序列系统");
            // 启用阶段0的所有控制器
            EnableControllers(phase0Controllers);
        }

        protected override void OnPhaseEnter(int phase)
        {
            Debug.Log($"{bossName} 进入阶段 {phase}");

            // 禁用所有控制器
            DisableAllControllers();

            // 根据阶段启用对应的控制器
            switch (phase)
            {
                case 0:
                    EnableControllers(phase0Controllers);
                    Debug.Log("启用阶段0的序列控制器");
                    break;

                case 1:
                    EnableControllers(phase1Controllers);
                    Debug.Log("启用阶段1的序列控制器");
                    break;

                case 2:
                    EnableControllers(phase2Controllers);
                    Debug.Log("启用阶段2的序列控制器");
                    break;
            }
        }

        /// <summary>
        /// 启用指定的控制器列表
        /// </summary>
        private void EnableControllers(List<BossSequenceController> controllers)
        {
            foreach (var controller in controllers)
            {
                if (controller != null)
                {
                    controller.enabled = true;
                    controller.ResetSequence();
                }
            }
        }

        /// <summary>
        /// 禁用所有控制器
        /// </summary>
        private void DisableAllControllers()
        {
            DisableControllers(phase0Controllers);
            DisableControllers(phase1Controllers);
            DisableControllers(phase2Controllers);
        }

        /// <summary>
        /// 禁用指定的控制器列表
        /// </summary>
        private void DisableControllers(List<BossSequenceController> controllers)
        {
            foreach (var controller in controllers)
            {
                if (controller != null)
                {
                    controller.enabled = false;
                }
            }
        }

        protected override void OnBossDefeated()
        {
            base.OnBossDefeated();
            Debug.Log($"{bossName} 被击败！");
            // 禁用所有控制器
            DisableAllControllers();
        }
    }
}
