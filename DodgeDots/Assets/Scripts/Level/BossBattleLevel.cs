using System;
using UnityEngine;
using DodgeDots.Enemy;
using DodgeDots.Player;
using DodgeDots.Bullet;

namespace DodgeDots.Level
{
    /// <summary>
    /// Boss战关卡管理，负责Boss战的流程控制
    /// </summary>
    public class BossBattleLevel : MonoBehaviour
    {
        [Header("关卡引用")]
        [SerializeField] private BossBase boss;
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerWeapon playerWeapon;

        [Header("关卡设置")]
        [SerializeField] private bool autoStartBattle = true;
        [SerializeField] private float battleStartDelay = 1f;

        private bool _battleStarted;
        private bool _battleEnded;

        public event Action OnBattleStart;
        public event Action OnBattleWin;
        public event Action OnBattleLose;

        private void Start()
        {
            InitializeBattle();

            if (autoStartBattle)
            {
                Invoke(nameof(StartBattle), battleStartDelay);
            }
        }

        /// <summary>
        /// 初始化战斗
        /// </summary>
        private void InitializeBattle()
        {
            // 设置玩家武器目标
            if (playerWeapon != null && boss != null)
            {
                playerWeapon.SetTarget(boss.transform);
            }

            // 订阅事件
            if (boss != null)
            {
                boss.OnDeath += OnBossDefeated;
            }

            if (playerHealth != null)
            {
                playerHealth.OnDeath += OnPlayerDeath;
            }
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartBattle()
        {
            if (_battleStarted) return;

            _battleStarted = true;
            OnBattleStart?.Invoke();

            // 启动Boss战斗
            if (boss != null)
            {
                boss.StartBattle();
            }

            Debug.Log("Boss战开始！");
        }

        /// <summary>
        /// Boss被击败
        /// </summary>
        private void OnBossDefeated()
        {
            if (_battleEnded) return;

            _battleEnded = true;
            OnBattleWin?.Invoke();

            Debug.Log("胜利！Boss被击败！");

            // 清空所有弹幕
            if (BulletManager.Instance != null)
            {
                BulletManager.Instance.ClearAllBullets();
            }
        }

        /// <summary>
        /// 玩家死亡
        /// </summary>
        private void OnPlayerDeath()
        {
            if (_battleEnded) return;

            _battleEnded = true;
            OnBattleLose?.Invoke();

            Debug.Log("失败！玩家被击败！");
        }

        /// <summary>
        /// 重置战斗
        /// </summary>
        public void ResetBattle()
        {
            _battleStarted = false;
            _battleEnded = false;

            // 重置Boss
            if (boss != null)
            {
                boss.ResetHealth();
            }

            // 重置玩家
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }

            // 清空弹幕
            if (BulletManager.Instance != null)
            {
                BulletManager.Instance.ClearAllBullets();
            }
        }

        private void OnDestroy()
        {
            // 取消订阅事件
            if (boss != null)
            {
                boss.OnDeath -= OnBossDefeated;
            }

            if (playerHealth != null)
            {
                playerHealth.OnDeath -= OnPlayerDeath;
            }
        }
    }
}
