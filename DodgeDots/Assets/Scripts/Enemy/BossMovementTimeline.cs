using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DodgeDots.Audio;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// 按秒触发Boss本体与发射点的移动（独立配置）。
    /// </summary>
    public class BossMovementTimeline : MonoBehaviour
    {
        public enum MoveTarget
        {
            Boss,
            Emitter
        }

        public enum TimelineMoveType
        {
            None,
            ToPosition,
            ByDirection,
            Circle,
            TwoPointLoop
        }

        [Serializable]
        public class MoveEvent
        {
            [Tooltip("触发时间（秒，基于BGM起点）")]
            public float timeSeconds = 0f;

            [Tooltip("移动目标")]
            public MoveTarget target = MoveTarget.Boss;

            [Tooltip("当目标为Emitter时，指定发射点类型")]
            public EmitterType emitterType = EmitterType.MainCore;

            [Header("移动设置")]
            public TimelineMoveType moveType = TimelineMoveType.ToPosition;

            [Tooltip("移动持续时间（秒）")]
            public float moveDuration = 1f;

            [Tooltip("移动速度（当前实现主要使用moveDuration，moveSpeed作为备用）")]
            public float moveSpeed = 5f;

            [Tooltip("目标位置（世界坐标）")]
            public Vector2 targetPosition = Vector2.zero;

            [Tooltip("移动方向（角度，0=右，90=上，180=左，270=下）")]
            public float moveDirection = 0f;

            [Tooltip("移动距离")]
            public float moveDistance = 5f;

            [Header("圆形移动")]
            [Tooltip("圆心偏移（相对Boss位置）")]
            public Vector2 circleCenterOffset = Vector2.zero;

            [Tooltip("半径（<=0 时使用当前与圆心的距离）")]
            public float circleRadius = 0f;

            [Tooltip("角速度（度/秒，负数为顺时针）")]
            public float circleAngularSpeed = 90f;

            [Header("两点循环移动")]
            [Tooltip("点A偏移（相对起始位置）")]
            public Vector2 pointAOffset = Vector2.zero;

            [Tooltip("点B偏移（相对起始位置）")]
            public Vector2 pointBOffset = new Vector2(2f, 0f);
        }

        [Header("时间对齐")]
        [Tooltip("用于对齐BGM起点（可选）")]
        public BGMManager bgmManager;

        [Tooltip("额外时间偏移（秒）")]
        public float startOffsetSeconds = 0f;

        [Tooltip("是否等待BGM开始（dspStart>0）")]
        public bool waitForBgmStart = true;

        [Header("移动目标")]
        public BossBase bossBase;

        [Tooltip("是否让发射点与Boss完全独立（保持世界坐标，不跟随Boss移动）")]
        public bool emittersIndependent = false;

        [Tooltip("时间轴事件列表")]
        public List<MoveEvent> events = new List<MoveEvent>();

        double dspStart = 0.0;
        bool started = false;
        int nextEventIndex = 0;
        List<MoveEvent> sortedEvents = new List<MoveEvent>();
        Dictionary<EmitterType, EmitterPoint> emitters = new Dictionary<EmitterType, EmitterPoint>();
        Dictionary<EmitterPoint, Vector3> independentEmitterWorldPos = new Dictionary<EmitterPoint, Vector3>();
        Dictionary<Transform, Coroutine> activeMoveByTarget = new Dictionary<Transform, Coroutine>();

        void OnEnable()
        {
            if (!started)
            {
                StartCoroutine(InitAndRun());
            }
        }

        void LateUpdate()
        {
            if (!emittersIndependent) return;
            foreach (var kvp in independentEmitterWorldPos)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.position = kvp.Value;
                }
            }
        }

        IEnumerator InitAndRun()
        {
            started = true;

            if (bossBase == null)
            {
                bossBase = FindObjectOfType<BossBase>();
            }

            if (bgmManager == null)
            {
                bgmManager = FindObjectOfType<BGMManager>();
            }

            CacheEmitters();

            if (waitForBgmStart && bgmManager != null)
            {
                while (bgmManager.GetDspStart() <= 0.0)
                {
                    yield return null;
                }
            }

            dspStart = (bgmManager != null ? bgmManager.GetDspStart() : AudioSettings.dspTime) + startOffsetSeconds;

            sortedEvents.Clear();
            sortedEvents.AddRange(events);
            sortedEvents.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
            nextEventIndex = 0;

            while (nextEventIndex < sortedEvents.Count)
            {
                double now = AudioSettings.dspTime;
                double t = now - dspStart;
                if (t >= sortedEvents[nextEventIndex].timeSeconds)
                {
                    TriggerMove(sortedEvents[nextEventIndex]);
                    nextEventIndex++;
                }
                yield return null;
            }
        }

        void CacheEmitters()
        {
            emitters.Clear();
            independentEmitterWorldPos.Clear();
            if (bossBase == null) return;

            var points = bossBase.GetComponentsInChildren<EmitterPoint>();
            foreach (var p in points)
            {
                if (!emitters.ContainsKey(p.EmitterType))
                {
                    emitters.Add(p.EmitterType, p);
                }

                if (emittersIndependent)
                {
                    independentEmitterWorldPos[p] = p.transform.position;
                }
            }
        }

        void TriggerMove(MoveEvent e)
        {
            Transform target = null;
            EmitterPoint emitterPoint = null;
            if (e.target == MoveTarget.Boss)
            {
                if (bossBase != null)
                {
                    target = bossBase.transform;
                }
            }
            else
            {
                if (emitters.TryGetValue(e.emitterType, out var emitter))
                {
                    target = emitter.transform;
                    emitterPoint = emitter;
                }
            }

            if (target == null)
            {
                Debug.LogWarning("[BossMovementTimeline] Target not found for move event.");
                return;
            }

            if (activeMoveByTarget.TryGetValue(target, out var running) && running != null)
            {
                StopCoroutine(running);
            }
            var co = StartCoroutine(MoveCoroutine(target, e, emitterPoint));
            activeMoveByTarget[target] = co;
        }

        IEnumerator MoveCoroutine(Transform target, MoveEvent e, EmitterPoint emitterPoint)
        {
            if (e.moveType == TimelineMoveType.None)
            {
                yield break;
            }

            Vector3 startPos = target.position;
            Vector3 endPos = startPos;

            switch (e.moveType)
            {
                case TimelineMoveType.ToPosition:
                    endPos = e.targetPosition;
                    break;

                case TimelineMoveType.ByDirection:
                    Vector2 dir = new Vector2(
                        Mathf.Cos(e.moveDirection * Mathf.Deg2Rad),
                        Mathf.Sin(e.moveDirection * Mathf.Deg2Rad)
                    );
                    endPos = startPos + (Vector3)(dir.normalized * e.moveDistance);
                    break;

                case TimelineMoveType.Circle:
                    // 在协程中逐帧更新位置
                    break;

                case TimelineMoveType.TwoPointLoop:
                    // 在协程中逐帧更新位置
                    break;
            }

            float duration = Mathf.Max(0.001f, e.moveDuration);
            float elapsed = 0f;

            // 计算圆心（相对Boss位置）
            Vector2 circleCenterWorld = Vector2.zero;
            if (e.moveType == TimelineMoveType.Circle)
            {
                Vector2 bossPos = bossBase != null ? (Vector2)bossBase.transform.position : Vector2.zero;
                circleCenterWorld = bossPos + e.circleCenterOffset;
            }

            // 起始角度：由当前位置相对圆心得出
            float startAngle = 0f;
            if (e.moveType == TimelineMoveType.Circle)
            {
                Vector2 startOffset = (Vector2)startPos - circleCenterWorld;
                startAngle = Mathf.Atan2(startOffset.y, startOffset.x) * Mathf.Rad2Deg;
            }

            // 两点循环：先移动到A，再在A/B间循环
            Vector3 pointA = Vector3.zero;
            Vector3 pointB = Vector3.zero;
            Vector3 loopTarget = Vector3.zero;
            bool reachedA = false;
            if (e.moveType == TimelineMoveType.TwoPointLoop)
            {
                pointA = startPos + (Vector3)e.pointAOffset;
                pointB = startPos + (Vector3)e.pointBOffset;
                loopTarget = pointA;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (e.moveType == TimelineMoveType.Circle)
                {
                    float radius = e.circleRadius;
                    if (radius <= 0f)
                    {
                        radius = Vector2.Distance(circleCenterWorld, startPos);
                    }
                    float angle = startAngle + e.circleAngularSpeed * (t * duration);
                    float rad = angle * Mathf.Deg2Rad;
                    Vector3 pos = new Vector3(
                        circleCenterWorld.x + Mathf.Cos(rad) * radius,
                        circleCenterWorld.y + Mathf.Sin(rad) * radius,
                        startPos.z
                    );
                    target.position = pos;
                }
                else if (e.moveType == TimelineMoveType.TwoPointLoop)
                {
                    float speed = Mathf.Max(0.001f, e.moveSpeed);
                    if (!reachedA)
                    {
                        target.position = Vector3.MoveTowards(target.position, pointA, speed * Time.deltaTime);
                        if (Vector3.Distance(target.position, pointA) <= 0.001f)
                        {
                            reachedA = true;
                            loopTarget = pointB;
                        }
                    }
                    else
                    {
                        target.position = Vector3.MoveTowards(target.position, loopTarget, speed * Time.deltaTime);
                        if (Vector3.Distance(target.position, loopTarget) <= 0.001f)
                        {
                            loopTarget = (loopTarget == pointA) ? pointB : pointA;
                        }
                    }
                }
                else
                {
                    target.position = Vector3.Lerp(startPos, endPos, t);
                }

                if (emittersIndependent && emitterPoint != null)
                {
                    independentEmitterWorldPos[emitterPoint] = target.position;
                }
                yield return null;
            }
            if (e.moveType == TimelineMoveType.Circle)
            {
                float radius = e.circleRadius <= 0f ? Vector2.Distance(circleCenterWorld, startPos) : e.circleRadius;
                float angle = startAngle + e.circleAngularSpeed * duration;
                float rad = angle * Mathf.Deg2Rad;
                target.position = new Vector3(
                    circleCenterWorld.x + Mathf.Cos(rad) * radius,
                    circleCenterWorld.y + Mathf.Sin(rad) * radius,
                    startPos.z
                );
            }
            else if (e.moveType == TimelineMoveType.TwoPointLoop)
            {
                // 持续时间结束后回到起始点
                target.position = startPos;
            }
            else
            {
                target.position = endPos;
            }

            if (emittersIndependent && emitterPoint != null)
            {
                independentEmitterWorldPos[emitterPoint] = target.position;
            }
            if (activeMoveByTarget.ContainsKey(target))
            {
                activeMoveByTarget[target] = null;
            }
        }
    }
}
