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

        [Tooltip("是否循环 BeatMap")]
        public bool loop = true;

        public event Action<int> OnBeat;
        public event Action<int> OnLoop;

        double[] beatDspTimes;
        int beatIndex;
        bool started;
        Coroutine loopCoroutine;
        int beatLoopCount;
        double loopLengthSeconds;
        double nextLoopDsp;
        bool useBgmLoop;

        private bool _isPaused;
        private double _pauseStartDsp;

        public void SetPaused(bool paused)
        {
            if (_isPaused == paused) return;
            
            _isPaused = paused;
            if (_isPaused)
            {
                _pauseStartDsp = AudioSettings.dspTime;
            }
            else
            {
                // 恢复时，将所有 DSP 时间线整体向后平移暂停的持续时间
                double pauseDuration = AudioSettings.dspTime - _pauseStartDsp;
                if (pauseDuration > 0)
                {
                    if (beatDspTimes != null)
                    {
                        for (int i = 0; i < beatDspTimes.Length; i++)
                        {
                            beatDspTimes[i] += pauseDuration;
                        }
                    }
                    nextLoopDsp += pauseDuration;
                }
            }
        }

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
            beatLoopCount = 0;

            if (bgmManager != null && bgmManager.audioSource != null && bgmManager.audioSource.clip != null)
            {
                loopLengthSeconds = bgmManager.audioSource.clip.length;
                useBgmLoop = loop && bgmManager.audioSource.loop && loopLengthSeconds > 0.0;
            }
            else
            {
                loopLengthSeconds = beatMap.beatTimes[beatMap.beatTimes.Length - 1];
                useBgmLoop = false;
            }
            nextLoopDsp = dspStart + loopLengthSeconds;
            loopCoroutine = StartCoroutine(BeatLoop());
        }

        IEnumerator BeatLoop()
        {
            while (beatDspTimes != null && beatDspTimes.Length > 0)
            {
                if (_isPaused)
                {
                    yield return null;
                    continue;
                }

                double dsp = AudioSettings.dspTime;
                if (useBgmLoop && dsp + 0.001 >= nextLoopDsp)
                {
                    beatIndex = 0;
                    beatLoopCount++;
                    nextLoopDsp += loopLengthSeconds;
                    try { OnLoop?.Invoke(beatLoopCount); } catch { }
                }

                if (beatIndex < beatDspTimes.Length)
                {
                    double loopOffset = loopLengthSeconds * beatLoopCount;
                    double targetDsp = beatDspTimes[beatIndex] + loopOffset;
                    if (dsp + 0.001 >= targetDsp)
                    {
                        beatIndex++;
                        try { OnBeat?.Invoke(beatIndex); } catch { }
                    }
                }

                if (!useBgmLoop && beatIndex >= beatDspTimes.Length)
                {
                    if (loop && loopLengthSeconds > 0.0)
                    {
                        beatIndex = 0;
                        beatLoopCount++;
                        try { OnLoop?.Invoke(beatLoopCount); } catch { }
                    }
                    else
                    {
                        break;
                    }
                }
                yield return null;
            }
        }
    }
}
