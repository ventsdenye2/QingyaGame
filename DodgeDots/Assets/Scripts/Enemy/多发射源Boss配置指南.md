# 多发射源Boss配置指南

## 概述

这个系统采用"指挥家-乐手"模式，让Boss可以从多个发射源（左手、右手、翅膀等）发射复杂的弹幕。

## 核心优势

✅ **统一配置**：整个Boss只需要一份BossAttackConfig，易于管理和调试
✅ **音乐同步**：所有发射源共享同一个时间轴，便于与音乐节拍对齐
✅ **灵活组合**：可以单发射源、多发射源同时发射，或者序列发射
✅ **可视化编辑**：在Unity Inspector中直观配置所有攻击序列

---

## 第一步：在场景中设置发射源

### 1. 创建Boss层级结构

```
▼ Boss (挂载 BossBase 或其子类)
   ▼ Visuals (模型/精灵)
   ▼ Emitters (空物体容器)
      ▶ LeftHand_Point (空物体，挂载 EmitterPoint)
      ▶ RightHand_Point (空物体，挂载 EmitterPoint)
      ▶ Core_Point (空物体，挂载 EmitterPoint)
      ▶ LeftWing_Point (空物体，挂载 EmitterPoint)
      ▶ RightWing_Point (空物体，挂载 EmitterPoint)
```

### 2. 配置EmitterPoint组件

在每个发射点上：
1. 添加 `EmitterPoint` 组件
2. 设置 `Emitter Type`（例如：LeftHand, RightHand, MainCore）
3. 调整位置到合适的发射点
4. 可以在Scene视图中看到红色的Gizmo标记

**提示**：如果没有配置MainCore发射源，系统会自动使用Boss自身位置作为默认发射源。

---

## 第二步：创建攻击配置

### 1. 创建BossAttackConfig资源

右键 → Create → DodgeDots → Boss Attack Config

### 2. 配置单个攻击（BossAttackData）

每个攻击可以配置：

#### 基础设置
- **Attack Name**：攻击名称（用于调试）
- **Delay Before Attack**：攻击前延迟时间

#### 发射源设置（新增）
- **Emitter Type**：从哪个发射源发射（MainCore, LeftHand, RightHand等）
- **Use Multiple Emitters**：是否同时从多个发射源发射
- **Multiple Emitters**：多发射源列表（勾选上面选项后生效）

#### 攻击类型
- **Attack Type**：Circle（圆形）、Fan（扇形）、Single（单发）、Custom（自定义）
- **Bullet Config**：使用的子弹配置

#### 弹幕参数
根据攻击类型配置相应的参数（子弹数量、角度、速度等）

---

## 第三步：实战案例

### 案例1：左右手交替发射

```
攻击序列：
1. 攻击1：
   - Emitter Type: LeftHand
   - Attack Type: Circle
   - Circle Count: 12
   - Delay Before Attack: 0.5s

2. 攻击2：
   - Emitter Type: RightHand
   - Attack Type: Circle
   - Circle Count: 12
   - Delay Before Attack: 0.5s
```

### 案例2：双手同时发射扇形弹幕

```
攻击1：
- Use Multiple Emitters: ✓
- Multiple Emitters: [LeftHand, RightHand]
- Attack Type: Fan
- Fan Count: 8
- Fan Spread Angle: 90°
```

### 案例3：核心+翅膀组合攻击

```
攻击序列：
1. 核心发射圆形弹幕（限制走位）
   - Emitter Type: MainCore
   - Attack Type: Circle
   - Circle Count: 36

2. 双翼发射扇形弹幕（封锁逃跑路线）
   - Use Multiple Emitters: ✓
   - Multiple Emitters: [LeftWing, RightWing]
   - Attack Type: Fan
   - Delay Before Attack: 0.3s
```

---

## 进阶技巧

### 1. 东方永夜抄风格的复杂弹幕

对于特别复杂的弹幕（螺旋、花型、不规则图形），建议：
- 使用 `Attack Type: Custom`
- 在Boss子类中重写 `OnCustomAttack` 方法
- 在方法中实现自定义的弹幕生成逻辑

### 2. 音乐节拍同步（待实现）

如果需要与音乐节拍同步，可以扩展 `BossAttackData`：
- 添加 `bool syncToMusic` 字段
- 添加 `float triggerBeat` 字段（在第几拍触发）
- 在 `BossBase.AttackLoopCoroutine` 中根据音乐时间触发

### 3. 阶段切换时改变攻击模式

在Boss子类中重写 `OnPhaseEnter` 方法：
```csharp
protected override void OnPhaseEnter(int phase)
{
    base.OnPhaseEnter(phase);

    // 根据阶段切换攻击配置
    if (phase == 1)
    {
        attackConfig = phase1AttackConfig;
    }
    else if (phase == 2)
    {
        attackConfig = phase2AttackConfig;
    }

    // 重启攻击循环
    StopAttackLoop();
    _currentAttackIndex = 0;
    _attackCoroutine = StartCoroutine(AttackLoopCoroutine());
}
```

---

## 调试技巧

1. **查看发射源注册**：运行游戏时，Console会输出所有注册的发射源
2. **Gizmo可视化**：在Scene视图中可以看到所有发射源的位置
3. **攻击名称**：给每个攻击起个有意义的名字，方便在Console中追踪

---

## 常见问题

**Q: 为什么弹幕从Boss中心发射，而不是从指定的发射源？**
A: 检查是否正确配置了EmitterPoint组件，并且EmitterType与BossAttackData中的设置一致。

**Q: 如何实现"左手发射后0.5秒右手发射"的效果？**
A: 创建两个攻击，第二个攻击的 `Delay Before Attack` 设置为0.5秒。

**Q: 多发射源同时发射时，弹幕会重叠吗？**
A: 不会，每个发射源会从各自的位置发射弹幕。

**Q: 如何实现旋转发射（每次发射角度递增）？**
A: 使用Custom攻击类型，在OnCustomAttack中实现自定义逻辑，或者在攻击序列中配置多个攻击，每个攻击的circleStartAngle递增。

---

## 总结

**推荐方案**：给整个Boss配置一份Config（BossAttackConfig），而不是每个发射源单独配置。

这样做的好处：
- 📋 所见即所得：一个配置文件看到完整的攻击流程
- 🎵 音乐同步：统一的时间轴，便于对齐节拍
- 🐛 易于调试：只需要修改一个文件
- 🔄 便于复用：可以为不同Boss创建不同的攻击配置

祝你的弹幕音游开发顺利！🎮
