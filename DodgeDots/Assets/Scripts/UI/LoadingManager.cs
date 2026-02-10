using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DodgeDots.UI
{
    public class LoadingManager : MonoBehaviour
    {
        private static LoadingManager _instance;

        public static LoadingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("LoadingManager");
                    _instance = go.AddComponent<LoadingManager>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        [Header("Resources (可选)")]
        [SerializeField] private string loadingPrefabPath = "UI/LoadingCanvas";

        private GameObject _loadingCanvas;
        private TextMeshProUGUI _progressText;

        private Coroutine _loadingCoroutine;

        private void EnsureUI()
        {
            if (_loadingCanvas != null) return;

            var prefab = Resources.Load<GameObject>(loadingPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[LoadingManager] Resources 未找到预制体: {loadingPrefabPath}。将仅执行异步加载，不显示界面。");
                return;
            }

            _loadingCanvas = Instantiate(prefab);
            _loadingCanvas.name = "LoadingCanvas";
            DontDestroyOnLoad(_loadingCanvas);

            _progressText = _loadingCanvas.GetComponentInChildren<TextMeshProUGUI>(true);

            _loadingCanvas.SetActive(false);
        }

        public void LoadScene(string sceneName, float minShowSeconds = 3f)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[LoadingManager] sceneName 为空，无法加载场景。");
                return;
            }

            if (_loadingCoroutine != null)
            {
                StopCoroutine(_loadingCoroutine);
                _loadingCoroutine = null;
            }

            EnsureUI();
            _loadingCoroutine = StartCoroutine(LoadAsync(sceneName, minShowSeconds));
        }

        private IEnumerator LoadAsync(string sceneName, float minShowSeconds)
        {
            if (_loadingCanvas != null) _loadingCanvas.SetActive(true);
            
            // 加载开始时禁用所有音效
            AudioListener.pause = true;

            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError($"[LoadingManager] LoadSceneAsync 返回 null: {sceneName}");
                if (_loadingCanvas != null) _loadingCanvas.SetActive(false);
                AudioListener.pause = false;
                yield break;
            }

            op.allowSceneActivation = false;

            float shownTime = 0f;
            float dotTimer = 0f;
            int dots = 0;

            while (!op.isDone)
            {
                float dt = Time.unscaledDeltaTime;
                shownTime += dt;
                dotTimer += dt;

                if (dotTimer >= 0.25f)
                {
                    dotTimer = 0f;
                    dots = (dots + 1) % 4;
                }

                if (_progressText != null)
                {
                    _progressText.text = "Loading" + new string('.', dots);
                }

                if (shownTime >= minShowSeconds && op.progress >= 0.9f)
                {
                    op.allowSceneActivation = true;
                }

                yield return null;
            }

            if (_loadingCanvas != null) _loadingCanvas.SetActive(false);
            
            // 加载结束后恢复音效
            AudioListener.pause = false;
            
            _loadingCoroutine = null;
        }
    }
}
