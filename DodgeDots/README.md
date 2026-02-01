# QingyaGame
这是目指米哈游小组参与himoyo青芽gamejam的项目

## 项目简介

DodgeDots 是一款2D俯视角弹幕躲避游戏，核心玩法是躲避弹幕并击败Boss。

### 核心特性

- **双关卡类型**: Boss战（固定边界）和大世界（无边界限制）
- **双控制模式**: 支持键盘（WASD）和鼠标控制，可随时切换
- **混合攻击**: 自动射击 + 手动技能释放
- **多阶段Boss**: Boss根据生命值进入不同战斗阶段
- **高性能**: 使用对象池管理弹幕，支持大量弹幕同屏
- **可扩展架构**: 预留接口便于后续功能扩展

## 快速开始

1. 打开Unity项目：`QingyaGame/DodgeDots`
2. 查看场景设置指南：[SCENE_SETUP.md](DodgeDots/SCENE_SETUP.md)
3. 按照指南搭建Boss战场景
4. 运行场景开始测试

## 项目结构

```
DodgeDots/Assets/Scripts/
├── Core/                    # 核心基础类
│   ├── IHealth.cs          # 生命值接口
│   ├── IDamageable.cs      # 可受伤接口
│   ├── GameConfig.cs       # 游戏配置（ScriptableObject）
│   └── ObjectPool.cs       # 通用对象池
├── Player/                  # 玩家系统
│   ├── PlayerController.cs # 玩家控制器（支持键盘/鼠标）
│   ├── PlayerHealth.cs     # 玩家生命值管理
│   └── PlayerWeapon.cs     # 玩家武器系统
├── Bullet/                  # 弹幕系统
│   ├── Bullet.cs           # 基础弹幕类
│   └── BulletManager.cs    # 弹幕管理器（对象池+弹幕模式）
├── Enemy/                   # 敌人/Boss系统
│   ├── BossBase.cs         # Boss基类（生命值+阶段系统）
│   ├── BossAttackPattern.cs # Boss攻击模式基类
│   └── ExampleBoss.cs      # 示例Boss实现
├── Level/                   # 关卡管理
│   ├── LevelType.cs        # 关卡类型枚举
│   ├── LevelManager.cs     # 关卡管理器
│   └── BossBattleLevel.cs  # Boss战关卡管理
└── Managers/                # 全局管理器（预留）
```

## 核心系统架构

### 1. 玩家系统 (Player/)

**PlayerController** - 玩家控制器
- 支持两种控制模式：键盘（WASD）和鼠标
- 可通过 `SetControlMode()` 切换控制模式
- 支持边界限制（Boss战）和无边界（大世界）
- 接口：`SetBoundsRestriction()`, `SetBounds()`

**PlayerHealth** - 生命值管理
- 实现 `IHealth` 和 `IDamageable` 接口
- 受伤后短暂无敌时间
- 事件：`OnHealthChanged`, `OnDeath`, `OnDamageTaken`

**PlayerWeapon** - 武器系统
- 自动射击：持续向目标发射
- 手动技能：空格键或鼠标左键触发
- 技能冷却系统
- 事件：`OnAutoShoot`, `OnSkillUsed`

### 2. 弹幕系统 (Bullet/)

**Bullet** - 基础弹幕类
- 自动移动和生命周期管理
- 碰撞检测和伤害计算
- 支持对象池复用

**BulletManager** - 弹幕管理器（单例）
- 对象池管理，自动回收
- 内置弹幕模式：
  - `SpawnCirclePattern()` - 圆形弹幕
  - `SpawnFanPattern()` - 扇形弹幕
- 可扩展自定义弹幕模式

### 3. Boss系统 (Enemy/)

**BossBase** - Boss基类（抽象类）
- 实现 `IHealth` 和 `IDamageable` 接口
- 多阶段系统：根据生命值百分比自动切换阶段
- 状态机：Idle, Fighting, Defeated
- 需要实现的抽象方法：
  - `OnBattleStart()` - 战斗开始时调用
  - `OnPhaseEnter(int phase)` - 进入新阶段时调用
- 事件：`OnHealthChanged`, `OnDeath`, `OnPhaseChanged`, `OnStateChanged`

**BossAttackPattern** - 攻击模式基类（抽象类）
- 冷却时间管理
- 协程执行攻击逻辑
- 需要实现：`ExecutePattern()` 协程

**ExampleBoss** - 示例Boss实现
- 展示如何使用Boss框架
- 三个阶段，难度递增
- 不同阶段使用不同弹幕模式

### 4. 关卡系统 (Level/)

**LevelManager** - 关卡管理器（单例）
- 管理关卡类型切换（Boss战/大世界）
- 自动配置玩家边界限制
- 在Scene视图中显示边界线（Gizmos）

**BossBattleLevel** - Boss战关卡管理
- 战斗流程控制
- 胜利/失败判定
- 事件：`OnBattleStart`, `OnBattleWin`, `OnBattleLose`
- 功能：`StartBattle()`, `ResetBattle()`

## 扩展指南

### 创建新Boss

1. 创建新类继承 `BossBase`
2. 实现抽象方法：
```csharp
protected override void OnBattleStart()
{
    // 战斗开始时的初始化
}

protected override void OnPhaseEnter(int phase)
{
    // 根据阶段调整行为
    switch (phase)
    {
        case 1: // 第二阶段
            break;
        case 2: // 第三阶段
            break;
    }
}
```
3. 在Update中实现攻击逻辑
4. 使用 `BulletManager.Instance` 发射弹幕

### 创建自定义弹幕模式

在 `BulletManager` 中添加新方法：
```csharp
public void SpawnCustomPattern(Vector2 position, ...)
{
    // 自定义弹幕生成逻辑
    for (int i = 0; i < count; i++)
    {
        SpawnBullet(position, direction, speed, damage);
    }
}
```

### 创建自定义攻击模式

1. 创建新类继承 `BossAttackPattern`
2. 实现 `ExecutePattern()` 协程：
```csharp
protected override IEnumerator ExecutePattern()
{
    // 攻击逻辑
    for (int i = 0; i < waves; i++)
    {
        _bulletManager.SpawnCirclePattern(...);
        yield return new WaitForSeconds(interval);
    }

    FinishPattern(); // 完成后调用
}
```
3. 将组件添加到Boss对象
4. 在Boss的Update中调用 `attackPattern.Execute()`

### 切换关卡类型

```csharp
// 切换到Boss战模式
LevelManager.Instance.SwitchLevelType(LevelType.BossBattle);

// 切换到大世界模式
LevelManager.Instance.SwitchLevelType(LevelType.OpenWorld);
```

## 技术要点

### 对象池优化
- 所有弹幕使用对象池管理，避免频繁创建/销毁
- 初始容量和最大容量可在 `GameConfig` 中配置
- 自动回收超时弹幕

### 事件驱动架构
- 各系统通过事件解耦，便于扩展
- 主要事件：生命值变化、死亡、阶段切换、战斗状态等

### 接口设计
- `IHealth` - 统一的生命值管理接口
- `IDamageable` - 统一的伤害接收接口
- 便于后续添加新的可交互对象

### 可配置性
- 使用 `GameConfig` ScriptableObject 集中管理配置
- 支持在Inspector中实时调整参数
- 便于平衡性调整和测试

## 后续扩展方向

- [ ] UI系统：生命值条、技能冷却显示、得分系统
- [ ] 大世界场景：剧情触发点、场景切换
- [ ] 更多Boss：不同攻击模式和机制
- [ ] 玩家技能系统：多种技能选择
- [ ] 音效和特效：提升游戏体验
- [ ] 存档系统：进度保存和加载
- [ ] 难度选择：不同难度级别

## 开发团队

目指米哈游小组 - himoyo青芽gamejam

