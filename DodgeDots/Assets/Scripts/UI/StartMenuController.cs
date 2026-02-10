using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DodgeDots.Save;

namespace DodgeDots.UI
{
    /// <summary>
    /// 开始菜单控制器（仅处理：新游戏 / 加载游戏 / 退出）
    /// 背景和标题完全由场景里的 GameObject 自己控制，本脚本不再改动。
    /// </summary>
    public class StartMenuController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string worldMapSceneName = "WorldMap";

        [Header("Buttons")]
        [SerializeField] private Button loadButton;
        [SerializeField] private Button newButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            // 确保有一份当前存档数据
            SaveSystem.LoadOrCreate();
            BindButtons();
            RefreshButtons();
        }

        private void OnEnable()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(OnLoadGame);
                loadButton.onClick.AddListener(OnLoadGame);
            }
            if (newButton != null)
            {
                newButton.onClick.RemoveListener(OnNewGame);
                newButton.onClick.AddListener(OnNewGame);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitGame);
                quitButton.onClick.AddListener(OnQuitGame);
            }
        }

        private void RefreshButtons()
        {
            // 只有在有存档的时候才允许点击“加载游戏”
            if (loadButton != null) loadButton.interactable = SaveSystem.HasSave;
        }

        public void OnLoadGame()
        {
            if (!SaveSystem.HasSave) return;
            SaveSystem.Load();
            DodgeDots.UI.LoadingManager.Instance.LoadScene(worldMapSceneName);
        }

        public void OnNewGame()
        {
            SaveSystem.Clear();
            SaveSystem.NewGame();
            SaveSystem.Save();
            DodgeDots.UI.LoadingManager.Instance.LoadScene(worldMapSceneName);
        }

        public void OnQuitGame()
        {
            SaveSystem.Save();
            Application.Quit();
        }
    }
}
