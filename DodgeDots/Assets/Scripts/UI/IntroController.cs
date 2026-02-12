using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DodgeDots.UI;

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
        [Tooltip("勾选此项，播放本页音效前会强制停止之前未播完的声音")]
        public bool stopPreviousAudio = false;

        [Tooltip("音频结束前多久开始淡出（秒）")]
        public float fadeOutTriggerTime = 0.5f; // 新增：距离结束还有多久开始淡出

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
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("全局设置")]
        [SerializeField] private float fadeSpeed = 1.0f;
        [SerializeField] private float typewriterSpeed = 0.05f;

        [Header("数据配置")]
        [SerializeField] private List<IntroSlide> slides;

        private int _currentIndex = -1;
        private float _timer = 0f;
        private bool _isTransitioning = false;
        private bool _isTyping = false;
        private bool _isFinished = false;

        private Coroutine _typewriterCoroutine;
        private Coroutine _shakeCoroutine;
        private Coroutine _audioRoutine; // 处理音频播放和淡出的协程

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

            // 1. 淡出旧内容
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

            // --- 3. 播放音频并处理自动淡出 ---
            if (sfxAudioSource != null)
            {
                // 如果设置了强制停止，先杀掉之前的协程和声音
                if (currentData.stopPreviousAudio)
                {
                    if (_audioRoutine != null) StopCoroutine(_audioRoutine);
                    sfxAudioSource.Stop();
                }

                if (currentData.slideSfx != null)
                {
                    _audioRoutine = StartCoroutine(PlayAudioWithAutoFadeOut(currentData));
                }
            }

            // 4. 淡入新内容
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

        // --- 核心：音频播放与自动淡出逻辑 ---
        private IEnumerator PlayAudioWithAutoFadeOut(IntroSlide data)
        {
            sfxAudioSource.clip = data.slideSfx;
            sfxAudioSource.volume = data.sfxVolume;
            sfxAudioSource.Play();

            float clipLength = data.slideSfx.length;
            float fadeStartTime = clipLength - data.fadeOutTriggerTime;

            // 正常播放阶段
            while (sfxAudioSource.isPlaying && sfxAudioSource.time < fadeStartTime)
            {
                yield return null;
            }

            // 淡出阶段
            float startVolume = sfxAudioSource.volume;
            float fadeTimer = 0f;
            while (sfxAudioSource.isPlaying && fadeTimer < data.fadeOutTriggerTime)
            {
                fadeTimer += Time.deltaTime;
                sfxAudioSource.volume = Mathf.Lerp(startVolume, 0f, fadeTimer / data.fadeOutTriggerTime);
                yield return null;
            }

            sfxAudioSource.Stop();
            sfxAudioSource.volume = startVolume; // 重置音量备用
        }

        // --- 其余逻辑保持不变 ---
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
            if (LoadingManager.Instance != null) LoadingManager.Instance.LoadScene(nextSceneName);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}