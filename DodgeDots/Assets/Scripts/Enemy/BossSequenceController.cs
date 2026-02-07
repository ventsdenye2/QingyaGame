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

        [Header("调试")]
        [Tooltip("是否显示调试日志")]
        public bool showDebugLog = false;

        // 内部状态
        private int _attackIndex = 0;
        private int _moveIndex = 0;
        private int _lastHandledBeat = -1;
        private bool _subscribed = false;
        private Dictionary<EmitterType, Coroutine> _activeMoveCoroutines = new Dictionary<EmitterType, Coroutine>();

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

            // 订阅节拍事件
            if (beatMapPlayer != null && !_subscribed)
            {
                beatMapPlayer.OnBeat += HandleBeat;
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
                _subscribed = false;
            }

            // 停止所有移动协程
            StopAllMoveCoroutines();
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
            ExecuteNextMove();
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
            if (moveData.moveType == BossMoveType.None)
            {
                return;
            }

            // 获取发射源位置
            Transform targetTransform = GetEmitterTransform(moveData.emitterType);
            if (targetTransform == null)
            {
                Debug.LogWarning($"[BossSequenceController] Emitter {moveData.emitterType} not found!");
                return;
            }

            // 停止该发射源的旧移动协程
            if (_activeMoveCoroutines.ContainsKey(moveData.emitterType))
            {
                if (_activeMoveCoroutines[moveData.emitterType] != null)
                {
                    StopCoroutine(_activeMoveCoroutines[moveData.emitterType]);
                }
            }

            // 启动新的移动协程
            Coroutine moveCoroutine = StartCoroutine(MoveCoroutine(targetTransform, moveData));
            _activeMoveCoroutines[moveData.emitterType] = moveCoroutine;
        }

        /// <summary>
        /// 移动协程
        /// </summary>
        IEnumerator MoveCoroutine(Transform target, EmitterMoveData moveData)
        {
            Vector3 startPosition = target.position;
            Vector3 targetPosition = startPosition;

            // 计算目标位置
            switch (moveData.moveType)
            {
                case BossMoveType.ToPosition:
                    targetPosition = moveData.targetPosition;
                    break;

                case BossMoveType.ByDirection:
                    Vector2 direction = new Vector2(
                        Mathf.Cos(moveData.moveDirection * Mathf.Deg2Rad),
                        Mathf.Sin(moveData.moveDirection * Mathf.Deg2Rad)
                    );
                    targetPosition = startPosition + (Vector3)(direction * moveData.moveDistance);
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
            while (elapsedTime < moveData.moveDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveData.moveDuration;
                target.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            // 确保到达目标位置
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
