# Boss战斗系统分析文档（重构版）

## 📋 目录

1. [系统概述](#系统概述)
2. [核心架构](#核心架构)
3. [序列配置系统](#序列配置系统)
4. [序列控制器](#序列控制器)
5. [Boss基类](#boss基类)
6. [发射源系统](#发射源系统)
7. [阶段系统](#阶段系统)
8. [攻击类型详解](#攻击类型详解)
9. [配置和使用](#配置和使用)
10. [最佳实践](#最佳实践)

---

## 系统概述

DodgeDots的Boss战斗系统经过重构，现在是一个**高度简化、节拍驱动**的弹幕射击游戏框架。新系统的核心特点：

- ✅ **攻击与移动分离**：攻击序列和移动序列独立配置，互不干扰
- ✅ **统一的节拍驱动**：所有攻击和移动都由BeatMap节拍事件驱动
- ✅ **多序列叠加**：支持多个序列控制器同时运行，实现复杂Boss行为
- ✅ **多发射源系统**：每个发射源可以独立移动和攻击
- ✅ **阶段系统**：根据血量自动切换不同的序列控制器组合
- ✅ **灵活的攻击类型**：圆形、扇形、单发、自机狙等多种弹幕模式

### 核心设计理念

**简化原则**：
- 移除了复杂的自动攻击循环系统
- 移除了攻击内嵌移动系统
- 移除了独立的时间轴移动系统
- 统一使用节拍驱动，每个节拍执行下一个攻击和下一个移动

**模块化原则**：
- 每个`BossSequenceController`管理一个独立的攻击+移动序列
- 多个Controller可以同时运行，互不干扰
- 通过启用/禁用Controller实现阶段切换

### 核心文件列表

| 文件 | 位置 | 功能 |
|------|------|------|
| `BossBase.cs` | Assets/Scripts/Enemy/ | Boss基类，提供核心框架（简化版） |
| `BossSequenceConfig.cs` | Assets/Scripts/Enemy/ | 序列配置（包含BeatMap、攻击序列、移动序列） |
| `BossSequenceController.cs` | Assets/Scripts/Enemy/ | 序列控制器（节拍驱动） |
| `BossAttackConfig.cs` | Assets/Scripts/Enemy/ | 攻击数据结构（保留用于向后兼容） |
| `EmitterPoint.cs` | Assets/Scripts/Enemy/ | 发射源标记组件 |
| `EmitterType.cs` | Assets/Scripts/Enemy/ | 发射源类型枚举 |
| `ExampleBoss.cs` | Assets/Scripts/Enemy/ | 示例Boss实现 |

---

## 核心架构

### 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                         Boss GameObject                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ BossBase (抽象基类)                                     │ │
│  │  - 健康管理                                             │ │
│  │  - 阶段系统                                             │ │
│  │  - 发射源管理                                           │ │
│  │  - 攻击执行方法                                         │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ BossSequenceController #1                              │ │
│  │  - 订阅 BeatMapPlayer.OnBeat                           │ │
│  │  - 管理 BossSequenceConfig #1                          │ │
│  │  - 每个节拍执行下一个攻击和移动                        │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ BossSequenceController #2                              │ │
│  │  - 订阅 BeatMapPlayer.OnBeat                           │ │
│  │  - 管理 BossSequenceConfig #2                          │ │
│  │  - 每个节拍执行下一个攻击和移动                        │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ EmitterPoint (子物体)                                   │ │
│  │  - MainCore / LeftHand / RightHand / ...               │ │
│  │  - 可以独立移动                                         │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ 订阅节拍事件
                            ▼
                  ┌──────────────────┐
                  │ BeatMapPlayer    │
                  │  - 播放BeatMap   │
                  │  - 触发OnBeat    │
                  └──────────────────┘
                            │
                            │ 基于同一首BGM
                            ▼
                  ┌──────────────────┐
                  │ BGMManager       │
                  │  - 播放BGM       │
                  │  - 提供DSP时间   │
                  └──────────────────┘
```

### 类图关系

```
BossBase (抽象基类)
    ├── 实现 IHealth 接口
    ├── 实现 IDamageable 接口
    ├── 管理 Dictionary<EmitterType, EmitterPoint>
    ├── 提供 ExecuteAttackAction(BossAttackAction)
    ├── 提供 GetEmitter(EmitterType)
    ├── 提供 ExecuteCustomMove(EmitterType, int)
    └── 被 ExampleBoss 继承

BossSequenceConfig (ScriptableObject)
    ├── 包含 BeatMap 引用
    ├── 包含 BossAttackAction[] 攻击序列
    └── 包含 EmitterMoveData[] 移动序列

BossSequenceController (MonoBehaviour)
    ├── 订阅 BeatMapPlayer.OnBeat 事件
    ├── 管理一个 BossSequenceConfig
    ├── 维护攻击索引和移动索引
    └── 每个节拍执行下一个攻击和移动

EmitterPoint (MonoBehaviour)
    ├── 标记发射源位置
    ├── 提供 Position 属性
    └── 可以被 BossSequenceController 移动
```

### Boss状态机

Boss有三个主要状态：

```csharp
public enum BossState
{
    Idle,       // 空闲状态
    Fighting,   // 战斗中
    Defeated    // 被击败
}
```

**状态转换流程：**
1. `Idle` → `Fighting`：调用 `StartBattle()` 时
2. `Fighting` → `Defeated`：血量降至0时
3. 只有在 `Fighting` 状态下才能受到伤害和执行攻击

---

## 序列配置系统

### BossSequenceConfig（ScriptableObject）

`BossSequenceConfig` 是新系统的核心配置文件，包含了完整的攻击和移动序列。

**文件位置：** [BossSequenceConfig.cs](Assets/Scripts/Enemy/BossSequenceConfig.cs)

#### 核心结构

```csharp
[CreateAssetMenu(fileName = "BossSequenceConfig", menuName = "DodgeDots/Boss Sequence Config")]
public class BossSequenceConfig : ScriptableObject
{
    [Header("配置信息")]
    public string configName = "Boss Sequence Config";

    [Header("攻击序列")]
    public BossAttackAction[] attackSequence;  // 攻击序列
    public bool loopAttackSequence = true;     // 是否循环

    [Header("移动序列")]
    public EmitterMoveData[] moveSequence;     // 移动序列
    public bool loopMoveSequence = true;       // 是否循环
}
```

#### 配置参数说明

| 参数 | 类型 | 说明 |
|------|------|------|
| `configName` | string | 配置名称（用于调试） |
| `attackSequence` | BossAttackAction[] | 攻击序列，每个节拍执行下一个 |
| `loopAttackSequence` | bool | 攻击序列是否循环 |
| `moveSequence` | EmitterMoveData[] | 移动序列，每个节拍执行下一个 |
| `loopMoveSequence` | bool | 移动序列是否循环 |

**重要说明**：
- BossSequenceConfig **不包含** BeatMap 引用
- BeatMap 由 **BeatMapPlayer** 挂载和管理
- BossSequenceController 订阅 BeatMapPlayer 的节拍事件来驱动序列执行

### BossAttackAction（攻击动作）

`BossAttackAction` 是简化的攻击数据结构，**不包含移动参数**。

**文件位置：** [BossSequenceConfig.cs](Assets/Scripts/Enemy/BossSequenceConfig.cs)

#### 核心结构

```csharp
[System.Serializable]
public class BossAttackAction
{
    [Header("基础设置")]
    public string attackName = "Attack";

    [Header("发射源设置")]
    public EmitterType emitterType = EmitterType.MainCore;
    public bool useMultipleEmitters = false;
    public EmitterType[] multipleEmitters = new EmitterType[0];

    [Header("攻击类型")]
    public BossAttackType attackType = BossAttackType.Circle;
    public BulletConfig bulletConfig;

    // 各种攻击类型的参数（Circle、Fan、Single、Aiming等）
    // ...
}
```

#### 关键特点

- **不包含移动参数**：移动逻辑完全独立
- **支持多发射源**：可以从多个发射源同时发射
- **简化的结构**：只关注攻击本身

### EmitterMoveData（移动数据）

`EmitterMoveData` 定义单个发射源的移动行为。

**文件位置：** [BossAttackConfig.cs](Assets/Scripts/Enemy/BossAttackConfig.cs)

#### 核心结构

```csharp
[System.Serializable]
public class EmitterMoveData
{
    [Header("发射源")]
    public EmitterType emitterType = EmitterType.MainCore;

    [Header("移动设置")]
    public BossMoveType moveType = BossMoveType.None;
    public float moveDuration = 1f;
    public float moveSpeed = 5f;

    // ToPosition模式
    public Vector2 targetPosition = Vector2.zero;

    // ByDirection模式
    public float moveDirection = 0f;
    public float moveDistance = 5f;

    // Custom模式
    public int customMoveId = 0;
}
```

#### 移动类型

```csharp
public enum BossMoveType
{
    None,           // 不移动
    ToPosition,     // 移动到目标位置
    ByDirection,    // 沿方向移动
    Custom          // 自定义移动
}
```

---

## 序列控制器

### BossSequenceController

`BossSequenceController` 是新系统的核心控制器，负责订阅节拍事件并执行攻击和移动。

**文件位置：** [BossSequenceController.cs](Assets/Scripts/Enemy/BossSequenceController.cs)

#### 核心功能

1. **订阅节拍事件**：订阅 `BeatMapPlayer.OnBeat` 事件
2. **执行攻击序列**：每个节拍执行下一个攻击
3. **执行移动序列**：每个节拍执行下一个移动
4. **支持循环**：攻击和移动序列可以独立循环
5. **防止重复触发**：使用 `_lastHandledBeat` 防止重复处理

#### 核心代码

**节拍处理：**

```csharp
void HandleBeat(int beatIndex)
{
    // 防止重复处理
    if (beatIndex <= _lastHandledBeat)
    {
        return;
    }
    _lastHandledBeat = beatIndex;

    // 执行攻击
    ExecuteNextAttack();

    // 执行移动
    ExecuteNextMove();
}
```

**攻击执行：**

```csharp
void ExecuteNextAttack()
{
    if (sequenceConfig.attackSequence == null ||
        sequenceConfig.attackSequence.Length == 0)
    {
        return;
    }

    // 获取当前攻击
    BossAttackAction attackAction = sequenceConfig.attackSequence[_attackIndex];

    // 调用BossBase执行攻击
    bossBase.ExecuteAttackAction(attackAction);

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
```

**移动执行：**

```csharp
void ExecuteNextMove()
{
    if (sequenceConfig.moveSequence == null ||
        sequenceConfig.moveSequence.Length == 0)
    {
        return;
    }

    // 获取当前移动
    EmitterMoveData moveData = sequenceConfig.moveSequence[_moveIndex];

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
```

**移动协程：**

```csharp
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
            bossBase.ExecuteCustomMove(moveData.emitterType, moveData.customMoveId);
            yield break;
    }

    // 平滑移动（Lerp）
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
```

#### 关键特点

- **每个节拍同时执行攻击和移动**：攻击和移动是并行的
- **独立的索引管理**：攻击索引和移动索引独立维护
- **支持多个控制器**：可以在Boss上挂载多个Controller，同时运行
- **移动协程管理**：每个发射源的移动协程独立管理，避免冲突

---

## Boss基类

### BossBase（简化版）

`BossBase` 经过简化，移除了自动攻击循环和攻击内嵌移动系统。

**文件位置：** [BossBase.cs](Assets/Scripts/Enemy/BossBase.cs)

#### 核心功能

1. **健康管理**：实现 `IHealth` 和 `IDamageable` 接口
2. **阶段系统**：根据血量阈值自动切换阶段
3. **发射源管理**：管理所有 `EmitterPoint` 组件
4. **攻击执行**：提供 `ExecuteAttackAction()` 方法供Controller调用
5. **自定义移动**：提供 `ExecuteCustomMove()` 方法供Controller调用

#### 简化的结构

**移除的内容：**
- ❌ `attackConfig` 字段（不再需要）
- ❌ `_attackCoroutine` 字段
- ❌ `_currentAttackIndex` 字段
- ❌ `beatDrivenMode` 字段
- ❌ `AttackLoopCoroutine()` 方法
- ❌ `ExecuteAttackCoroutine()` 方法
- ❌ `ExecuteMoveCoroutine()` 方法
- ❌ `TriggerBeatAttack()` 方法
- ❌ `StopAttackLoop()` 方法

**保留的内容：**
- ✅ 健康管理（`TakeDamage()`, `Heal()`, `ResetHealth()`）
- ✅ 阶段系统（`CheckPhaseTransition()`, `EnterPhase()`）
- ✅ 发射源管理（`InitializeEmitters()`, `GetEmitterPosition()`, `GetEmitter()`）
- ✅ 攻击执行（`ExecuteAttack()`, `ExecuteSingleEmitterAttack()`）
- ✅ 状态管理（`StartBattle()`, `SetState()`, `OnBossDefeated()`）

**新增的内容：**
- ✅ `ExecuteAttackAction(BossAttackAction)` - 执行攻击动作
- ✅ `ExecuteSingleEmitterAttackAction(BossAttackAction, EmitterType)` - 从单个发射源执行攻击
- ✅ `GetEmitter(EmitterType)` - 获取发射源组件
- ✅ `ExecuteCustomMove(EmitterType, int)` - 执行自定义移动

#### 核心代码

**执行攻击动作：**

```csharp
public void ExecuteAttackAction(BossAttackAction attackAction)
{
    if (_currentState != BossState.Fighting || attackAction == null)
    {
        return;
    }

    // 支持多发射源同时发射
    if (attackAction.useMultipleEmitters &&
        attackAction.multipleEmitters != null &&
        attackAction.multipleEmitters.Length > 0)
    {
        foreach (EmitterType emitterType in attackAction.multipleEmitters)
        {
            ExecuteSingleEmitterAttackAction(attackAction, emitterType);
        }
    }
    else
    {
        ExecuteSingleEmitterAttackAction(attackAction, attackAction.emitterType);
    }
}
```

**获取发射源：**

```csharp
public virtual EmitterPoint GetEmitter(EmitterType emitterType)
{
    if (_emitters != null && _emitters.TryGetValue(emitterType, out EmitterPoint emitter))
    {
        return emitter;
    }
    return null;
}
```

**自定义移动：**

```csharp
public virtual void ExecuteCustomMove(EmitterType emitterType, int customMoveId)
{
    Debug.LogWarning($"自定义移动 {customMoveId} for {emitterType} 未实现");
}
```

---

## 发射源系统

### EmitterType（发射源类型）

`EmitterType` 枚举定义了Boss身上不同的弹幕发射点。

**文件位置：** [EmitterType.cs](Assets/Scripts/Enemy/EmitterType.cs)

```csharp
public enum EmitterType
{
    MainCore,       // 核心/身体中心
    LeftHand,       // 左手
    RightHand,      // 右手
    LeftWing,       // 左翼
    RightWing,      // 右翼
    Head,           // 头部
    Tail,           // 尾部
    Custom1,        // 自定义发射点1
    Custom2,        // 自定义发射点2
    Custom3,        // 自定义发射点3
    Custom4,        // 自定义发射点4
    Custom5         // 自定义发射点5
}
```

### EmitterPoint（发射源标记组件）

`EmitterPoint` 是一个标记组件，挂载在Boss的子物体上，标识该位置为弹幕发射点。

**文件位置：** [EmitterPoint.cs](Assets/Scripts/Enemy/EmitterPoint.cs)

#### 核心功能

1. **标记发射源位置**：标识Boss身上的发射点
2. **提供实时位置**：通过 `Position` 属性获取当前位置
3. **Scene视图可视化**：在Scene视图中显示发射源位置

#### 核心代码

```csharp
public class EmitterPoint : MonoBehaviour
{
    [SerializeField] private EmitterType emitterType = EmitterType.MainCore;
    [SerializeField] private string emitterName = "Emitter";
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = Color.red;
    [SerializeField] private float gizmoSize = 0.3f;

    public EmitterType EmitterType => emitterType;
    public string EmitterName => emitterName;
    public Vector2 Position => transform.position;  // 实时位置
}
```

#### 使用方式

1. 在Boss的子物体上添加 `EmitterPoint` 组件
2. 设置 `emitterType` 为对应的类型
3. Boss会在 `InitializeEmitters()` 中自动注册所有发射源
4. 发射源可以被 `BossSequenceController` 独立移动

---

## 阶段系统

### 阶段转换机制

Boss的阶段系统基于血量百分比自动触发，每个阶段可以启用不同的序列控制器组合。

#### 阶段配置

在BossBase中配置阶段阈值：

```csharp
[Header("阶段设置")]
[SerializeField] protected List<float> phaseHealthThresholds = new List<float> { 0.7f, 0.3f };
```

**阈值说明：**
- 阈值列表应该是**降序**排列
- 血量百分比 ≤ 0.7 时进入阶段1
- 血量百分比 ≤ 0.3 时进入阶段2
- 阶段0是初始阶段（血量 > 0.7）

#### 阶段检测代码

```csharp
protected virtual void CheckPhaseTransition(float previousHealth, float currentHealth)
{
    float currentHealthPercent = currentHealth / maxHealth;

    int targetPhase = 0;
    for (int i = 0; i < phaseHealthThresholds.Count; i++)
    {
        if (currentHealthPercent <= phaseHealthThresholds[i])
        {
            targetPhase = i + 1;
        }
        else
        {
            break;
        }
    }

    if (targetPhase > _currentPhase)
    {
        EnterPhase(targetPhase);
    }
}
```

### ExampleBoss中的阶段管理

#### 配置方式

```csharp
[Header("阶段序列控制器配置")]
[SerializeField] private List<BossSequenceController> phase0Controllers;
[SerializeField] private List<BossSequenceController> phase1Controllers;
[SerializeField] private List<BossSequenceController> phase2Controllers;
```

#### 阶段切换实现

```csharp
protected override void OnPhaseEnter(int phase)
{
    DisableAllControllers();

    switch (phase)
    {
        case 0:
            EnableControllers(phase0Controllers);
            break;
        case 1:
            EnableControllers(phase1Controllers);
            break;
        case 2:
            EnableControllers(phase2Controllers);
            break;
    }
}
```

---

## 攻击类型详解

### 支持的攻击类型

```csharp
public enum BossAttackType
{
    Circle,         // 圆形弹幕
    Fan,            // 扇形弹幕
    Single,         // 单发子弹
    Aiming,         // 自机狙（瞄准玩家）
    Custom          // 自定义攻击
}
```

### 1. Circle（圆形弹幕）

从发射源向四周发射均匀分布的子弹。

**配置参数：**
- `circleCount`：圆形弹幕的子弹数量
- `circleStartAngle`：起始角度（度，0=右，90=上）

### 2. Fan（扇形弹幕）

向指定方向发射扇形分布的子弹。

**配置参数：**
- `fanCount`：扇形弹幕的子弹数量
- `fanSpreadAngle`：扩散角度（度）
- `fanCenterAngle`：中心方向（0=右，90=上，180=左，270=下）

### 3. Single（单发子弹）

向指定方向发射单个子弹。

**配置参数：**
- `singleDirection`：发射方向角度（0=右，90=上，180=左，270=下）

### 4. Aiming（自机狙）

瞄准玩家位置发射子弹，支持预判玩家移动。

**配置参数：**
- `aimingBulletCount`：子弹数量
- `aimingSpreadAngle`：扩散角度（0=精确瞄准）
- `aimingPredictMovement`：是否预判玩家移动

---

## 配置和使用

### 基础配置流程

#### 1. 创建BossSequenceConfig

在Unity编辑器中：
1. 右键 → Create → DodgeDots → Boss Sequence Config
2. 配置攻击序列（BossAttackAction数组）
3. 配置移动序列（EmitterMoveData数组）
4. 设置是否循环

**注意**：BossSequenceConfig 不需要配置 BeatMap，BeatMap 由 BeatMapPlayer 管理

#### 2. 设置发射源

在Boss的子物体上：
1. 创建空物体（如 "LeftHand"）
2. 添加 `EmitterPoint` 组件
3. 设置 `emitterType` 为对应类型
4. 调整位置到合适的发射点

#### 3. 配置Boss

在Boss GameObject上：
1. 添加Boss脚本（继承自BossBase）
2. 设置 `maxHealth`、`bossName` 等基础属性
3. 配置 `phaseHealthThresholds`（如 [0.7f, 0.3f]）
4. 添加多个 `BossSequenceController` 组件
5. 为每个Controller分配 `BossSequenceConfig`

#### 4. 配置阶段控制器

在Boss脚本中：
1. 创建不同阶段的Controller列表
2. 在 `OnPhaseEnter()` 中启用/禁用对应的Controller
3. 切换阶段时重置序列

### 配置示例

#### 示例1：简单的圆形弹幕Boss

**BossSequenceConfig配置：**
```
configName: "Simple Circle Pattern"
attackSequence:
  - attackName: "Circle 16"
    emitterType: MainCore
    attackType: Circle
    circleCount: 16
    circleStartAngle: 0
moveSequence: []  // 不移动
```

**BeatMapPlayer配置：**
```
beatMap: [BeatMap引用]
bgmManager: [BGMManager引用]
autoStart: true
```

#### 示例2：多发射源扇形攻击

**BossSequenceConfig配置：**
```
configName: "Triple Fan Pattern"
attackSequence:
  - attackName: "Triple Fan"
    useMultipleEmitters: true
    multipleEmitters: [MainCore, LeftHand, RightHand]
    attackType: Fan
    fanCount: 7
    fanSpreadAngle: 60
    fanCenterAngle: 270
moveSequence: []  // 不移动
```

#### 示例3：攻击+移动组合

**BossSequenceConfig配置：**
```
attackSequence:
  - attackName: "Aimed Shot"
    emitterType: MainCore
    attackType: Aiming
    aimingBulletCount: 3
    aimingSpreadAngle: 15

moveSequence:
  - emitterType: MainCore
    moveType: ToPosition
    targetPosition: (0, 2)
    moveDuration: 1.0
  - emitterType: MainCore
    moveType: ToPosition
    targetPosition: (2, 0)
    moveDuration: 1.0
```

---

## 最佳实践

### 系统设计原则

1. **攻击与移动分离**：
   - ✅ 攻击序列只关注攻击逻辑
   - ✅ 移动序列只关注移动逻辑
   - ✅ 两者独立配置，互不干扰

2. **多序列叠加**：
   - ✅ 使用多个Controller实现复杂行为
   - ✅ 每个Controller管理一个独立的序列
   - ✅ 通过启用/禁用Controller实现阶段切换

3. **节拍驱动**：
   - ✅ 所有攻击和移动都由BeatMap驱动
   - ✅ 确保所有BeatMap基于同一首BGM配置
   - ✅ 使用BeatMapPlayer统一管理节拍事件

### 配置建议

#### 攻击序列设计

```
✅ 推荐：
- 攻击序列长度与BeatMap节拍数匹配
- 使用有意义的attackName便于调试
- 合理使用多发射源增加弹幕密度

❌ 避免：
- 攻击序列过短导致频繁循环
- 所有攻击使用相同参数缺乏变化
- 过度使用多发射源导致性能问题
```

#### 移动序列设计

```
✅ 推荐：
- 移动序列长度可以与攻击序列不同
- 使用ToPosition实现精确位置控制
- 使用ByDirection实现相对移动

❌ 避免：
- moveDuration过短导致移动过快
- 频繁移动导致玩家难以瞄准
- 移动范围超出游戏区域
```

#### 阶段系统使用

```
✅ 推荐：
- 阈值设置合理（如 [0.7f, 0.3f]）
- 每个阶段使用不同的Controller组合
- 阶段切换时重置序列索引

❌ 避免：
- 阈值设置过于接近（如 [0.7f, 0.69f]）
- 阶段过多导致每个阶段时间过短
- 阶段转换时没有视觉反馈
```

### 性能优化

1. **子弹池化**：确保BulletManager使用对象池
2. **控制器数量**：建议每个阶段不超过3-4个Controller
3. **发射源数量**：建议不超过5个发射源
4. **攻击频率**：控制每秒发射的子弹总数

### 调试技巧

1. **使用调试日志**：在BossSequenceController中启用 `showDebugLog`
2. **Scene视图可视化**：EmitterPoint的Gizmo可以显示发射源位置
3. **分阶段测试**：先测试单个Controller，再测试多个Controller组合
4. **BeatMap对齐**：确保BeatMap的节拍时间与BGM对齐

---

## 总结

### 新系统的优势

1. **简化**：移除了复杂的自动攻击循环和多层移动系统
2. **统一**：所有攻击和移动都由节拍驱动，逻辑清晰
3. **灵活**：支持多个序列控制器叠加，实现复杂Boss行为
4. **易用**：配置驱动，无需编写代码即可设计Boss

### 与旧系统的对比

| 特性 | 旧系统 | 新系统 |
|------|--------|--------|
| 攻击驱动 | 自动循环 + 节拍驱动 | 统一节拍驱动 |
| 移动系统 | 攻击内嵌 + 时间轴 | 节拍驱动移动序列 |
| 配置复杂度 | 高（多个控制器类型） | 低（单一控制器类型） |
| 多序列支持 | 有限 | 完全支持 |
| 阶段切换 | 切换Config | 启用/禁用Controller |

### 快速参考

**核心类：**
- `BossBase` - Boss基类
- `BossSequenceConfig` - 序列配置
- `BossSequenceController` - 序列控制器
- `EmitterPoint` - 发射源标记

**核心方法：**
- `ExecuteAttackAction()` - 执行攻击
- `GetEmitter()` - 获取发射源
- `ExecuteCustomMove()` - 自定义移动
- `ResetSequence()` - 重置序列

---

**文档版本：** 2.0（重构版）
**最后更新：** 2026-02-07
**适用版本：** DodgeDots Boss System v2.x

