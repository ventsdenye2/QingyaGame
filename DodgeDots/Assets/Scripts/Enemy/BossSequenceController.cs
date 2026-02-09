using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DodgeDots.Audio;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// Boss序列控制器
    /// 订阅BeatMapPlayer的节拍事件，每个节拍执行下一个攻击和移动
    /// 支持多个控制器同时运行，实现复杂的Boss行为
    /// </summary>
    public class BossSequenceController : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("BeatMapPlayer引用")]
        public BeatMapPlayer beatMapPlayer;

        [Tooltip("Boss引用")]
        public BossBase bossBase;

        [Header("配置")]
        [Tooltip("序列配置")]
        public BossSequenceConfig sequenceConfig;

        [Header("序列开关")]
        [Tooltip("是否执行移动序列")]
        public bool enableMoveSequence = true;

        [Header("调试")]
        [Tooltip("是否显示调试日志")]
        public bool showDebugLog = false;

        // 内部状态
        private int _attackIndex = 0;
        private int _moveIndex = 0;
        private int _lastHandledBeat = -1;
        private bool _subscribed = false;
        private Dictionary<EmitterType, Coroutine> _activeMoveCoroutines = new Dictionary<EmitterType, Coroutine>();
        private readonly Dictionary<Transform, Vector3> _defaultPositions = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Transform> _emitterOriginalParents = new Dictionary<Transform, Transform>();
        private Transform _emittersRoot;
        private bool _emittersDetached;

        void OnEnable()
        {
            // 查找引用
            if (beatMapPlayer == null)
            {
                beatMapPlayer = FindObjectOfType<BeatMapPlayer>();
            }

            if (bossBase == null)
            {
                bossBase = GetComponent<BossBase>();
            }

            CacheDefaultPositions();
            StartCoroutine(DetachEmittersWhenReady());

            // 订阅节拍事件
            if (beatMapPlayer != null && !_subscribed)
            {
                beatMapPlayer.OnBeat += HandleBeat;
                beatMapPlayer.OnLoop += HandleBeatLoop;
                _subscribed = true;
                if (showDebugLog)
                {
                    Debug.Log($"[BossSequenceController] {sequenceConfig?.configName} subscribed to BeatMapPlayer");
                }
            }
            else if (beatMapPlayer == null)
            {
                Debug.LogError("[BossSequenceController] No BeatMapPlayer found!");
            }
        }

        void OnDisable()
        {
            // 取消订阅
            if (beatMapPlayer != null && _subscribed)
            {
                beatMapPlayer.OnBeat -= HandleBeat;
                beatMapPlayer.OnLoop -= HandleBeatLoop;
                _subscribed = false;
            }

            // 停止所有移动协程
            StopAllMoveCoroutines();
        }

        void OnDestroy()
        {
            ReattachEmitters();
        }

        /// <summary>
        /// 处理节拍事件
        /// </summary>
        void HandleBeat(int beatIndex)
        {
            // 防止重复处理
            if (beatIndex <= _lastHandledBeat)
            {
                return;
            }
            _lastHandledBeat = beatIndex;

            if (sequenceConfig == null)
            {
                Debug.LogWarning("[BossSequenceController] No sequence config assigned!");
                return;
            }

            // 执行攻击
            ExecuteNextAttack();

            // 执行移动
            if (enableMoveSequence)
            {
                ExecuteNextMove();
            }
        }

        void HandleBeatLoop(int loopIndex)
        {
            ResetSequence();
        }

        /// <summary>
        /// 执行下一个攻击
        /// </summary>
        void ExecuteNextAttack()
        {
            if (sequenceConfig.attackSequence == null || sequenceConfig.attackSequence.Length == 0)
            {
                return;
            }

            // 获取当前攻击
            BossAttackAction attackAction = sequenceConfig.attackSequence[_attackIndex];

            if (showDebugLog)
            {
                Debug.Log($"[BossSequenceController] Executing attack {_attackIndex}: {attackAction.attackName}");
            }

            // 调用BossBase执行攻击
            if (bossBase != null)
            {
                bossBase.ExecuteAttackAction(attackAction);
            }

            // 推进攻击索引
            _attackIndex++;
            if (_attackIndex >= sequenceConfig.attackSequence.Length)
            {
                if (sequenceConfig.loopAttackSequence)
                {
                    _attackIndex = 0;
                }
                else
                {
                    _attackIndex = sequenceConfig.attackSequence.Length - 1;
                }
            }
        }

        /// <summary>
        /// 执行下一个移动
        /// </summary>
        void ExecuteNextMove()
        {
            if (sequenceConfig.moveSequence == null || sequenceConfig.moveSequence.Length == 0)
            {
                return;
            }

            // 获取当前移动
            EmitterMoveData moveData = sequenceConfig.moveSequence[_moveIndex];

            if (showDebugLog)
            {
                Debug.Log($"[BossSequenceController] Executing move {_moveIndex}: {moveData.emitterType} -> {moveData.moveType}");
            }

            // 执行移动
            ExecuteMove(moveData);

            // 推进移动索引
            _moveIndex++;
            if (_moveIndex >= sequenceConfig.moveSequence.Length)
            {
                if (sequenceConfig.loopMoveSequence)
                {
                    _moveIndex = 0;
                }
                else
                {
                    _moveIndex = sequenceConfig.moveSequence.Length - 1;
                }
            }
        }

        /// <summary>
        /// 执行移动
        /// </summary>
        void ExecuteMove(EmitterMoveData moveData)
        {
            if (moveData.useMultipleEmitters && moveData.multipleEmitters != null && moveData.multipleEmitters.Length > 0)
            {
                foreach (var emitterType in moveData.multipleEmitters)
                {
                    ExecuteMoveForEmitter(moveData, emitterType);
                }
                return;
            }

            ExecuteMoveForEmitter(moveData, moveData.emitterType);
        }

        void ExecuteMoveForEmitter(EmitterMoveData moveData, EmitterType emitterType)
        {
            if (moveData.moveType == BossMoveType.None)
            {
                Transform noneTargetTransform = GetEmitterTransform(emitterType);
                if (noneTargetTransform == null)
                {
                    Debug.LogWarning($"[BossSequenceController] Emitter {emitterType} not found!");
                    return;
                }

                if (_activeMoveCoroutines.ContainsKey(emitterType))
                {
                    if (_activeMoveCoroutines[emitterType] != null)
                    {
                        StopCoroutine(_activeMoveCoroutines[emitterType]);
                    }
                }

                Coroutine noneMoveCoroutine = StartCoroutine(MoveCoroutine(noneTargetTransform, moveData, emitterType));
                _activeMoveCoroutines[emitterType] = noneMoveCoroutine;
                return;
            }

            // 获取发射源位置
            Transform targetTransform = GetEmitterTransform(emitterType);
            if (targetTransform == null)
            {
                Debug.LogWarning($"[BossSequenceController] Emitter {emitterType} not found!");
                return;
            }

            // 停止该发射源的旧移动协程
            if (_activeMoveCoroutines.ContainsKey(emitterType))
            {
                if (_activeMoveCoroutines[emitterType] != null)
                {
                    StopCoroutine(_activeMoveCoroutines[emitterType]);
                }
            }

            // 启动新的移动协程
            Coroutine moveCoroutine = StartCoroutine(MoveCoroutine(targetTransform, moveData, emitterType));
            _activeMoveCoroutines[emitterType] = moveCoroutine;
        }

        /// <summary>
        /// 移动协程
        /// </summary>
        IEnumerator MoveCoroutine(Transform target, EmitterMoveData moveData, EmitterType emitterType)
        {
            Vector3 startPosition = target.position;
            Vector3 targetPosition = startPosition;
            Transform bossTransform = bossBase != null ? bossBase.transform : null;
            Vector3 defaultPosition = GetDefaultPosition(target);

            if (moveData.moveType == BossMoveType.None)
            {
                float preMoveTime = 0f;
                if (moveData.moveSpeed > 0f)
                {
                    float distance = Vector3.Distance(target.position, defaultPosition);
                    preMoveTime = distance / moveData.moveSpeed;
                }

                if (preMoveTime > 0f)
                {
                    float preElapsed = 0f;
                    Vector3 preStart = target.position;
                    while (preElapsed < preMoveTime)
                    {
                        preElapsed += Time.deltaTime;
                        float t = preElapsed / preMoveTime;
                        target.position = Vector3.Lerp(preStart, defaultPosition, t);
                        yield return null;
                    }
                }
                else
                {
                    target.position = defaultPosition;
                }
                yield break;
            }

            // 计算目标位置
            switch (moveData.moveType)
            {
                case BossMoveType.ToPosition:
                    if (moveData.useLocalSpace && bossTransform != null)
                    {
                        targetPosition = bossTransform.TransformPoint(moveData.targetPosition);
                    }
                    else
                    {
                        targetPosition = moveData.targetPosition;
                    }
                    break;

                case BossMoveType.ByDirection:
                    Vector2 direction = new Vector2(
                        Mathf.Cos(moveData.moveDirection * Mathf.Deg2Rad),
                        Mathf.Sin(moveData.moveDirection * Mathf.Deg2Rad)
                    );
                    targetPosition = startPosition + (Vector3)(direction * moveData.moveDistance);
                    break;

                case BossMoveType.Circle:
                case BossMoveType.TwoPointLoop:
                    break;

                case BossMoveType.Custom:
                    // 自定义移动需要在Boss子类中实现
                    if (bossBase != null)
                    {
                        bossBase.ExecuteCustomMove(moveData.emitterType, moveData.customMoveId);
                    }
                    yield break;
            }

            // 平滑移动
            float elapsedTime = 0f;
            if (moveData.moveType == BossMoveType.Circle)
            {
                // 先移动回默认位置
                float preMoveTime = 0f;
                if (moveData.moveSpeed > 0f)
                {
                    float distance = Vector3.Distance(target.position, defaultPosition);
                    preMoveTime = distance / moveData.moveSpeed;
                }
                if (preMoveTime > 0f)
                {
                    float preElapsed = 0f;
                    Vector3 preStart = target.position;
                    while (preElapsed < preMoveTime)
                    {
                        preElapsed += Time.deltaTime;
                        float t = preElapsed / preMoveTime;
                        target.position = Vector3.Lerp(preStart, defaultPosition, t);
                        yield return null;
                    }
                }
                else
                {
                    target.position = defaultPosition;
                }

                float remainingDuration = moveData.moveDuration > 0f ? Mathf.Max(0f, moveData.moveDuration - preMoveTime) : 0f;
                if (moveData.moveDuration <= 0f || moveData.circleRadius <= 0f || moveData.circleAngularSpeed == 0f)
                {
                    float angle = moveData.circleStartAngle * Mathf.Deg2Rad;
                    Vector3 center = moveData.circleCenter;
                    if (moveData.useLocalSpace && bossTransform != null)
                    {
                        center = defaultPosition + (Vector3)moveData.circleCenter;
                    }
                    else
                    {
                        center = defaultPosition + (Vector3)moveData.circleCenter;
                    }
                    target.position = new Vector3(
                        center.x + Mathf.Cos(angle) * moveData.circleRadius,
                        center.y + Mathf.Sin(angle) * moveData.circleRadius,
                        target.position.z
                    );
                    yield break;
                }

                float dir = moveData.circleClockwise ? -1f : 1f;
                while (elapsedTime < remainingDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float angle = (moveData.circleStartAngle + dir * moveData.circleAngularSpeed * elapsedTime) * Mathf.Deg2Rad;
                    Vector3 center = moveData.circleCenter;
                    if (moveData.useLocalSpace && bossTransform != null)
                    {
                        center = defaultPosition + (Vector3)moveData.circleCenter;
                    }
                    else
                    {
                        center = defaultPosition + (Vector3)moveData.circleCenter;
                    }
                    target.position = new Vector3(
                        center.x + Mathf.Cos(angle) * moveData.circleRadius,
                        center.y + Mathf.Sin(angle) * moveData.circleRadius,
                        target.position.z
                    );
                    yield return null;
                }
                yield break;
            }

            if (moveData.moveType == BossMoveType.TwoPointLoop)
            {
                Vector3 a = moveData.pointA;
                Vector3 b = moveData.pointB;
                if (moveData.useLocalSpace && bossTransform != null)
                {
                    a = defaultPosition + (Vector3)moveData.pointA;
                    b = defaultPosition + (Vector3)moveData.pointB;
                }
                else
                {
                    a = defaultPosition + (Vector3)moveData.pointA;
                    b = defaultPosition + (Vector3)moveData.pointB;
                }
                if (!moveData.startFromA)
                {
                    Vector3 temp = a;
                    a = b;
                    b = temp;
                }

                // 先移动到起点A
                float preMoveTime = 0f;
                if (moveData.moveSpeed > 0f)
                {
                    float distanceToA = Vector3.Distance(target.position, a);
                    preMoveTime = distanceToA / moveData.moveSpeed;
                }
                if (preMoveTime > 0f)
                {
                    float preElapsed = 0f;
                    Vector3 preStart = target.position;
                    while (preElapsed < preMoveTime)
                    {
                        preElapsed += Time.deltaTime;
                        float t = preElapsed / preMoveTime;
                        target.position = Vector3.Lerp(preStart, a, t);
                        yield return null;
                    }
                }
                else
                {
                    target.position = a;
                }

                float length = Vector3.Distance(a, b);
                float remainingDuration = moveData.moveDuration > 0f ? Mathf.Max(0f, moveData.moveDuration - preMoveTime) : 0f;
                if (remainingDuration <= 0f || length <= 0.0001f || moveData.loopSpeed <= 0f)
                {
                    target.position = a;
                    yield break;
                }

                while (elapsedTime < remainingDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float distance = Mathf.PingPong(elapsedTime * moveData.loopSpeed, length);
                    float t = distance / length;
                    target.position = Vector3.Lerp(a, b, t);
                    yield return null;
                }
                yield break;
            }

            while (elapsedTime < moveData.moveDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveData.moveDuration;
                target.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            target.position = targetPosition;
        }

        /// <summary>
        /// 获取发射源的Transform
        /// </summary>
        Transform GetEmitterTransform(EmitterType emitterType)
        {
            if (bossBase == null)
            {
                return null;
            }

            // 如果是MainCore，返回Boss自身
            if (emitterType == EmitterType.MainCore)
            {
                return bossBase.transform;
            }

            // 否则查找发射源
            EmitterPoint emitter = bossBase.GetEmitter(emitterType);
            if (emitter != null)
            {
                return emitter.transform;
            }

            return null;
        }

        /// <summary>
        /// 停止所有移动协程
        /// </summary>
        void StopAllMoveCoroutines()
        {
            foreach (var coroutine in _activeMoveCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _activeMoveCoroutines.Clear();
        }

        private void CacheDefaultPositions()
        {
            _defaultPositions.Clear();
            if (bossBase == null) return;

            _defaultPositions[bossBase.transform] = bossBase.transform.position;

            foreach (var emitter in GetComponentsInChildren<EmitterPoint>())
            {
                if (emitter != null)
                {
                    _defaultPositions[emitter.transform] = emitter.transform.position;
                }
            }
        }

        private void DetachEmittersFromBoss()
        {
            if (bossBase == null) return;
            if (_emittersDetached) return;

            if (_emittersRoot == null)
            {
                GameObject root = new GameObject("EmittersRoot(Runtime)");
                _emittersRoot = root.transform;
                _emittersRoot.SetParent(bossBase.transform.parent, worldPositionStays: true);
            }

            foreach (var emitter in GetComponentsInChildren<EmitterPoint>())
            {
                if (emitter == null) continue;
                Transform t = emitter.transform;
                if (_emitterOriginalParents.ContainsKey(t)) continue;
                _emitterOriginalParents[t] = t.parent;
                t.SetParent(_emittersRoot, worldPositionStays: true);
            }
            _emittersDetached = true;
        }

        private void ReattachEmitters()
        {
            if (_emittersRoot != null && !_emittersRoot.gameObject.activeInHierarchy)
            {
                return;
            }
            foreach (var kvp in _emitterOriginalParents)
            {
                if (kvp.Key == null) continue;
                kvp.Key.SetParent(kvp.Value, worldPositionStays: true);
            }
            _emitterOriginalParents.Clear();
            _emittersDetached = false;
        }

        private IEnumerator DetachEmittersWhenReady()
        {
            // 等待 BossBase.Start 完成初始化（注册发射点）
            yield return null;
            yield return null;
            DetachEmittersFromBoss();
        }

        private Vector3 GetDefaultPosition(Transform target)
        {
            if (target == null) return Vector3.zero;
            if (_defaultPositions.TryGetValue(target, out var pos)) return pos;
            return target.position;
        }


        /// <summary>
        /// 重置序列（用于阶段切换）
        /// </summary>
        public void ResetSequence()
        {
            _attackIndex = 0;
            _moveIndex = 0;
            _lastHandledBeat = -1;
            StopAllMoveCoroutines();

            if (showDebugLog)
            {
                Debug.Log($"[BossSequenceController] {sequenceConfig?.configName} sequence reset");
            }
        }
    }
}
