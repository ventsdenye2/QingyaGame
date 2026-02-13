using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DodgeDots.Save;
using DodgeDots.Audio;

namespace DodgeDots.UI
{
    /// <summary>
    /// ESC 菜单：按下 ESC 显示/隐藏菜单（UI 必须是场景里的 GameObject，方便编辑）。
    /// - 大世界：可返回主界面
    /// - 关卡：可返回大世界或主界面
    /// 出现时背景变暗（通过一张全屏半透明 Image 实现）
    /// </summary>
    public class EscMenuController : MonoBehaviour
    {
        [Header("UI (Scene GameObjects)")]
        [SerializeField] private GameObject menuRoot;          // 菜单根节点（包含按钮面板等）
        [SerializeField] private Image dimBackground;          // 全屏暗色遮罩（Image）
        [SerializeField, Range(0f, 1f)] private float dimAlphaWhenOpen = 0.6f;

        [Header("Behaviour")]
        [SerializeField] private bool pauseTimeScaleWhenOpen = true;
        [SerializeField] private bool autoConfigureButtonsByScene = true;
        [SerializeField] private bool pauseBgmWhenOpenInLevelScenes = true;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "StartMenu";
        [SerializeField] private string worldMapSceneName = "WorldMap";

        [Header("Buttons (Scene GameObjects)")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button backToWorldMapButton;
        [SerializeField] private Button backToMainMenuButton;

        [Header("Optional Button GameObjects (for auto show/hide)")]
        [SerializeField] private GameObject resumeButtonObject;
        [SerializeField] private GameObject backToWorldMapButtonObject;
        [SerializeField] private GameObject backToMainMenuButtonObject;

        // 手动覆盖（autoConfigureButtonsByScene = false 时生效）
        [Header("Manual Visibility (if not auto)")]
        [SerializeField] private bool showResume = true;
        [SerializeField] private bool showBackToWorldMap = true;
        [SerializeField] private bool showBackToMainMenu = true;

        private bool _isOpen;
        private float _cachedTimeScale = 1f;

        private void Awake()
        {
            Debug.Log($"[ESC_Debug] {gameObject.name} Awake in scene: {SceneManager.GetActiveScene().name}");
            
            // 确保只有一个实例，或者至少能被找到
            if (menuRoot == null)
            {
                // 尝试查找名为 "MenuRoot" 的子物体作为兜底
                var found = transform.Find("MenuRoot");
                if (found != null) menuRoot = found.gameObject;
            }

            // 确保 SaveSystem.Current 有值
            SaveSystem.LoadOrCreate();

            // 开场默认关闭
            ApplyOpenState(false);
        }

        private void Start()
        {
            AutoWireButtonEvents();
            ConfigureButtonVisibility();

            // 检查EventSystem
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogError("[EscMenu] No EventSystem found in scene! UI interactions will not work.");
            }
            else
            {
                Debug.Log("[EscMenu] EventSystem found and active");
            }

            // 检查Canvas设置
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[EscMenu] Canvas found - RenderMode: {canvas.renderMode}, sortingOrder: {canvas.sortingOrder}");
                var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster == null)
                {
                    Debug.LogWarning("[EscMenu] No GraphicRaycaster on Canvas! Adding one...");
                    canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Toggle();
            }
        }

        private void OnDisable()
        {
            // 避免脚本被禁用时卡在暂停
            if (_isOpen)
            {
                ApplyOpenState(false);
            }
        }

        public void Toggle()
        {
            SetOpen(!_isOpen);
        }

        public void SetOpen(bool open)
        {
            if (_isOpen == open) return;
            ApplyOpenState(open);
        }

        private void ApplyOpenState(bool open)
        {
            _isOpen = open;

            if (open)
            {
                EnsureEventSystem();
                // 确保 Canvas 排序最高
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 999;
                }
            }

            if (dimBackground != null)
            {
                var c = dimBackground.color;
                c.r = 0f;
                c.g = 0f;
                c.b = 0f;
                c.a = open ? dimAlphaWhenOpen : 0f;
                dimBackground.color = c;
                // 确保不会挡住前面的按钮点击
                dimBackground.raycastTarget = false;
                dimBackground.gameObject.SetActive(open);
            }

            if (menuRoot != null)
            {
                menuRoot.SetActive(open);

                // 确保Canvas Group设置正确（如果存在）
                var canvasGroup = menuRoot.GetComponent<UnityEngine.CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = open;
                    canvasGroup.blocksRaycasts = open;
                    Debug.Log($"[EscMenu] CanvasGroup found - interactable: {open}, blocksRaycasts: {open}");
                }

                Debug.Log($"[EscMenu] MenuRoot set to active: {open}");
            }

            if (pauseTimeScaleWhenOpen)
            {
                if (open)
                {
                    _cachedTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                    Debug.Log($"[EscMenu] Time.timeScale set to 0, cached: {_cachedTimeScale}");
                }
                else
                {
                    Time.timeScale = _cachedTimeScale <= 0f ? 1f : _cachedTimeScale;
                    Debug.Log($"[EscMenu] Time.timeScale restored to {Time.timeScale}");
                }
            }

            // 同步控制 BeatMapPlayer 的暂停/恢复，确保节拍不会在暂停期间推进
            var beatPlayers = FindObjectsOfType<DodgeDots.Audio.BeatMapPlayer>();
            foreach (var bp in beatPlayers)
            {
                bp.SetPaused(open);
            }

            // 在关卡场景中打开 ESC 菜单时，额外暂停 / 恢复 BGM
            if (pauseBgmWhenOpenInLevelScenes)
            {
                string scene = SceneManager.GetActiveScene().name;
                // 约定：关卡场景名以 "Level_" 开头，例如 "Level_1", "Level_2", "Level_3", "Level_hajimi"
                bool isLevelScene = scene.StartsWith("Level_", System.StringComparison.OrdinalIgnoreCase) ||
                                   scene.Equals("Level_hajimi", System.StringComparison.OrdinalIgnoreCase);
                if (isLevelScene)
                {
                    var bgm = FindFirstObjectByType<BGMManager>();
                    if (bgm != null && bgm.audioSource != null)
                    {
                        if (open)
                        {
                            bgm.audioSource.Pause();
                        }
                        else
                        {
                            bgm.audioSource.UnPause();
                        }
                    }
                }
            }
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var esGo = new GameObject("EventSystem_AutoCreated");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("检测到关卡缺少 EventSystem，已按照 Level1 标准自动修复。");
            }
            else
            {
                // 强制确保 EventSystem 及其 Module 是启用状态
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (!es.gameObject.activeInHierarchy) es.gameObject.SetActive(true);
                
                var module = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (module != null && !module.enabled) module.enabled = true;

                // 清除当前选中，防止按钮点击状态被锁定
                es.SetSelectedGameObject(null);
            }
        }

        private void ConfigureButtonVisibility()
        {
            if (!autoConfigureButtonsByScene)
            {
                ApplyButtonVisibility();
                return;
            }

            string scene = SceneManager.GetActiveScene().name;

            // 在主界面一般不需要本菜单，但如果被复用，至少别出现“返回主界面”
            showBackToMainMenu = scene != mainMenuSceneName;

            // 在世界地图不需要“返回世界地图”
            showBackToWorldMap = scene != worldMapSceneName && scene != mainMenuSceneName;

            // 允许关闭菜单
            showResume = true;

            ApplyButtonVisibility();
        }

        private void ApplyButtonVisibility()
        {
            if (resumeButtonObject != null) resumeButtonObject.SetActive(showResume);
            if (backToWorldMapButtonObject != null) backToWorldMapButtonObject.SetActive(showBackToWorldMap);
            if (backToMainMenuButtonObject != null) backToMainMenuButtonObject.SetActive(showBackToMainMenu);
        }

        /// <summary>
        /// 自动给按钮挂上 OnClick 事件，避免忘记在 Inspector 里连导致“按钮没反应”。
        /// </summary>
        private void AutoWireButtonEvents()
        {
            if (resumeButton == null && resumeButtonObject != null)
            {
                resumeButton = resumeButtonObject.GetComponent<Button>();
                if (resumeButton == null)
                {
                    resumeButton = resumeButtonObject.GetComponentInChildren<Button>(true);
                }
            }
            if (backToWorldMapButton == null && backToWorldMapButtonObject != null)
            {
                backToWorldMapButton = backToWorldMapButtonObject.GetComponent<Button>();
                if (backToWorldMapButton == null)
                {
                    backToWorldMapButton = backToWorldMapButtonObject.GetComponentInChildren<Button>(true);
                }
            }
            if (backToMainMenuButton == null && backToMainMenuButtonObject != null)
            {
                backToMainMenuButton = backToMainMenuButtonObject.GetComponent<Button>();
                if (backToMainMenuButton == null)
                {
                    backToMainMenuButton = backToMainMenuButtonObject.GetComponentInChildren<Button>(true);
                }
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnClickResume);
                resumeButton.onClick.AddListener(OnClickResume);
                resumeButton.interactable = true;
                Debug.Log("[EscMenu] Resume button wired and set to interactable");
            }
            else
            {
                Debug.LogWarning("[EscMenu] Resume button is null!");
            }

            if (backToWorldMapButton != null)
            {
                backToWorldMapButton.onClick.RemoveListener(OnClickBackToWorldMap);
                backToWorldMapButton.onClick.AddListener(OnClickBackToWorldMap);
                backToWorldMapButton.interactable = true;
                Debug.Log("[EscMenu] BackToWorldMap button wired and set to interactable");
            }
            else
            {
                Debug.LogWarning("[EscMenu] BackToWorldMap button is null!");
            }

            if (backToMainMenuButton != null)
            {
                backToMainMenuButton.onClick.RemoveListener(OnClickBackToMainMenu);
                backToMainMenuButton.onClick.AddListener(OnClickBackToMainMenu);
                backToMainMenuButton.interactable = true;
                Debug.Log("[EscMenu] BackToMainMenu button wired and set to interactable");
            }
            else
            {
                Debug.LogWarning("[EscMenu] BackToMainMenu button is null!");
            }
        }

        // ===== Button events (bind in Inspector) =====

        public void OnClickResume()
        {
            Debug.Log("[EscMenu] OnClickResume called");
            SetOpen(false);
        }

        public void OnClickBackToWorldMap()
        {
            Debug.Log("[EscMenu] OnClickBackToWorldMap called");
            // 切场景前先保存
            SaveSystem.Save();
            if (pauseTimeScaleWhenOpen) Time.timeScale = 1f;
            DodgeDots.UI.LoadingManager.Instance.LoadScene(worldMapSceneName);
        }

        public void OnClickBackToMainMenu()
        {
            Debug.Log("[EscMenu] OnClickBackToMainMenu called");
            SaveSystem.Save();
            if (pauseTimeScaleWhenOpen) Time.timeScale = 1f;
            DodgeDots.UI.LoadingManager.Instance.LoadScene(mainMenuSceneName);
        }
    }
}

