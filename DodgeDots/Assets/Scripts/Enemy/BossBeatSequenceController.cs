using UnityEngine;
using DodgeDots.Audio;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// 使用BeatMap驱动指定攻击序列（可用于多个序列同时运行）
    /// </summary>
    public class BossBeatSequenceController : MonoBehaviour
    {
        [Tooltip("节拍来源（BeatMapPlayer）")]
        public BeatMapPlayer beatMapPlayer;

        [Tooltip("要驱动的Boss")]
        public BossBase bossBase;

        [Tooltip("要执行的攻击序列配置")]
        public BossAttackConfig attackConfig;

        [Tooltip("每隔多少拍触发一次攻击（1=每拍）")]
        public int attackEveryNBeats = 1;

        int lastHandledBeat = 0;
        int attackIndex = 0;
        bool subscribed = false;

        void OnEnable()
        {
            if (beatMapPlayer == null)
            {
                beatMapPlayer = FindObjectOfType<BeatMapPlayer>();
            }

            if (bossBase == null)
            {
                bossBase = FindObjectOfType<BossBase>();
            }

            if (bossBase != null)
            {
                bossBase.beatDrivenMode = true;
            }

            if (beatMapPlayer != null && !subscribed)
            {
                beatMapPlayer.OnBeat += HandleBeat;
                subscribed = true;
            }
            else if (beatMapPlayer == null)
            {
                Debug.LogError("[BossBeatSequenceController] No BeatMapPlayer found, cannot subscribe.");
            }
        }

        void OnDisable()
        {
            if (beatMapPlayer != null && subscribed)
            {
                beatMapPlayer.OnBeat -= HandleBeat;
                subscribed = false;
            }
        }

        void HandleBeat(int beatIndex)
        {
            if (beatIndex <= lastHandledBeat)
            {
                return;
            }
            lastHandledBeat = beatIndex;

            bool shouldTrigger = (attackEveryNBeats <= 1) || (beatIndex % Mathf.Max(1, attackEveryNBeats) == 0);
            if (!shouldTrigger) return;

            if (bossBase == null || attackConfig == null || attackConfig.attackSequence == null || attackConfig.attackSequence.Length == 0)
            {
                Debug.LogWarning("[BossBeatSequenceController] Missing bossBase or attackConfig.");
                return;
            }

            BossAttackData data = attackConfig.attackSequence[attackIndex];
            bossBase.ExecuteAttackData(data);

            attackIndex++;
            if (attackIndex >= attackConfig.attackSequence.Length)
            {
                if (attackConfig.loopSequence)
                {
                    attackIndex = 0;
                }
                else
                {
                    attackIndex = attackConfig.attackSequence.Length - 1;
                }
            }
        }
    }
}
