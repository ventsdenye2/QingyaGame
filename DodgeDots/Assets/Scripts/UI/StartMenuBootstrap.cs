using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DodgeDots.Save;

namespace DodgeDots.UI
{
    public class StartMenuBootstrap : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string worldMapSceneName = "WorldMap";
        [SerializeField] private string introSceneName = "IntroScene";

        [Header("UI Layout")]
        [SerializeField] private Vector2 panelSize = new Vector2(420f, 320f);
        [SerializeField] private float buttonHeight = 48f;
        [SerializeField] private float buttonSpacing = 12f;
        [SerializeField] private float panelPaddingX = 80f;

        [Header("UI Text")]
        [SerializeField] private string titleLabel = "开始";
        [SerializeField] private string startLabel = "开始游戏";
        [SerializeField] private string loadLabel = "读档";
        [SerializeField] private string newLabel = "新游戏";
        [SerializeField] private string quitLabel = "退出";
        [SerializeField] private string hintLabel = "自动保存：每 30 秒与切换/退出时";
        [SerializeField] private int titleFontSize = 36;
        [SerializeField] private int buttonFontSize = 22;
        [SerializeField] private int hintFontSize = 18;

        [Header("UI Visuals")]
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Color buttonColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Font font;

        private Button _startButton;
        private Button _loadButton;

        private void Start()
        {
            SaveSystem.LoadOrCreate();
            EnsureEventSystem();
            BuildUi();
            RefreshButtons();
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panelGo = new GameObject("Panel", typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = backgroundColor;
            if (backgroundSprite != null)
            {
                panelImage.sprite = backgroundSprite;
                panelImage.type = Image.Type.Sliced;
            }

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.sizeDelta = panelSize;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            var title = CreateText("Title", titleLabel, titleFontSize);
            title.transform.SetParent(panelGo.transform, false);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);

            float startY = -80f;
            _startButton = CreateButton(startLabel, OnStartGame);
            PositionButton(_startButton, panelGo.transform, startY);

            _loadButton = CreateButton(loadLabel, OnLoadGame);
            PositionButton(_loadButton, panelGo.transform, startY - (buttonHeight + buttonSpacing));

            var newButton = CreateButton(newLabel, OnNewGame);
            PositionButton(newButton, panelGo.transform, startY - 2 * (buttonHeight + buttonSpacing));

            var quitButton = CreateButton(quitLabel, OnQuitGame);
            PositionButton(quitButton, panelGo.transform, startY - 3 * (buttonHeight + buttonSpacing));

            var hint = CreateText("Hint", hintLabel, hintFontSize);
            hint.transform.SetParent(panelGo.transform, false);
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 16f);
        }

        private void PositionButton(Button button, Transform parent, float y)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(panelSize.x - panelPaddingX, buttonHeight);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private Button CreateButton(string label, Action onClick)
        {
            var go = new GameObject($"Button_{label}", typeof(Image), typeof(Button));
            var image = go.GetComponent<Image>();
            image.color = buttonColor;
            if (buttonSprite != null)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
            }

            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => onClick());

            var text = CreateText("Text", label, buttonFontSize);
            text.transform.SetParent(go.transform, false);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;

            return button;
        }

        private Text CreateText(string name, string content, int size)
        {
            var go = new GameObject(name, typeof(Text));
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private void RefreshButtons()
        {
            bool hasSave = SaveSystem.HasSave;
            if (_loadButton != null) _loadButton.interactable = hasSave;
            if (_startButton != null) _startButton.interactable = true;
        }

        private void OnStartGame()
        {
            if (SaveSystem.HasSave)
            {
                SaveSystem.Load();
                DodgeDots.UI.LoadingManager.Instance.LoadScene(worldMapSceneName);
            }
            else
            {
                SaveSystem.NewGame();
                SaveSystem.Save();
                DodgeDots.UI.LoadingManager.Instance.LoadScene(introSceneName);
            }
        }

        private void OnLoadGame()
        {
            if (!SaveSystem.HasSave) return;
            SaveSystem.Load();
            DodgeDots.UI.LoadingManager.Instance.LoadScene(worldMapSceneName);
        }

        private void OnNewGame()
        {
            SaveSystem.Clear();
            SaveSystem.NewGame();
            SaveSystem.Save();
            DodgeDots.UI.LoadingManager.Instance.LoadScene(introSceneName);
        }

        private void OnQuitGame()
        {
            SaveSystem.Save();
            Application.Quit();
        }
    }
}
