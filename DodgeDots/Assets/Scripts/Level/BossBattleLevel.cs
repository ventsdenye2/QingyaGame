using System;
using UnityEngine;
using DodgeDots.Enemy;
using DodgeDots.Player;
using DodgeDots.Bullet;
using DodgeDots.Audio;

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
        [SerializeField] private float playerDeathDelay = 2f; // 延迟检查玩家失败（允许复活）

        [Header("快进设置")]
        [Tooltip("快进倍速")]
        [SerializeField] private float fastForwardSpeed = 3f;

        private bool _battleStarted;
        private bool _battleEnded;
        private Coroutine _deathCheckCoroutine;
        private bool _isFastForwarding = false;
        private BGMManager _bgmManager;
        private float _normalTimeScale = 1f;
        private float _normalAudioPitch = 1f;

        public event Action OnBattleStart;
        public event Action OnBattleWin;
        public event Action OnBattleLose;

        private void Start()
        {
            InitializeBattle();

            // 查找BGMManager
            _bgmManager = FindObjectOfType<BGMManager>();

            if (autoStartBattle)
            {
                Invoke(nameof(StartBattle), battleStartDelay);
            }
        }

        private void Update()
        {
            // 检测K键切换快进
            if (Input.GetKeyDown(KeyCode.K))
            {
                ToggleFastForward();
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

        /// <summary>
        /// 切换快进模式
        /// </summary>
        private void ToggleFastForward()
        {
            _isFastForwarding = !_isFastForwarding;

            if (_isFastForwarding)
            {
                // 启用快进
                _normalTimeScale = Time.timeScale;
                Time.timeScale = fastForwardSpeed;

                // 修改BGM播放速度
                if (_bgmManager != null && _bgmManager.audioSource != null)
                {
                    _normalAudioPitch = _bgmManager.audioSource.pitch;
                    _bgmManager.audioSource.pitch = fastForwardSpeed;
                }

                Debug.Log($"[BossBattleLevel] 快进模式开启 ({fastForwardSpeed}x)");
            }
            else
            {
                // 恢复正常速度
                Time.timeScale = _normalTimeScale;

                // 恢复BGM播放速度
                if (_bgmManager != null && _bgmManager.audioSource != null)
                {
                    _bgmManager.audioSource.pitch = _normalAudioPitch;
                }

                Debug.Log("[BossBattleLevel] 快进模式关闭");
            }
        }

        private void OnDestroy()
        {
            // 恢复时间缩放和音频速度
            if (_isFastForwarding)
            {
                Time.timeScale = _normalTimeScale;
                if (_bgmManager != null && _bgmManager.audioSource != null)
                {
                    _bgmManager.audioSource.pitch = _normalAudioPitch;
                }
            }

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
