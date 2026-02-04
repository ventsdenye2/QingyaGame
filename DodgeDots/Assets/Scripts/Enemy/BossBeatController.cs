using System;
using UnityEngine;

namespace DodgeDots.Enemy
{
    // 将boss技能与BGM节拍绑定的桥接脚本。
    public class BossBeatController : MonoBehaviour
    {
        [Tooltip("指向场景中的BGMManager对象（或用 Find 获取）")]
        public DodgeDots.Audio.BGMManager bgmManager;

        [Tooltip("指向boss的BossBase组件（或自动查找）")]
        public BossBase bossBase;

        [Tooltip("每隔多少节拍触发一次技能（例如 1 表示每拍，4 表示每4拍一次）")]
        public int skillEveryNBeats = 1;

        int lastHandledBeat = 0;
        bool subscribed = false;

        void OnEnable()
        {
            if (bgmManager == null)
            {
                bgmManager = FindObjectOfType<DodgeDots.Audio.BGMManager>();
                Debug.Log($"[BossBeatController] OnEnable: Auto-found BGMManager = {(bgmManager != null ? "YES" : "NO")}");
            }

            if (bossBase == null)
            {
                bossBase = FindObjectOfType<BossBase>();
                Debug.Log($"[BossBeatController] OnEnable: Auto-found BossBase = {(bossBase != null ? "YES" : "NO")}");
            }

            // 启用 boss 的节拍驱动模式（禁用自动攻击循环）
            if (bossBase != null)
            {
                bossBase.beatDrivenMode = true;
                Debug.Log("[BossBeatController] Enabled beatDrivenMode on BossBase");
            }
            
            if (bgmManager != null && !subscribed)
            {
                bgmManager.OnBeat += HandleBeat;
                subscribed = true;
                Debug.Log($"[BossBeatController] Subscribed to BGMManager.OnBeat (skillEveryNBeats={skillEveryNBeats})");
            }
            else if (bgmManager == null)
            {
                Debug.LogError("[BossBeatController] CRITICAL: No BGMManager found in scene! Cannot subscribe to beat events.");
            }
        }

        void OnDisable()
        {
            if (bgmManager != null && subscribed)
            {
                bgmManager.OnBeat -= HandleBeat;
                subscribed = false;
                Debug.Log("[BossBeatController] Unsubscribed from BGMManager.OnBeat");
            }
        }

        void HandleBeat(int beatIndex)
        {
            Debug.Log($"[BossBeatController] HandleBeat received beatIndex={beatIndex}, lastHandledBeat={lastHandledBeat}");
            if (beatIndex <= lastHandledBeat) 
            {
                Debug.Log($"[BossBeatController] SKIPPED (already handled)");
                return;
            }
            lastHandledBeat = beatIndex;

            // 每隔 skillEveryNBeats 拍触发一次技能
            bool shouldTrigger = (skillEveryNBeats <= 1) || (beatIndex % Mathf.Max(1, skillEveryNBeats) == 0);
            if (shouldTrigger)
            {
                TriggerSkill();
            }
            else
            {
                Debug.Log($"[BossBeatController] Filtered out (beatIndex={beatIndex}, skillEveryNBeats={skillEveryNBeats}, beatIndex%N={beatIndex % Mathf.Max(1, skillEveryNBeats)})");
            }
        }

        void TriggerSkill()
        {
            Debug.Log($"<color=yellow>[BossBeatController] ====== TriggerSkill at beat #{lastHandledBeat} ======</color>");
            
            // 触发boss的攻击（获取当前要执行的攻击数据并直接发射）
            if (bossBase == null)
            {
                Debug.LogWarning("[BossBeatController] BossBase not found, cannot trigger skill");
                return;
            }

            // 通过反射获取 attackConfig 并执行攻击
            // 更简单的方法：让 BossBase 提供一个公共方法来触发下一个攻击
            // 暂时使用 SendMessage 让 boss 自己处理
            bossBase.SendMessage("TriggerBeatAttack", SendMessageOptions.DontRequireReceiver);
        }
    }
}
