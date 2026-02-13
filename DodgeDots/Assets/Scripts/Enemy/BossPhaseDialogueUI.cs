using System.Collections;
using UnityEngine;
using TMPro;
using DodgeDots.Enemy;
using DodgeDots.Player;

namespace DodgeDots.UI
{
    /// <summary>
    /// Boss阶段文案UI
    /// 监听Boss事件，在Boss进入新阶段时显示对应的文案
    /// </summary>
    public class BossPhaseDialogueUI : MonoBehaviour
    {
        [Header("引用设置")]
        [SerializeField] private BossBase boss;
        [SerializeField] private PlayerHealth playerHealth;
        
        [Header("Settings")]
        [Tooltip("When enabled, player is invincible to damage (e.g., boss bullets) during dialogue.")]
        [SerializeField] private bool enableDialogueInvincible = true;
        [Tooltip("Allow dialogue to pause the game (Time.timeScale = 0). Disable to keep boss moving/attacking.")]
        [SerializeField] private bool allowPauseGameForDialogue = false;

        [Header("UI Components")]
        [SerializeField] private CanvasGroup dialogueCanvasGroup;
        [SerializeField] private TextMeshProUGUI phaseNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        
        [Header("字体设置")]
        [SerializeField] private TMP_FontAsset phaseFontAsset;
        [SerializeField] private TMP_FontAsset dialogueFontAsset;
        [SerializeField] private float phaseNameFontSize = 36f;
        [SerializeField] private float dialogueFontSize = 24f;
        
        [Header("动画设置")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        private Coroutine _displayCoroutine;
        private bool _dialogueInvincibilityActive;

        private void Start()
        {
            // 初始化清空文本，防止闪现默认文本
            if (phaseNameText != null) phaseNameText.text = string.Empty;
            if (dialogueText != null) dialogueText.text = string.Empty;

            // 初始化CanvasGroup
            if (dialogueCanvasGroup == null)
            {
                dialogueCanvasGroup = GetComponent<CanvasGroup>();
            }
            
            if (dialogueCanvasGroup != null)
            {
                dialogueCanvasGroup.alpha = 0f;
            }

            // 监听Boss的阶段对话事件
            if (boss != null)
            {
                boss.OnPhaseDialogue += OnBossPhaseDialogue;
            }
            else
            {
                Debug.LogWarning("BossPhaseDialogueUI: Boss引用未设置！");
            }
        }

        private void OnDestroy()
        {
            // 取消监听
            if (boss != null)
            {
                boss.OnPhaseDialogue -= OnBossPhaseDialogue;
            }

            // 确保销毁时关闭文案无敌
            SetPlayerDialogueInvincible(false);
        }

        /// <summary>
        /// Boss阶段事件处理
        /// </summary>
        private void OnBossPhaseDialogue(BossPhaseDialogue dialogue)
        {
            if (_displayCoroutine != null)
            {
                StopCoroutine(_displayCoroutine);
                // 停止上一次显示时，确保关闭无敌
                SetPlayerDialogueInvincible(false);
            }

            _displayCoroutine = StartCoroutine(DisplayDialogueCoroutine(dialogue));
        }

        /// <summary>
        /// 显示文案的协程
        /// </summary>
        private IEnumerator DisplayDialogueCoroutine(BossPhaseDialogue dialogue)
        {
            // 文案显示期间让玩家无敌（避免被Boss弹幕击中）
            if (enableDialogueInvincible)
            {
                SetPlayerDialogueInvincible(true);
            }

            // 设置文本（先设置内容，再淡入，避免默认 "New Text" 闪现）
            if (phaseNameText != null)
            {
                phaseNameText.text = dialogue.phaseName;
                
                // 应用字体设置
                if (phaseFontAsset != null)
                {
                    phaseNameText.font = phaseFontAsset;
                }
                phaseNameText.fontSize = phaseNameFontSize;
                
                Debug.Log($"设置阶段名称: {dialogue.phaseName}");
            }
            else
            {
                Debug.LogWarning("phaseNameText 为空！");
            }
            
            if (dialogueText != null)
            {
                dialogueText.text = dialogue.dialogueText;
                
                // 应用字体设置
                if (dialogueFontAsset != null)
                {
                    dialogueText.font = dialogueFontAsset;
                }
                dialogueText.fontSize = dialogueFontSize;
                
                Debug.Log($"设置对话文本: {dialogue.dialogueText}");
            }
            else
            {
                Debug.LogWarning("dialogueText 为空！");
            }

            // 淡入
            yield return StartCoroutine(FadeInCoroutine());

            bool shouldPauseGame = allowPauseGameForDialogue && dialogue.pauseGameForDialogue;

            // Pause game (if allowed and configured)
            if (shouldPauseGame)
            {
                Time.timeScale = 0f;
            }

            // Wait for display duration
            float waitTime = dialogue.dialogueDuration > 0 ? dialogue.dialogueDuration : 3f;
            
            if (shouldPauseGame)
            {
                yield return new WaitForSecondsRealtime(waitTime);
            }
            else
            {
                yield return new WaitForSeconds(waitTime);
            }

            // Resume game speed
            if (shouldPauseGame)
            {
                Time.timeScale = 1f;
            }


            // 淡出
            yield return StartCoroutine(FadeOutCoroutine());

            // 文案结束后关闭无敌
            if (enableDialogueInvincible)
            {
                SetPlayerDialogueInvincible(false);
            }
        }

        /// <summary>
        /// 控制玩家在文案期间的无敌状态
        /// </summary>
        private void SetPlayerDialogueInvincible(bool active)
        {
            if (_dialogueInvincibilityActive == active) return;
            _dialogueInvincibilityActive = active;

            if (playerHealth == null)
            {
                // 尝试自动寻找玩家
                var found = FindObjectOfType<PlayerHealth>();
                if (found != null)
                {
                    playerHealth = found;
                }
            }

            if (playerHealth != null)
            {
                playerHealth.SetDialogueInvincible(active);
            }
        }

        /// <summary>
        /// 淡入效果
        /// </summary>
        private IEnumerator FadeInCoroutine()
        {
            if (dialogueCanvasGroup == null) yield break;

            float elapsedTime = 0f;
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                dialogueCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
                yield return null;
            }
            dialogueCanvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 淡出效果
        /// </summary>
        private IEnumerator FadeOutCoroutine()
        {
            if (dialogueCanvasGroup == null) yield break;

            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                dialogueCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                yield return null;
            }
            dialogueCanvasGroup.alpha = 0f;
        }
    }
}
