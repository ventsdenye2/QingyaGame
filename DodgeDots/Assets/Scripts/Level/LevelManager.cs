using System;
using UnityEngine;
using DodgeDots.Core;
using DodgeDots.Player;

namespace DodgeDots.Level
{
    /// <summary>
    /// 关卡管理器，负责场景切换和关卡状态管理
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("关卡设置")]
        [SerializeField] private LevelType currentLevelType = LevelType.BossBattle;
        [SerializeField] private GameConfig gameConfig;

        [Header("玩家引用")]
        [SerializeField] private PlayerController playerController;

        private static LevelManager _instance;
        public static LevelManager Instance => _instance;

        public LevelType CurrentLevelType => currentLevelType;

        public event Action<LevelType> OnLevelTypeChanged;

        private void Awake()
        {
            // 单例模式
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // 从配置加载设置
            if (gameConfig == null)
            {
                gameConfig = Resources.Load<GameConfig>("GameConfig");
            }
        }

        private void Start()
        {
            // 初始化当前关卡类型
            ApplyLevelType(currentLevelType);
        }

        /// <summary>
        /// 切换关卡类型
        /// </summary>
        public void SwitchLevelType(LevelType newType)
        {
            if (currentLevelType == newType) return;

            currentLevelType = newType;
            ApplyLevelType(newType);
            OnLevelTypeChanged?.Invoke(newType);
        }

        /// <summary>
        /// 应用关卡类型设置
        /// </summary>
        private void ApplyLevelType(LevelType type)
        {
            if (playerController == null) return;

            switch (type)
            {
                case LevelType.BossBattle:
                    // Boss战：启用边界限制
                    playerController.SetBoundsRestriction(true);
                    if (gameConfig != null)
                    {
                        playerController.SetBounds(gameConfig.bossBattleBounds);
                    }
                    break;

                case LevelType.OpenWorld:
                    // 大世界：禁用边界限制
                    playerController.SetBoundsRestriction(false);
                    break;
            }
        }

        /// <summary>
        /// 设置玩家控制器引用
        /// </summary>
        public void SetPlayerController(PlayerController controller)
        {
            playerController = controller;
            ApplyLevelType(currentLevelType);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnDrawGizmos()
        {
            // 绘制Boss战边界
            if (currentLevelType == LevelType.BossBattle && gameConfig != null && gameConfig.showBoundaryGizmos)
            {
                Gizmos.color = Color.red;
                Vector2 bounds = gameConfig.bossBattleBounds;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(bounds.x, bounds.y, 0));
            }
        }
    }
}
