using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DodgeDots.Save;

namespace DodgeDots.UI
{
    public class StartMenuController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string worldMapSceneName = "WorldMap";

        [Header("UI")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button newButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text hintText;

        private void Start()
        {
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
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartGame);
                startButton.onClick.AddListener(OnStartGame);
            }
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
            if (loadButton != null) loadButton.interactable = SaveSystem.HasSave;
            if (startButton != null) startButton.interactable = true;
        }

        public void OnStartGame()
        {
            if (SaveSystem.HasSave)
            {
                SaveSystem.Load();
            }
            else
            {
                SaveSystem.NewGame();
                SaveSystem.Save();
            }
            SceneManager.LoadScene(worldMapSceneName);
        }

        public void OnLoadGame()
        {
            if (!SaveSystem.HasSave) return;
            SaveSystem.Load();
            SceneManager.LoadScene(worldMapSceneName);
        }

        public void OnNewGame()
        {
            SaveSystem.Clear();
            SaveSystem.NewGame();
            SaveSystem.Save();
            SceneManager.LoadScene(worldMapSceneName);
        }

        public void OnQuitGame()
        {
            SaveSystem.Save();
            Application.Quit();
        }
    }
}
