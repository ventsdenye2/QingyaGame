using System;
using System.Collections;
using UnityEngine;

namespace DodgeDots.Audio
{
    /// <summary>
    /// 仅负责按 BeatMap 触发节拍事件，不播放音频。
    /// 用于多个 BeatMap 同时驱动不同出招序列。
    /// </summary>
    public class BeatMapPlayer : MonoBehaviour
    {
        [Tooltip("可选：用于对齐同一首BGM的起始DSP时间")]
        public BGMManager bgmManager;

        [Tooltip("要播放的BeatMap（秒数组）")]
        public BeatMap beatMap;

        [Tooltip("是否自动开始")]
        public bool autoStart = true;

        [Tooltip("是否等待BGMManager初始化（dspStart>0）")]
        public bool waitForBgmStart = true;

        [Tooltip("额外偏移（秒），用于微调节拍")]
        public double startOffsetSeconds = 0.0;

        public event Action<int> OnBeat;

        double[] beatDspTimes;
        int beatIndex;
        bool started;
        Coroutine loopCoroutine;

        void OnEnable()
        {
            if (autoStart)
            {
                Begin();
            }
        }

        void OnDisable()
        {
            if (loopCoroutine != null)
            {
                StopCoroutine(loopCoroutine);
                loopCoroutine = null;
            }
            started = false;
        }

        public void Begin()
        {
            if (started) return;
            started = true;
            loopCoroutine = StartCoroutine(InitAndPlay());
        }

        IEnumerator InitAndPlay()
        {
            if (beatMap == null || beatMap.beatTimes == null || beatMap.beatTimes.Length == 0)
            {
                Debug.LogWarning("[BeatMapPlayer] No BeatMap or empty beatTimes.");
                yield break;
            }

            if (bgmManager == null)
            {
                bgmManager = FindObjectOfType<BGMManager>();
            }

            if (waitForBgmStart && bgmManager != null)
            {
                while (bgmManager.GetDspStart() <= 0.0)
                {
                    yield return null;
                }
            }

            double dspStart = (bgmManager != null) ? bgmManager.GetDspStart() : AudioSettings.dspTime;
            dspStart += startOffsetSeconds;

            beatDspTimes = new double[beatMap.beatTimes.Length];
            for (int i = 0; i < beatMap.beatTimes.Length; i++)
            {
                beatDspTimes[i] = dspStart + beatMap.beatTimes[i];
            }

            beatIndex = 0;
            loopCoroutine = StartCoroutine(BeatLoop());
        }

        IEnumerator BeatLoop()
        {
            while (beatDspTimes != null && beatIndex < beatDspTimes.Length)
            {
                double dsp = AudioSettings.dspTime;
                if (dsp + 0.001 >= beatDspTimes[beatIndex])
                {
                    beatIndex++;
                    try { OnBeat?.Invoke(beatIndex); } catch { }
                }
                yield return null;
            }
        }
    }
}
