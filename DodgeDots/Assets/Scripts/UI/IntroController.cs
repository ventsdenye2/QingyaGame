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
        [Tooltip("勾选则此页切换时会有淡入淡出，不勾选则直接硬切")]
        public bool useFade = true;

        [Tooltip("是否在此页开始时触发抖动")]
        public bool useShake = false;
        [Tooltip("抖动持续时间")]
        public float shakeDuration = 0.5f;
        [Tooltip("抖动幅度（建议 10-50）")]
        public float shakeIntensity = 20f;

        [Header("听觉效果")]
        [Tooltip("该页面播放时触发的音效（如爆炸声、拔剑声）")]
        public AudioClip slideSfx; // 单页音效
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
        [Tooltip("拖入你想震动的物体（比如 ContentParent 或 Image）")]
        [SerializeField] private RectTransform shakeTarget; // 恢复：指定抖动目标
        [Tooltip("用来播放音效的 AudioSource")]
        [SerializeField] private AudioSource sfxAudioSource; // 新增：音频源

        [Header("全局设置")]
        [SerializeField] private float fadeSpeed = 1.0f;
        [SerializeField] private float typewriterSpeed = 0.05f;

        [Header("数据配置")]
        [SerializeField] private List<IntroSlide> slides;

        private int _currentIndex = -1;
        private float _timer = 0f;
        private bool _isTransitioning = false;
        private bool _isTyping = false;
        // 防止重复触发结束逻辑的标志位
        private bool _isFinished = false;

        private Coroutine _typewriterCoroutine;
        private Coroutine _shakeCoroutine;

        private Vector2 _originalPosition; // 记录抖动前的原始位置

        private void Start()
        {
            if (skipButton != null) skipButton.onClick.AddListener(FinishIntro);

            // 初始隐藏
            if (contentCanvasGroup != null) contentCanvasGroup.alpha = 0f;

            // 记录震动目标的初始位置，防止越震越歪
            if (shakeTarget != null) _originalPosition = shakeTarget.anchoredPosition;

            ShowNextSlide();
        }

        private void Update()
        {
            if (_isTransitioning || _isFinished) return;

            // 计时逻辑
            if (!_isTyping)
            {
                _timer += Time.deltaTime;
                if (_timer >= GetCurrentDuration()) ShowNextSlide();
            }

            // 点击跳过
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

            // 1. 淡出旧内容 (如果开启)
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
                contentCanvasGroup.alpha = 0f; // 硬切前设为透明
            }

            // 2. 切换数据
            _currentIndex = index;
            _timer = 0f;

            if (currentData.image != null) displayImage.sprite = currentData.image;
            displayText.text = "";

            // --- 播放音效 ---
            if (currentData.slideSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(currentData.slideSfx);
            }

            // 3. 淡入新内容
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
                contentCanvasGroup.alpha = 1f; // 硬切直接显示
            }

            _isTransitioning = false;

            // 4. 触发震动 (使用 shakeTarget)
            if (currentData.useShake && shakeTarget != null)
            {
                StartShake(currentData.shakeDuration, currentData.shakeIntensity);
            }

            // 5. 开始打字
            StartTypewriter(currentData.text);
        }

        // --- 震动逻辑 (针对 shakeTarget) ---
        private void StartShake(float duration, float intensity)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeTargetProcess(duration, intensity));
        }

        private IEnumerator ShakeTargetProcess(float duration, float intensity)
        {
            float elapsed = 0f;
            // 确保每次震动前都以 _originalPosition 为基准
            // 如果你的UI在播放过程中会移动（比如平移入场），这里逻辑需要调整
            // 但对于PPT式播放，固定位置是最稳的

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;

                shakeTarget.anchoredPosition = _originalPosition + new Vector2(x, y);

                elapsed += Time.deltaTime;
                yield return null;
            }

            shakeTarget.anchoredPosition = _originalPosition; // 归位
        }

        // --- 打字机逻辑 ---
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
            // 如果已经执行过一次结束逻辑，直接拦截
            if (_isFinished) return;

            _isFinished = true; // 锁死，防止后续帧再次进入

            if (LoadingManager.Instance != null) LoadingManager.Instance.LoadScene(nextSceneName);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}