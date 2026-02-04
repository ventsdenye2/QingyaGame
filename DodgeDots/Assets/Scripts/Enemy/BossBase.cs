using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DodgeDots.Core;
using DodgeDots.Bullet;

namespace DodgeDots.Enemy
{
    /// <summary>
    /// Boss状态枚举
    /// </summary>
    public enum BossState
    {
        Idle,       // 空闲
        Fighting,   // 战斗中
        Defeated    // 被击败
    }

    /// <summary>
    /// Boss基类，提供可扩展的Boss行为框架
    /// </summary>
    public abstract class BossBase : MonoBehaviour, IHealth, IDamageable
    {
        [Header("Boss基础设置")]
        [SerializeField] protected float maxHealth = 1000f;
        [SerializeField] protected string bossName = "Boss";

        [Header("阶段设置")]
        [SerializeField] protected List<float> phaseHealthThresholds = new List<float> { 0.7f, 0.3f };

        [Header("攻击配置")]
        [SerializeField] protected BossAttackConfig attackConfig;

        [Header("文案配置")]
        [SerializeField] protected BossDialogueConfig dialogueConfig;

        [Header("音频设置")]
        [SerializeField] protected AudioSource audioSource;

        protected float _currentHealth;
        protected BossState _currentState;
        protected int _currentPhase;
        [System.NonSerialized] protected BulletManager _bulletManager;
        protected Coroutine _attackCoroutine;
        protected int _currentAttackIndex;

        // 发射源管理
        protected Dictionary<EmitterType, EmitterPoint> _emitters;
        protected EmitterPoint _defaultEmitter;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public bool CanTakeDamage => _currentState == BossState.Fighting;
        public BossState CurrentState => _currentState;
        public int CurrentPhase => _currentPhase;
        public string BossName => bossName;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;
        public event Action<int> OnPhaseChanged;
        public event Action<BossState> OnStateChanged;
        public event Action<BossPhaseDialogue> OnPhaseDialogue;

        protected virtual void Awake()
        {
            _currentHealth = maxHealth;
            _currentState = BossState.Idle;
            _currentPhase = 0;
            _currentAttackIndex = 0;
        }

        protected virtual void Start()
        {
            // 在Start中获取BulletManager，确保单例已初始化
            _bulletManager = BulletManager.Instance;

            if (_bulletManager == null)
            {
                Debug.LogError("BulletManager未找到！请确保场景中存在BulletManager组件。");
            }

            // 初始化AudioSource
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }

            // 初始化发射源
            InitializeEmitters();

            // 子类可以重写此方法进行初始化
        }

        /// <summary>
        /// 初始化发射源
        /// 自动查找Boss身上所有的EmitterPoint组件并注册
        /// </summary>
        protected virtual void InitializeEmitters()
        {
            _emitters = new Dictionary<EmitterType, EmitterPoint>();

            // 查找所有子物体上的EmitterPoint组件
            EmitterPoint[] emitterPoints = GetComponentsInChildren<EmitterPoint>();

            foreach (EmitterPoint emitter in emitterPoints)
            {
                if (!_emitters.ContainsKey(emitter.EmitterType))
                {
                    _emitters.Add(emitter.EmitterType, emitter);
                    Debug.Log($"注册发射源: {emitter.EmitterType} at {emitter.name}");
                }
                else
                {
                    Debug.LogWarning($"发射源类型 {emitter.EmitterType} 已存在，跳过 {emitter.name}");
                }
            }

            // 如果没有找到MainCore，使用Boss自身位置作为默认发射源
            if (!_emitters.ContainsKey(EmitterType.MainCore))
            {
                Debug.LogWarning("未找到MainCore发射源，将使用Boss自身位置作为默认发射源");
            }
        }

        public virtual void TakeDamage(float damage, GameObject source = null)
        {
            if (!CanTakeDamage) return;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            // 检查是否进入新阶段
            CheckPhaseTransition(previousHealth, _currentHealth);

            // 检查是否死亡
            if (_currentHealth <= 0)
            {
                OnBossDefeated();
            }
        }

        public virtual void Heal(float amount)
        {
            if (!IsAlive) return;

            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public virtual void ResetHealth()
        {
            _currentHealth = maxHealth;
            _currentPhase = 0;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public virtual void StartBattle()
        {
            SetState(BossState.Fighting);
            OnBattleStart();

            // 如果有攻击配置，启动攻击循环
            if (attackConfig != null && attackConfig.attackSequence != null && attackConfig.attackSequence.Length > 0)
            {
                _attackCoroutine = StartCoroutine(AttackLoopCoroutine());
            }
        }

        /// <summary>
        /// 检查阶段转换
        /// </summary>
        protected virtual void CheckPhaseTransition(float previousHealth, float currentHealth)
        {
            float currentHealthPercent = currentHealth / maxHealth;

            // 遍历所有阶段阈值，找到当前应该处于的最高阶段
            int targetPhase = 0;
            for (int i = 0; i < phaseHealthThresholds.Count; i++)
            {
                if (currentHealthPercent <= phaseHealthThresholds[i])
                {
                    targetPhase = i + 1;
                }
                else
                {
                    break; // 因为列表是降序的，一旦血量比例高于某个阈值就可以停止
                }
            }

            // 只有当进入新阶段时才触发
            if (targetPhase > _currentPhase)
            {
                EnterPhase(targetPhase);
            }
        }

        /// <summary>
        /// 进入新阶段
        /// </summary>
        protected virtual void EnterPhase(int phase)
        {
            _currentPhase = phase;
            OnPhaseChanged?.Invoke(_currentPhase);

            // 触发阶段文案
            if (dialogueConfig != null)
            {
                BossPhaseDialogue dialogue = dialogueConfig.GetDialogueByPhase(_currentPhase);
                if (dialogue != null)
                {
                    OnPhaseDialogue?.Invoke(dialogue);
                }
            }

            OnPhaseEnter(_currentPhase);
        }

        /// <summary>
        /// 设置Boss状态
        /// </summary>
        protected virtual void SetState(BossState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            OnStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Boss被击败时调用
        /// </summary>
        protected virtual void OnBossDefeated()
        {
            StopAttackLoop();
            StopBackgroundMusic();
            SetState(BossState.Defeated);
            OnDeath?.Invoke();
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        protected virtual void PlayBackgroundMusic()
        {
            if (audioSource == null || attackConfig == null || attackConfig.backgroundMusic == null)
            {
                return;
            }

            audioSource.clip = attackConfig.backgroundMusic;
            audioSource.volume = attackConfig.musicVolume;
            audioSource.loop = attackConfig.loopMusic;
            audioSource.Play();

            Debug.Log($"播放背景音乐: {attackConfig.backgroundMusic.name}");
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        protected virtual void StopBackgroundMusic()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// 攻击循环协程
        /// </summary>
        protected virtual IEnumerator AttackLoopCoroutine()
        {
            // 播放背景音乐
            PlayBackgroundMusic();

            while (_currentState == BossState.Fighting && attackConfig != null)
            {
                // 执行当前攻击
                BossAttackData currentAttack = attackConfig.attackSequence[_currentAttackIndex];
                yield return StartCoroutine(ExecuteAttackCoroutine(currentAttack));

                // 移动到下一个攻击
                _currentAttackIndex++;

                // 检查是否需要循环
                if (_currentAttackIndex >= attackConfig.attackSequence.Length)
                {
                    if (attackConfig.loopSequence)
                    {
                        _currentAttackIndex = 0;
                        // 完成一轮后的延迟
                        if (attackConfig.delayAfterLoop > 0)
                        {
                            yield return new WaitForSeconds(attackConfig.delayAfterLoop);
                        }
                    }
                    else
                    {
                        // 不循环则停止
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 执行单个攻击的协程
        /// </summary>
        protected virtual IEnumerator ExecuteAttackCoroutine(BossAttackData attackData)
        {
            // 攻击前延迟
            if (attackData.delayBeforeAttack > 0)
            {
                yield return new WaitForSeconds(attackData.delayBeforeAttack);
            }

            // 如果有Boss主体移动配置，执行Boss移动
            if (attackData.moveType != BossMoveType.None)
            {
                StartCoroutine(ExecuteMoveCoroutine(attackData));
            }

            // 如果有发射源移动配置，执行发射源移动
            if (attackData.moveEmitters && attackData.emitterMoves != null && attackData.emitterMoves.Length > 0)
            {
                foreach (EmitterMoveData emitterMove in attackData.emitterMoves)
                {
                    StartCoroutine(ExecuteEmitterMoveCoroutine(emitterMove));
                }
            }

            // 执行攻击
            ExecuteAttack(attackData);
        }

        /// <summary>
        /// 获取发射源位置
        /// </summary>
        protected virtual Vector2 GetEmitterPosition(EmitterType emitterType)
        {
            if (_emitters != null && _emitters.TryGetValue(emitterType, out EmitterPoint emitter))
            {
                return emitter.Position;
            }

            // 如果找不到指定的发射源，使用Boss自身位置
            Debug.LogWarning($"未找到发射源 {emitterType}，使用Boss自身位置");
            return transform.position;
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        protected virtual void ExecuteAttack(BossAttackData attackData)
        {
            if (_bulletManager == null || attackData == null)
            {
                Debug.LogWarning("BulletManager或AttackData为空，无法执行攻击");
                return;
            }

            // 支持多发射源同时发射
            if (attackData.useMultipleEmitters && attackData.multipleEmitters != null && attackData.multipleEmitters.Length > 0)
            {
                foreach (EmitterType emitterType in attackData.multipleEmitters)
                {
                    ExecuteSingleEmitterAttack(attackData, emitterType);
                }
            }
            else
            {
                // 单发射源发射
                ExecuteSingleEmitterAttack(attackData, attackData.emitterType);
            }
        }

        /// <summary>
        /// 从单个发射源执行攻击
        /// </summary>
        protected virtual void ExecuteSingleEmitterAttack(BossAttackData attackData, EmitterType emitterType)
        {
            Vector2 bossPosition = GetEmitterPosition(emitterType);

            // 如果使用组合攻击，执行所有子攻击
            if (attackData.useComboAttack && attackData.subAttacks != null && attackData.subAttacks.Length > 0)
            {
                foreach (SubAttackData subAttack in attackData.subAttacks)
                {
                    ExecuteSubAttack(subAttack, bossPosition);
                }
                return;
            }

            // 否则执行单一攻击模式
            switch (attackData.attackType)
            {
                case BossAttackType.Circle:
                    _bulletManager.SpawnCirclePattern(
                        bossPosition,
                        attackData.circleCount,
                        Team.Enemy,
                        attackData.bulletConfig,
                        attackData.circleStartAngle
                    );
                    break;

                case BossAttackType.Fan:
                    Vector2 fanDirection = new Vector2(
                        Mathf.Cos(attackData.fanCenterAngle * Mathf.Deg2Rad),
                        Mathf.Sin(attackData.fanCenterAngle * Mathf.Deg2Rad)
                    );
                    _bulletManager.SpawnFanPattern(
                        bossPosition,
                        fanDirection,
                        attackData.fanCount,
                        attackData.fanSpreadAngle,
                        Team.Enemy,
                        attackData.bulletConfig
                    );
                    break;

                case BossAttackType.Single:
                    Vector2 singleDirection = new Vector2(
                        Mathf.Cos(attackData.singleDirection * Mathf.Deg2Rad),
                        Mathf.Sin(attackData.singleDirection * Mathf.Deg2Rad)
                    );
                    _bulletManager.SpawnBullet(
                        bossPosition,
                        singleDirection,
                        Team.Enemy,
                        attackData.bulletConfig
                    );
                    break;

                case BossAttackType.Spiral:
                    SpawnSpiralPattern(bossPosition, attackData);
                    break;

                case BossAttackType.Flower:
                    SpawnFlowerPattern(bossPosition, attackData);
                    break;

                case BossAttackType.Aiming:
                    SpawnAimingPattern(bossPosition, attackData);
                    break;

                case BossAttackType.Custom:
                    OnCustomAttack(attackData);
                    break;
            }
        }

        /// <summary>
        /// 执行单个子攻击
        /// </summary>
        protected virtual void ExecuteSubAttack(SubAttackData subAttack, Vector2 position)
        {
            if (_bulletManager == null || subAttack == null)
            {
                return;
            }

            switch (subAttack.attackType)
            {
                case BossAttackType.Circle:
                    _bulletManager.SpawnCirclePattern(
                        position,
                        subAttack.circleCount,
                        Team.Enemy,
                        subAttack.bulletConfig,
                        subAttack.circleStartAngle
                    );
                    break;

                case BossAttackType.Fan:
                    Vector2 fanDirection = new Vector2(
                        Mathf.Cos(subAttack.fanCenterAngle * Mathf.Deg2Rad),
                        Mathf.Sin(subAttack.fanCenterAngle * Mathf.Deg2Rad)
                    );
                    _bulletManager.SpawnFanPattern(
                        position,
                        fanDirection,
                        subAttack.fanCount,
                        subAttack.fanSpreadAngle,
                        Team.Enemy,
                        subAttack.bulletConfig
                    );
                    break;

                case BossAttackType.Single:
                    Vector2 singleDirection = new Vector2(
                        Mathf.Cos(subAttack.singleDirection * Mathf.Deg2Rad),
                        Mathf.Sin(subAttack.singleDirection * Mathf.Deg2Rad)
                    );
                    _bulletManager.SpawnBullet(
                        position,
                        singleDirection,
                        Team.Enemy,
                        subAttack.bulletConfig
                    );
                    break;

                case BossAttackType.Spiral:
                    SpawnSpiralPattern(position, subAttack);
                    break;

                case BossAttackType.Flower:
                    SpawnFlowerPattern(position, subAttack);
                    break;

                case BossAttackType.Aiming:
                    SpawnAimingPattern(position, subAttack);
                    break;

                case BossAttackType.Custom:
                    // 自定义攻击暂不支持子攻击
                    Debug.LogWarning($"子攻击不支持Custom类型");
                    break;
            }
        }

        /// <summary>
        /// 生成螺旋弹幕
        /// </summary>
        protected virtual void SpawnSpiralPattern(Vector2 position, SubAttackData subAttack)
        {
            if (_bulletManager == null || subAttack.bulletConfig == null)
            {
                return;
            }

            float angleStep = (360f * subAttack.spiralTurns) / subAttack.spiralBulletCount;
            float currentAngle = subAttack.spiralStartAngle;

            for (int i = 0; i < subAttack.spiralBulletCount; i++)
            {
                // 计算螺旋半径（随角度增长）
                float radius = (i / (float)subAttack.spiralBulletCount) * subAttack.spiralRadiusGrowth;

                // 计算子弹方向
                float angleRad = currentAngle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                // 计算子弹起始位置（在螺旋线上）
                Vector2 spawnPos = position + direction * radius;

                // 发射子弹
                _bulletManager.SpawnBullet(spawnPos, direction, Team.Enemy, subAttack.bulletConfig);

                currentAngle += angleStep;
            }
        }

        /// <summary>
        /// 生成花型弹幕
        /// </summary>
        protected virtual void SpawnFlowerPattern(Vector2 position, SubAttackData subAttack)
        {
            if (_bulletManager == null || subAttack.bulletConfig == null)
            {
                return;
            }

            // 计算每个花瓣之间的角度
            float petalAngleStep = 360f / subAttack.flowerPetals;

            for (int petal = 0; petal < subAttack.flowerPetals; petal++)
            {
                // 计算花瓣的中心角度
                float petalCenterAngle = subAttack.flowerStartAngle + (petal * petalAngleStep);

                // 在每个花瓣内发射多个子弹
                for (int bullet = 0; bullet < subAttack.flowerBulletsPerPetal; bullet++)
                {
                    // 计算子弹在花瓣内的偏移角度
                    float offset = 0f;
                    if (subAttack.flowerBulletsPerPetal > 1)
                    {
                        offset = Mathf.Lerp(
                            -subAttack.flowerPetalSpread / 2f,
                            subAttack.flowerPetalSpread / 2f,
                            bullet / (float)(subAttack.flowerBulletsPerPetal - 1)
                        );
                    }

                    float finalAngle = petalCenterAngle + offset;
                    float angleRad = finalAngle * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                    _bulletManager.SpawnBullet(position, direction, Team.Enemy, subAttack.bulletConfig);
                }
            }
        }

        /// <summary>
        /// 生成自机狙弹幕（瞄准玩家）
        /// </summary>
        protected virtual void SpawnAimingPattern(Vector2 position, SubAttackData subAttack)
        {
            if (_bulletManager == null || subAttack.bulletConfig == null)
            {
                return;
            }

            // 查找玩家
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("未找到玩家，无法执行自机狙攻击");
                return;
            }

            // 计算瞄准方向
            Vector2 playerPos = player.transform.position;
            Vector2 aimDirection = (playerPos - position).normalized;

            // 如果需要预判玩家移动
            if (subAttack.aimingPredictMovement)
            {
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    // 简单的预判：假设子弹速度，计算到达时间，预测玩家位置
                    float bulletSpeed = subAttack.bulletConfig != null ? subAttack.bulletConfig.defaultSpeed : 5f;
                    float distance = Vector2.Distance(position, playerPos);
                    float timeToReach = distance / bulletSpeed;
                    Vector2 predictedPos = playerPos + playerRb.velocity * timeToReach;
                    aimDirection = (predictedPos - position).normalized;
                }
            }

            // 计算瞄准角度
            float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

            // 发射子弹
            if (subAttack.aimingBulletCount == 1 && subAttack.aimingSpreadAngle == 0f)
            {
                // 精确瞄准，单发
                _bulletManager.SpawnBullet(position, aimDirection, Team.Enemy, subAttack.bulletConfig);
            }
            else
            {
                // 有扩散角度或多发子弹
                float startAngle = aimAngle - (subAttack.aimingSpreadAngle / 2f);
                float angleStep = subAttack.aimingBulletCount > 1
                    ? subAttack.aimingSpreadAngle / (subAttack.aimingBulletCount - 1)
                    : 0f;

                for (int i = 0; i < subAttack.aimingBulletCount; i++)
                {
                    float currentAngle = startAngle + (angleStep * i);
                    float angleRad = currentAngle * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                    _bulletManager.SpawnBullet(position, direction, Team.Enemy, subAttack.bulletConfig);
                }
            }
        }

        // 重载方法：支持BossAttackData参数
        protected virtual void SpawnSpiralPattern(Vector2 position, BossAttackData attackData)
        {
            if (_bulletManager == null || attackData.bulletConfig == null)
            {
                return;
            }

            float angleStep = (360f * attackData.spiralTurns) / attackData.spiralBulletCount;
            float currentAngle = attackData.spiralStartAngle;

            for (int i = 0; i < attackData.spiralBulletCount; i++)
            {
                float radius = (i / (float)attackData.spiralBulletCount) * attackData.spiralRadiusGrowth;
                float angleRad = currentAngle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                Vector2 spawnPos = position + direction * radius;
                _bulletManager.SpawnBullet(spawnPos, direction, Team.Enemy, attackData.bulletConfig);
                currentAngle += angleStep;
            }
        }

        protected virtual void SpawnFlowerPattern(Vector2 position, BossAttackData attackData)
        {
            if (_bulletManager == null || attackData.bulletConfig == null)
            {
                return;
            }

            float petalAngleStep = 360f / attackData.flowerPetals;

            for (int petal = 0; petal < attackData.flowerPetals; petal++)
            {
                float petalCenterAngle = attackData.flowerStartAngle + (petal * petalAngleStep);

                for (int bullet = 0; bullet < attackData.flowerBulletsPerPetal; bullet++)
                {
                    float offset = 0f;
                    if (attackData.flowerBulletsPerPetal > 1)
                    {
                        offset = Mathf.Lerp(
                            -attackData.flowerPetalSpread / 2f,
                            attackData.flowerPetalSpread / 2f,
                            bullet / (float)(attackData.flowerBulletsPerPetal - 1)
                        );
                    }

                    float finalAngle = petalCenterAngle + offset;
                    float angleRad = finalAngle * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                    _bulletManager.SpawnBullet(position, direction, Team.Enemy, attackData.bulletConfig);
                }
            }
        }

        protected virtual void SpawnAimingPattern(Vector2 position, BossAttackData attackData)
        {
            if (_bulletManager == null || attackData.bulletConfig == null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("未找到玩家，无法执行自机狙攻击");
                return;
            }

            Vector2 playerPos = player.transform.position;
            Vector2 aimDirection = (playerPos - position).normalized;

            if (attackData.aimingPredictMovement)
            {
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    float bulletSpeed = attackData.bulletConfig != null ? attackData.bulletConfig.defaultSpeed : 5f;
                    float distance = Vector2.Distance(position, playerPos);
                    float timeToReach = distance / bulletSpeed;
                    Vector2 predictedPos = playerPos + playerRb.velocity * timeToReach;
                    aimDirection = (predictedPos - position).normalized;
                }
            }

            float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

            if (attackData.aimingBulletCount == 1 && attackData.aimingSpreadAngle == 0f)
            {
                _bulletManager.SpawnBullet(position, aimDirection, Team.Enemy, attackData.bulletConfig);
            }
            else
            {
                float startAngle = aimAngle - (attackData.aimingSpreadAngle / 2f);
                float angleStep = attackData.aimingBulletCount > 1
                    ? attackData.aimingSpreadAngle / (attackData.aimingBulletCount - 1)
                    : 0f;

                for (int i = 0; i < attackData.aimingBulletCount; i++)
                {
                    float currentAngle = startAngle + (angleStep * i);
                    float angleRad = currentAngle * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                    _bulletManager.SpawnBullet(position, direction, Team.Enemy, attackData.bulletConfig);
                }
            }
        }

        /// <summary>
        /// 停止攻击循环
        /// </summary>
        protected virtual void StopAttackLoop()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }
        }

        /// <summary>
        /// 自定义攻击（由子类重写实现特殊攻击）
        /// </summary>
        protected virtual void OnCustomAttack(BossAttackData attackData)
        {
            Debug.LogWarning($"自定义攻击 {attackData.customAttackId} 未实现");
        }

        /// <summary>
        /// 执行移动的协程
        /// </summary>
        protected virtual IEnumerator ExecuteMoveCoroutine(BossAttackData attackData)
        {
            Vector3 startPosition = transform.position;
            float elapsedTime = 0f;

            switch (attackData.moveType)
            {
                case BossMoveType.ToPosition:
                    // 移动到目标位置
                    while (elapsedTime < attackData.moveDuration)
                    {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / attackData.moveDuration;
                        transform.position = Vector3.Lerp(startPosition, attackData.targetPosition, t);
                        yield return null;
                    }
                    transform.position = attackData.targetPosition;
                    break;

                case BossMoveType.ByDirection:
                    // 沿方向移动
                    Vector2 direction = new Vector2(
                        Mathf.Cos(attackData.moveDirection * Mathf.Deg2Rad),
                        Mathf.Sin(attackData.moveDirection * Mathf.Deg2Rad)
                    );
                    Vector3 targetPos = startPosition + (Vector3)(direction * attackData.moveDistance);

                    while (elapsedTime < attackData.moveDuration)
                    {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / attackData.moveDuration;
                        transform.position = Vector3.Lerp(startPosition, targetPos, t);
                        yield return null;
                    }
                    transform.position = targetPos;
                    break;

                case BossMoveType.Custom:
                    // 自定义移动
                    yield return StartCoroutine(OnCustomMove(attackData));
                    break;
            }
        }

        /// <summary>
        /// 自定义移动（由子类重写实现特殊移动）
        /// </summary>
        protected virtual IEnumerator OnCustomMove(BossAttackData attackData)
        {
            Debug.LogWarning($"自定义移动 {attackData.customMoveId} 未实现");
            yield return null;
        }

        /// <summary>
        /// 执行发射源移动的协程
        /// </summary>
        protected virtual IEnumerator ExecuteEmitterMoveCoroutine(EmitterMoveData emitterMove)
        {
            // 获取发射源
            if (_emitters == null || !_emitters.TryGetValue(emitterMove.emitterType, out EmitterPoint emitter))
            {
                Debug.LogWarning($"未找到发射源 {emitterMove.emitterType}，无法执行移动");
                yield break;
            }

            Transform emitterTransform = emitter.transform;
            Vector3 startPosition = emitterTransform.position;
            float elapsedTime = 0f;

            switch (emitterMove.moveType)
            {
                case BossMoveType.ToPosition:
                    // 移动到目标位置
                    while (elapsedTime < emitterMove.moveDuration)
                    {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / emitterMove.moveDuration;
                        emitterTransform.position = Vector3.Lerp(startPosition, emitterMove.targetPosition, t);
                        yield return null;
                    }
                    emitterTransform.position = emitterMove.targetPosition;
                    break;

                case BossMoveType.ByDirection:
                    // 沿方向移动
                    Vector2 direction = new Vector2(
                        Mathf.Cos(emitterMove.moveDirection * Mathf.Deg2Rad),
                        Mathf.Sin(emitterMove.moveDirection * Mathf.Deg2Rad)
                    );
                    Vector3 targetPos = startPosition + (Vector3)(direction * emitterMove.moveDistance);

                    while (elapsedTime < emitterMove.moveDuration)
                    {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / emitterMove.moveDuration;
                        emitterTransform.position = Vector3.Lerp(startPosition, targetPos, t);
                        yield return null;
                    }
                    emitterTransform.position = targetPos;
                    break;

                case BossMoveType.Custom:
                    // 自定义移动暂不支持发射源
                    Debug.LogWarning($"发射源移动暂不支持Custom类型");
                    break;
            }
        }

        // 以下方法由子类实现具体的Boss行为
        protected abstract void OnBattleStart();
        protected abstract void OnPhaseEnter(int phase);
    }
}
