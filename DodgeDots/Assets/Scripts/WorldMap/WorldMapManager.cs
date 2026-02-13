using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DodgeDots.Save;

namespace DodgeDots.WorldMap
{
    /// <summary>
    /// 世界地图管理器
    /// 管理关卡节点状态、解锁逻辑和进度保存
    /// </summary>
    public class WorldMapManager : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private WorldMapConfig mapConfig;

        [Header("场景中的节点")]
        [SerializeField] private LevelNode[] levelNodesInScene;

        private Dictionary<string, LevelNode> _nodeDict;
        private HashSet<string> _completedLevels;
        private HashSet<string> _unlockedLevels;
        private string _currentSelectedLevelId;

        private static WorldMapManager _instance;
        public static WorldMapManager Instance => _instance;

        public event Action<LevelNode> OnLevelSelected;
        public event Action<string> OnLevelCompleted;
        public event Action<string> OnLevelUnlocked;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            InitializeNodes();
            LoadProgress();
        }

        /// <summary>
        /// 初始化节点字典
        /// </summary>
        private void InitializeNodes()
        {
            _nodeDict = new Dictionary<string, LevelNode>();
            _completedLevels = new HashSet<string>();
            _unlockedLevels = new HashSet<string>();

            // 如果手动列表为空，则自动在场景中查找所有 LevelNode
            if (levelNodesInScene == null || levelNodesInScene.Length == 0)
            {
                levelNodesInScene = FindObjectsByType<LevelNode>(FindObjectsSortMode.None);
            }

            foreach (var node in levelNodesInScene)
            {
                // 增加判空保护，防止配置了空数据报错
                if (node != null && node.NodeData != null && !string.IsNullOrEmpty(node.LevelId))
                {
                    if (!_nodeDict.ContainsKey(node.LevelId))
                    {
                        _nodeDict.Add(node.LevelId, node);
                    }
                    else
                    {
                        Debug.LogError($"[WorldMapManager] 重复的关卡ID: {node.LevelId}，请检查 LevelNodeData 配置！");
                    }

                    node.OnNodeClicked += HandleNodeClicked;
                }
            }
        }

        /// <summary>
        /// 加载进度
        /// </summary>
        /// <summary>
        /// 加载进度
        /// </summary>
        private void LoadProgress()
        {
            SaveSystem.LoadOrCreate();
            _completedLevels.Clear();
            _unlockedLevels.Clear();

            // 1. 加载已完成关卡
            if (SaveSystem.Current.completedLevels != null)
            {
                foreach (var levelId in SaveSystem.Current.completedLevels)
                {
                    if (!string.IsNullOrEmpty(levelId)) _completedLevels.Add(levelId);
                }
            }

            // 2. 加载已解锁关卡
            if (SaveSystem.Current.unlockedLevels != null)
            {
                foreach (var levelId in SaveSystem.Current.unlockedLevels)
                {
                    if (!string.IsNullOrEmpty(levelId)) _unlockedLevels.Add(levelId);
                }
            }

            // 3. 确保初始关卡解锁
            if (mapConfig != null && !string.IsNullOrEmpty(mapConfig.initialUnlockedLevelId))
            {
                UnlockLevel(mapConfig.initialUnlockedLevelId);
            }

            // 4. 把所有“已完成”的也合并进“已解锁” (兜底逻辑)
            foreach (var levelId in _completedLevels)
            {
                _unlockedLevels.Add(levelId);
            }

            UpdateAllNodeStates();
        }

        /// <summary>
        /// 处理节点点击
        /// </summary>
        private void HandleNodeClicked(LevelNode node)
        {
            if (node == null || node.NodeData == null) return;

            _currentSelectedLevelId = node.LevelId;
            OnLevelSelected?.Invoke(node);

            // 加载关卡场景
            if (!string.IsNullOrEmpty(node.NodeData.sceneName))
            {
                // 离开大世界前强制存档 ---
                SaveProgress();
                // 使用加载管理器进行异步跳转
                DodgeDots.UI.LoadingManager.Instance.LoadScene(node.NodeData.sceneName);
            }
        }

        /// <summary>
        /// 解锁关卡
        /// </summary>
        public void UnlockLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            if (_unlockedLevels.Contains(levelId)) return;

            _unlockedLevels.Add(levelId);
            OnLevelUnlocked?.Invoke(levelId);

            if (_nodeDict.TryGetValue(levelId, out var node))
            {
                node.SetState(LevelNodeState.Unlocked);
            }
        }

        /// <summary>
        /// 完成关卡
        /// </summary>
        public void CompleteLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            if (_completedLevels.Contains(levelId)) return;

            _completedLevels.Add(levelId);
            _unlockedLevels.Add(levelId); // 完成了自然算解锁
            OnLevelCompleted?.Invoke(levelId);

            // 更新节点状态
            if (_nodeDict.TryGetValue(levelId, out var node))
            {
                node.SetState(LevelNodeState.Completed);

                // 注释掉自动解锁下一关的逻辑
                // 由 GeneralInteraction 控制，这里就不要了
                /*
                if (node.NextNodes != null)
                {
                    foreach (var nextNode in node.NextNodes)
                    {
                        if (nextNode != null)
                        {
                            UnlockLevel(nextNode.LevelId);
                        }
                    }
                }
                */
            }

            SaveProgress();
        }

        /// <summary>
        /// 保存进度
        /// </summary>
        private void SaveProgress()
        {
            if (SaveSystem.Current == null)
            {
                SaveSystem.LoadOrCreate();
            }

            // 保存已完成列表
            SaveSystem.Current.completedLevels.Clear();
            SaveSystem.Current.completedLevels.AddRange(_completedLevels);

            // 【关键修复】保存已解锁列表
            SaveSystem.Current.unlockedLevels.Clear();
            SaveSystem.Current.unlockedLevels.AddRange(_unlockedLevels);

            SaveSystem.Current.lastScene = SceneManager.GetActiveScene().name;

            // --- 保存玩家精确坐标 ---
            // 尝试在场景中找到玩家控制器
            var playerController = FindFirstObjectByType<PlayerWorldMapController>();
            if (playerController != null)
            {
                Vector3 pos = playerController.transform.position;
                SaveSystem.Current.playerPosX = pos.x;
                SaveSystem.Current.playerPosY = pos.y;
                SaveSystem.Current.playerPosZ = pos.z;
                SaveSystem.Current.hasSavedPosition = true;

                Debug.Log($"[WorldMapManager] 已保存玩家坐标: {pos}");
            }
            SaveSystem.Save();
        }

        private void RebuildUnlocksFromCompleted()
        {
            foreach (var levelId in _completedLevels)
            {
                if (string.IsNullOrEmpty(levelId)) continue;

                _unlockedLevels.Add(levelId);

                // 加载存档时也不要自动推导下一关
                /*
                if (_nodeDict.TryGetValue(levelId, out var node) && node != null && node.NextNodes != null)
                {
                    foreach (var nextNode in node.NextNodes)
                    {
                        if (nextNode != null)
                        {
                            UnlockLevel(nextNode.LevelId);
                        }
                    }
                }
                */
            }
        }

        /// <summary>
        /// 更新所有节点状态
        /// </summary>
        private void UpdateAllNodeStates()
        {
            foreach (var kvp in _nodeDict)
            {
                string levelId = kvp.Key;
                LevelNode node = kvp.Value;

                if (_completedLevels.Contains(levelId))
                {
                    node.SetState(LevelNodeState.Completed);
                }
                else if (_unlockedLevels.Contains(levelId))
                {
                    node.SetState(LevelNodeState.Unlocked);
                }
                else
                {
                    node.SetState(LevelNodeState.Locked);
                }
            }
        }

        /// <summary>
        /// 检查关卡是否已完成
        /// </summary>
        public bool IsLevelCompleted(string levelId)
        {
            return _completedLevels.Contains(levelId);
        }

        /// <summary>
        /// 检查关卡是否已解锁
        /// </summary>
        public bool IsLevelUnlocked(string levelId)
        {
            return _unlockedLevels.Contains(levelId);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            // 测试专用：按下 'C' 键直接完成 level_1
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("【测试】强制完成 level_1");
                CompleteLevel("level_1"); // 关卡ID，默认是 level_1
            }
            // 测试专用：按下 'V' 键直接完成 level_2
            if (Input.GetKeyDown(KeyCode.V))
            {
                LevelNode nearNode = null;
                if (levelNodesInScene != null)
                {
                    foreach (var node in levelNodesInScene)
                    {
                        if (node != null && node.IsPlayerNear && node.CurrentState != LevelNodeState.Locked)
                        {
                            nearNode = node;
                            break;
                        }
                    }
                }

                if (nearNode != null)
                {
                    Debug.Log($"【测试】强制完成 {nearNode.LevelId}");
                    CompleteLevel(nearNode.LevelId);
                }
            }

            // 测试专用：按下 'B' 键直接完成 level_3
            if (Input.GetKeyDown(KeyCode.B))
            {
                Debug.Log("【测试】强制完成 level_3");
                CompleteLevel("level_3"); // 关卡ID，默认是 level_1
            }

            // 测试专用：按下 'N' 键直接完成 level_beginner
            if (Input.GetKeyDown(KeyCode.N))
            {
                Debug.Log("【测试】强制完成 level_beginner");
                CompleteLevel("level_beginner"); // 关卡ID，默认是 level_1
            }
        }
    }
}
