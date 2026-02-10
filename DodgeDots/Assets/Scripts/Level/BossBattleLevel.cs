using System;
using UnityEngine;
using DodgeDots.Enemy;
using DodgeDots.Player;
using DodgeDots.Bullet;
using DodgeDots.Save;

namespace DodgeDots.Level
{
    /// <summary>
    /// Boss战关卡管理，负责Boss战的流程控制
    /// </summary>
    public class BossBattleLevel : MonoBehaviour
    {
        [Header("关卡ID")]
        [Tooltip("这个场景对应的关卡ID，必须和WorldMapConfig里的一致")]
        [SerializeField] private string currentLevelId = "level_1";

        [Header("关卡引用")]
        [SerializeField] private BossBase boss;
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerWeapon playerWeapon;

        [Header("关卡设置")]
        [SerializeField] private bool autoStartBattle = true;
        [SerializeField] private float battleStartDelay = 1f;
        [SerializeField] private float playerDeathDelay = 2f; // 延迟检查玩家失败（允许复活）

        private bool _battleStarted;
        private bool _battleEnded;
        private Coroutine _deathCheckCoroutine;

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

            // --- 核心：写入存档 ---
            // 获取当前关卡ID
            string levelId = "level_1"; // <--- 确保这里填的是正确的 ID，最好做成 [SerializeField] 变量

            // 2. 写入 SaveSystem
            if (SaveSystem.Current == null) SaveSystem.LoadOrCreate();

            if (!SaveSystem.Current.completedLevels.Contains(levelId))
            {
                SaveSystem.Current.completedLevels.Add(levelId);
                Debug.Log($"[BossLevel] 关卡 {levelId} 进度已记录。");
            }

            // 3. 强制保存文件
            SaveSystem.Save();

            OnBattleWin?.Invoke();

            Debug.Log("胜利！Boss被击败！");

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

            Debug.Log("玩家死亡，等待复活机制检查...");

            // 延迟检查，给复活机制时间处理
            if (_deathCheckCoroutine != null)
            {
                StopCoroutine(_deathCheckCoroutine);
            }
            _deathCheckCoroutine = StartCoroutine(CheckPlayerDeathDelayed());
        }

        /// <summary>
        /// 延迟检查玩家是否真的失败了
        /// </summary>
        private System.Collections.IEnumerator CheckPlayerDeathDelayed()
        {
            yield return new WaitForSeconds(playerDeathDelay);

            // 如果在延迟期间玩家复活了，则不结束战斗
            if (playerHealth != null && playerHealth.IsAlive)
            {
                Debug.Log("玩家已复活，战斗继续！");
                _deathCheckCoroutine = null;
                yield break;
            }

            // 玩家真的死亡了
            if (!_battleEnded)
            {
                _battleEnded = true;
                OnBattleLose?.Invoke();
                Debug.Log("失败！玩家被击败！");
            }

            _deathCheckCoroutine = null;
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

        /// <summary>
        /// 重新开始战斗（玩家复活后调用）
        /// </summary>
        public void ResumeBattle()
        {
            // 重置战斗结束标志，允许战斗继续
            if (_deathCheckCoroutine != null)
            {
                StopCoroutine(_deathCheckCoroutine);
                _deathCheckCoroutine = null;
            }
            Debug.Log("战斗已恢复");
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

            if (_deathCheckCoroutine != null)
            {
                StopCoroutine(_deathCheckCoroutine);
            }
        }
    }
}
