using System.Collections;
using UnityEngine;
using DodgeDots.Save; // 引用存档系统

namespace DodgeDots.Dialogue
{
    public class AutoTriggerDialogue : MonoBehaviour
    {
        [Header("要播放的对话")]
        public DialogueConfig dialogueToPlay;

        [Header("延迟时间")]
        [Tooltip("等待几秒后开始播放（建议设置 0.5 或 1.0，等待黑屏淡入结束）")]
        public float startDelay = 0.5f;

        private IEnumerator Start()
        {
            // 安全检查
            if (dialogueToPlay == null) yield break;

            // 等待画面淡入（防止一进场景就弹窗，体验不好）
            yield return new WaitForSeconds(startDelay);

            // 确保存档已加载
            if (SaveSystem.Current == null) SaveSystem.LoadOrCreate();

            // 检查存档里是否包含这个对话的名字
            string dialogueId = dialogueToPlay.name;

            if (!SaveSystem.Current.finishedDialogues.Contains(dialogueId))
            {
                // 如果没播过，就开始播放
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(dialogueToPlay);
                }
            }
            else
            {
                Debug.Log($"[AutoTrigger] 对话 {dialogueId} 已经播放过，自动跳过。");
            }
        }
    }
}