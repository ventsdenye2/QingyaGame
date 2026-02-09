using System;
using System.Collections;
using UnityEngine;

namespace DodgeDots.Audio
{
    // 精准按拍发事件的BGM管理器。支持两种模式：基于BPM的等间隔节拍，或基于手动的BeatMap（秒数组）触发。
    public class BGMManager : MonoBehaviour
    {
        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip clip;
        [Tooltip("是否循环播放BGM")]
        public bool loop = true;

        [Header("Timing (BPM Mode)")]
        [Tooltip("Beats per minute（仅在不使用 BeatMap 时生效）")]
        public float bpm = 120f;
        [Tooltip("在播放开始前的延迟（秒），用于 scheduling 稳定性）")]
        public double startDelaySeconds = 0.2;

        [Header("Optional Manual BeatMap")]
        [Tooltip("如果指定了 BeatMap，则使用手动时间点触发节拍（秒，基于播放起点）")]
        public BeatMap beatMap;

        // beat event: 参数为从1开始的节拍索引
        public event Action<int> OnBeat;

        double beatInterval;
        double nextBeatDsp;
        int beatIndex;

        // 如果使用 BeatMap，预先计算 DSP 时间数组
        double[] beatDspTimes;
        double dspStart = 0.0;
        int beatLoopCount = 0;

        void Start()
        {
            // 检测场景中 AudioListener 的数量，Unity 要求场景中只有一个
            var listeners = FindObjectsOfType<AudioListener>();
            if (listeners.Length != 1)
            {
                Debug.LogWarning($"[BGMManager] Found {listeners.Length} AudioListener(s) in scene. Ensure exactly one AudioListener is present.");
            }

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            if (clip != null)
                audioSource.clip = clip;
            audioSource.loop = loop;

            beatInterval = 60.0 / Mathf.Max(1f, bpm);

            dspStart = AudioSettings.dspTime + startDelaySeconds;
            audioSource.PlayScheduled(dspStart);
            Debug.Log($"[BGMManager] Scheduled play at dspStart={dspStart:F6}, startDelaySeconds={startDelaySeconds:F3}, bpm={bpm}");

            beatIndex = 0;
            beatLoopCount = 0;

            if (beatMap != null && beatMap.beatTimes != null && beatMap.beatTimes.Length > 0)
            {
                beatDspTimes = new double[beatMap.beatTimes.Length];
                for (int i = 0; i < beatMap.beatTimes.Length; i++)
                    beatDspTimes[i] = dspStart + beatMap.beatTimes[i];

                nextBeatDsp = beatDspTimes[0];
            }
            else
            {
                nextBeatDsp = dspStart; // 第一拍为起始点
            }

            if (beatDspTimes != null && beatDspTimes.Length > 0)
                Debug.Log($"[BGMManager] Using BeatMap with {beatDspTimes.Length} beats, first at {beatDspTimes[0]-dspStart:F6}s (relative)");
            StartCoroutine(BeatLoop());
        }

        IEnumerator BeatLoop()
        {
            // 不再依赖 audioSource.isPlaying 的初始状态：PlayScheduled 会在未来某个 dsp 时间开始播放，
            // 我们需要等待并在正确的 dsp 时间触发节拍事件，即使 audioSource.isPlaying 仍为 false。
            while (true)
            {
                double dsp = AudioSettings.dspTime;

                if (beatDspTimes != null && beatDspTimes.Length > 0)
                {
                    if (beatIndex < beatDspTimes.Length)
                    {
                        double loopOffset = (clip != null) ? clip.length * beatLoopCount : 0.0;
                        double targetBeatDsp = beatDspTimes[beatIndex] + loopOffset;
                        if (dsp + 0.001 >= targetBeatDsp)
                        {
                            beatIndex++;
                            Debug.Log($"[BGMManager] Beat triggered (manual) #{beatIndex} at dsp={dsp:F6}, targetDsp={targetBeatDsp:F6}");
                            try { OnBeat?.Invoke(beatIndex); } catch { }
                        }
                    }
                    // 全部手动节拍触发完成后退出循环
                    if (beatIndex >= beatDspTimes.Length)
                    {
                        if (loop && clip != null)
                        {
                            beatIndex = 0;
                            beatLoopCount++;
                        }
                        else
                        {
                            Debug.Log("[BGMManager] All manual beats triggered, exiting BeatLoop.");
                            break;
                        }
                    }
                }
                else
                {
                    if (dsp + 0.001 >= nextBeatDsp)
                    {
                        beatIndex++;
                        Debug.Log($"[BGMManager] Beat triggered (bpm) #{beatIndex} at dsp={dsp:F6}, nextBeatDsp(before)={nextBeatDsp:F6}");
                        try { OnBeat?.Invoke(beatIndex); } catch { }
                        nextBeatDsp += beatInterval;
                    }

                    // 如果音频已停止且不再有接近的节拍，安全地退出
                    if (audioSource != null && !audioSource.isPlaying && dsp > nextBeatDsp + 1.0)
                    {
                        Debug.Log("[BGMManager] Audio stopped and no near beats, exiting BeatLoop.");
                        break;
                    }
                }

                yield return null;
            }
        }

        // 可在运行时调整BPM（仅BPM模式生效）
        public void SetBPM(float newBpm)
        {
            bpm = newBpm;
            beatInterval = 60.0 / Mathf.Max(1f, bpm);
        }

        // 公开播放起始的 DSP 时间，供编辑器录制工具使用
        public double GetDspStart()
        {
            return dspStart;
        }

        // 方便查询当前 DSP 时间
        public double GetDspNow()
        {
            return AudioSettings.dspTime;
        }
    }
}
