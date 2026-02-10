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
            // 确保 SaveSystem.Current 有值，避免按钮里 Save() 时为 null
            SaveSystem.LoadOrCreate();

            // 开场默认关闭
            ApplyOpenState(false);
        }

        private void Start()
        {
            AutoWireButtonEvents();
            ConfigureButtonVisibility();
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
            }

            if (pauseTimeScaleWhenOpen)
            {
                if (open)
                {
                    _cachedTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = _cachedTimeScale <= 0f ? 1f : _cachedTimeScale;
                }
            }

            // 在关卡场景中打开 ESC 菜单时，额外暂停 / 恢复 BGM
            if (pauseBgmWhenOpenInLevelScenes)
            {
                string scene = SceneManager.GetActiveScene().name;
                // 约定：关卡场景名以 "Level_" 开头，例如 "Level_1"
                bool isLevelScene = scene.StartsWith("Level_", System.StringComparison.OrdinalIgnoreCase);
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
            }

            if (backToWorldMapButton != null)
            {
                backToWorldMapButton.onClick.RemoveListener(OnClickBackToWorldMap);
                backToWorldMapButton.onClick.AddListener(OnClickBackToWorldMap);
            }

            if (backToMainMenuButton != null)
            {
                backToMainMenuButton.onClick.RemoveListener(OnClickBackToMainMenu);
                backToMainMenuButton.onClick.AddListener(OnClickBackToMainMenu);
            }
        }

        // ===== Button events (bind in Inspector) =====

        public void OnClickResume()
        {
            SetOpen(false);
        }

        public void OnClickBackToWorldMap()
        {
            // 切场景前先保存
            SaveSystem.Save();
            if (pauseTimeScaleWhenOpen) Time.timeScale = 1f;
            SceneManager.LoadScene(worldMapSceneName);
        }

        public void OnClickBackToMainMenu()
        {
            SaveSystem.Save();
            if (pauseTimeScaleWhenOpen) Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}

