using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DodgeDots.UI; // 保持你原有的命名空间引用

namespace DodgeDots.Cutscene
{
    [System.Serializable]
    public class IntroSlide
    {
        [Header("基础内容")]
        public Sprite image;
        [TextArea(3, 5)] public string text;
        public float duration = 4.0f;

        [Header("视觉效果")]
        public bool useFade = true;
        public bool useShake = false;
        public float shakeDuration = 0.5f;
        public float shakeIntensity = 20f;

        [Header("听觉效果")]
        [Tooltip("勾选此项，播放本页音效前会强制停止之前所有未播完的声音")]
        public bool stopPreviousAudio = false;

        [Tooltip("该页面播放时触发的音效")]
        public AudioClip slideSfx;

        [Range(0f, 1f)]
        public float sfxVolume = 1.0f;
    }

    public class IntroController : MonoBehaviour
    {
        [Header("场景跳转")]
        [SerializeField] private string nextSceneName = "WorldMap";

        [Header("UI 组件绑定")]
        [SerializeField] private Image displayImage;
        [SerializeField] private TextMeshProUGUI displayText;
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private Button skipButton;

        [Header("效果组件")]
        [SerializeField] private RectTransform shakeTarget;
        // 注意：不再需要拖拽 AudioSource，代码会自动创建

        [Header("全局设置")]
        [SerializeField] private float fadeSpeed = 1.0f;
        [SerializeField] private float typewriterSpeed = 0.05f;
        [Tooltip("音频在结束前多少秒开始自动淡出（全局统一设置）")]
        [SerializeField] private float globalAudioFadeDuration = 1.5f;

        [Header("数据配置")]
        [SerializeField] private List<IntroSlide> slides;

        private int _currentIndex = -1;
        private float _timer = 0f;
        private bool _isTransitioning = false;
        private bool _isTyping = false;
        private bool _isFinished = false;

        private Coroutine _typewriterCoroutine;
        private Coroutine _shakeCoroutine;

        // 用来记录当前所有正在播放的音频对象，以便实现“强制停止”功能
        private List<GameObject> _activeAudioObjects = new List<GameObject>();
        private Vector2 _originalPosition;

        private void Start()
        {
            if (skipButton != null) skipButton.onClick.AddListener(FinishIntro);
            if (contentCanvasGroup != null) contentCanvasGroup.alpha = 0f;
            if (shakeTarget != null) _originalPosition = shakeTarget.anchoredPosition;

            ShowNextSlide();
        }

        private void Update()
        {
            if (_isTransitioning || _isFinished) return;

            if (!_isTyping)
            {
                _timer += Time.deltaTime;
                if (_timer >= GetCurrentDuration()) ShowNextSlide();
            }

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                if (_isTyping) SkipTypewriter();
                else ShowNextSlide();
            }
        }

        private float GetCurrentDuration() => (slides != null && _currentIndex >= 0) ? slides[_currentIndex].duration : 1f;

        public void ShowNextSlide()
        {
            if (_isTransitioning || _isFinished) return;
            int nextIndex = _currentIndex + 1;

            if (nextIndex >= slides.Count) FinishIntro();
            else StartCoroutine(TransitionToSlide(nextIndex));
        }

        private IEnumerator TransitionToSlide(int index)
        {
            _isTransitioning = true;
            IntroSlide currentData = slides[index];

            // 1. 淡出旧画面
            if (index > 0 && currentData.useFade && contentCanvasGroup != null)
            {
                while (contentCanvasGroup.alpha > 0f)
                {
                    contentCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
                    yield return null;
                }
            }
            else if (contentCanvasGroup != null)
            {
                contentCanvasGroup.alpha = 0f;
            }

            // 2. 切换数据
            _currentIndex = index;
            _timer = 0f;

            if (currentData.image != null) displayImage.sprite = currentData.image;
            displayText.text = "";

            // --- 3. 音频处理逻辑 (新) ---

            // 如果需要强制打断之前的声音
            if (currentData.stopPreviousAudio)
            {
                StopAllActiveAudio();
            }

            // 播放新声音（独立播放，互不干扰）
            if (currentData.slideSfx != null)
            {
                StartCoroutine(PlayIndependentAudio(currentData));
            }

            // 4. 淡入新画面
            if (currentData.useFade && contentCanvasGroup != null)
            {
                while (contentCanvasGroup.alpha < 1f)
                {
                    contentCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
                    yield return null;
                }
                contentCanvasGroup.alpha = 1f;
            }
            else if (contentCanvasGroup != null)
            {
                contentCanvasGroup.alpha = 1f;
            }

            _isTransitioning = false;

            if (currentData.useShake && shakeTarget != null) StartShake(currentData.shakeDuration, currentData.shakeIntensity);
            StartTypewriter(currentData.text);
        }

        // --- 音频核心逻辑：独立播放并自动淡出 ---
        private IEnumerator PlayIndependentAudio(IntroSlide data)
        {
            // 创建临时 GameObject
            GameObject audioObj = new GameObject($"TempSFX_{data.slideSfx.name}");
            audioObj.transform.SetParent(this.transform);

            // 添加并配置 AudioSource
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = data.slideSfx;
            source.volume = data.sfxVolume;
            source.loop = false;
            source.playOnAwake = false;

            // 加入列表管理
            _activeAudioObjects.Add(audioObj);

            source.Play();

            // 计算时间：总时长 - 全局淡出时间
            // 比如 10秒音频，全局淡出1.5秒，那么播放 8.5秒后开始淡出
            float fadeStartTime = Mathf.Max(0, data.slideSfx.length - globalAudioFadeDuration);

            // 等待直到需要淡出的那一刻
            // 增加 source != null 检查，防止外部被 StopAllActiveAudio 销毁后这里报错
            while (source != null && source.isPlaying && source.time < fadeStartTime)
            {
                yield return null;
            }

            // 执行淡出
            if (source != null && source.isPlaying)
            {
                float startVol = source.volume;
                float fadeTimer = 0f;
                // 实际淡出时长取 Min，防止音频本身极短
                float actualFadeDuration = Mathf.Min(globalAudioFadeDuration, data.slideSfx.length);

                while (source != null && source.isPlaying && fadeTimer < actualFadeDuration)
                {
                    fadeTimer += Time.deltaTime;
                    source.volume = Mathf.Lerp(startVol, 0f, fadeTimer / actualFadeDuration);
                    yield return null;
                }
            }

            // 播放完毕，清理垃圾
            if (audioObj != null)
            {
                _activeAudioObjects.Remove(audioObj);
                Destroy(audioObj);
            }
        }

        private void StopAllActiveAudio()
        {
            // 倒序遍历销毁
            for (int i = _activeAudioObjects.Count - 1; i >= 0; i--)
            {
                if (_activeAudioObjects[i] != null)
                {
                    Destroy(_activeAudioObjects[i]);
                }
            }
            _activeAudioObjects.Clear();
        }

        // --- 视觉效果逻辑 (保持原样) ---
        private void StartShake(float duration, float intensity)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeTargetProcess(duration, intensity));
        }

        private IEnumerator ShakeTargetProcess(float duration, float intensity)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;
                shakeTarget.anchoredPosition = _originalPosition + new Vector2(x, y);
                elapsed += Time.deltaTime;
                yield return null;
            }
            shakeTarget.anchoredPosition = _originalPosition;
        }

        private void StartTypewriter(string text)
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text));
        }

        private IEnumerator TypewriterCoroutine(string text)
        {
            _isTyping = true;
            displayText.text = "";
            foreach (char c in text)
            {
                displayText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }
            _isTyping = false;
        }

        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            displayText.text = slides[_currentIndex].text;
            _isTyping = false;
            _timer = 0f;
        }

        private void FinishIntro()
        {
            if (_isFinished) return;
            _isFinished = true;

            // 离开前把所有声音清理干净
            StopAllActiveAudio();

            if (LoadingManager.Instance != null) LoadingManager.Instance.LoadScene(nextSceneName);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}