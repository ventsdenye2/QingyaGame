using System.Collections;
using UnityEngine;
using TMPro;
using DodgeDots.Enemy;

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
        
        [Header("UI组件")]
        [SerializeField] private CanvasGroup dialogueCanvasGroup;
        [SerializeField] private TextMeshProUGUI phaseNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        
        [Header("动画设置")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        private Coroutine _displayCoroutine;

        private void Start()
        {
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
        }

        /// <summary>
        /// Boss阶段事件处理
        /// </summary>
        private void OnBossPhaseDialogue(BossPhaseDialogue dialogue)
        {
            if (_displayCoroutine != null)
            {
                StopCoroutine(_displayCoroutine);
            }

            _displayCoroutine = StartCoroutine(DisplayDialogueCoroutine(dialogue));
        }

        /// <summary>
        /// 显示文案的协程
        /// </summary>
        private IEnumerator DisplayDialogueCoroutine(BossPhaseDialogue dialogue)
        {
            // 淡入
            yield return StartCoroutine(FadeInCoroutine());

            // 设置文本
            if (phaseNameText != null)
            {
                phaseNameText.text = dialogue.phaseName;
                Debug.Log($"设置阶段名称: {dialogue.phaseName}");
            }
            else
            {
                Debug.LogWarning("phaseNameText 为空！");
            }
            
            if (dialogueText != null)
            {
                dialogueText.text = dialogue.dialogueText;
                Debug.Log($"设置对话文本: {dialogue.dialogueText}");
            }
            else
            {
                Debug.LogWarning("dialogueText 为空！");
            }

            // 暂停游戏（如果配置了）
            if (dialogue.pauseGameForDialogue)
            {
                Time.timeScale = 0f;
            }

            // 等待显示时间
            float waitTime = dialogue.dialogueDuration > 0 ? dialogue.dialogueDuration : 3f;
            
            if (dialogue.pauseGameForDialogue)
            {
                yield return new WaitForSecondsRealtime(waitTime);
            }
            else
            {
                yield return new WaitForSeconds(waitTime);
            }

            // 恢复游戏速度
            if (dialogue.pauseGameForDialogue)
            {
                Time.timeScale = 1f;
            }

            // 淡出
            yield return StartCoroutine(FadeOutCoroutine());
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
