using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DodgeDots.Bullet;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// 示例Boss实现，展示如何使用Boss框架
    /// </summary>
    public class ExampleBoss : BossBase
    {
        [Header("攻击设置")]
        [SerializeField] private float attackInterval = 2f;
        [SerializeField] private int circlePatternBulletCount = 12;
        [SerializeField] private float bulletSpeed = 3f;
        [SerializeField] private float bulletDamage = 10f;

        private float _attackTimer;
        private BulletManager _bulletManager;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            // 在Start中获取BulletManager，确保BulletManager已经初始化
            _bulletManager = BulletManager.Instance;

            if (_bulletManager == null)
            {
                Debug.LogError("BulletManager.Instance 为 null！请确保场景中有 BulletManager 组件。");
            }
            else
            {
                Debug.Log("BulletManager 初始化成功");
            }
        }

        private void Update()
        {
            if (_currentState != BossState.Fighting) return;

            // 攻击计时器
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0)
            {
                PerformAttack();
                _attackTimer = attackInterval;
            }
        }

        protected override void OnBattleStart()
        {
            Debug.Log($"{bossName} 战斗开始！");
            _attackTimer = attackInterval;
        }

        protected override void OnPhaseEnter(int phase)
        {
            Debug.Log($"{bossName} 进入阶段 {phase}");

            // 根据阶段调整难度
            switch (phase)
            {
                case 1:
                    // 第二阶段：增加弹幕数量
                    circlePatternBulletCount = 16;
                    attackInterval = 1.5f;
                    break;
                case 2:
                    // 第三阶段：进一步增加难度
                    circlePatternBulletCount = 20;
                    attackInterval = 1f;
                    bulletSpeed = 4f;
                    break;
            }
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        private void PerformAttack()
        {
            Debug.Log($"PerformAttack 被调用，_bulletManager = {(_bulletManager == null ? "null" : "not null")}");

            if (_bulletManager == null)
            {
                Debug.LogError("_bulletManager 为 null，无法发射子弹！");
                return;
            }

            Debug.Log($"准备发射弹幕，阶段：{_currentPhase}，位置：{transform.position}");

            // 根据阶段使用不同的攻击模式
            switch (_currentPhase)
            {
                case 0:
                    // 第一阶段：简单圆形弹幕
                    _bulletManager.SpawnCirclePattern(
                        transform.position,
                        circlePatternBulletCount,
                        Core.Team.Enemy,
                        bulletSpeed,
                        bulletDamage
                    );
                    break;

                case 1:
                    // 第二阶段：旋转圆形弹幕
                    float rotationAngle = Time.time * 30f;
                    _bulletManager.SpawnCirclePattern(
                        transform.position,
                        circlePatternBulletCount,
                        Core.Team.Enemy,
                        bulletSpeed,
                        bulletDamage,
                        rotationAngle
                    );
                    break;

                case 2:
                    // 第三阶段：双重圆形弹幕
                    _bulletManager.SpawnCirclePattern(
                        transform.position,
                        circlePatternBulletCount,
                        Core.Team.Enemy,
                        bulletSpeed,
                        bulletDamage
                    );
                    _bulletManager.SpawnCirclePattern(
                        transform.position,
                        circlePatternBulletCount / 2,
                        Core.Team.Enemy,
                        bulletSpeed * 0.7f,
                        bulletDamage,
                        Time.time * 45f
                    );
                    break;
            }
        }

        protected override void OnBossDefeated()
        {
            base.OnBossDefeated();
            Debug.Log($"{bossName} 被击败！");

            // 停止所有攻击
            _attackTimer = float.MaxValue;
        }
    }
}
