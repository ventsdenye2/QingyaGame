# Boss攻击配置示例

本文档展示如何配置各种类型的Boss攻击模式。

## 创建配置文件

1. 在Project窗口右键
2. Create → DodgeDots → Boss Attack Config
3. 命名为 "ExampleBossAttackConfig"

---

## 示例1：基础圆形弹幕

**适用场景**：第一阶段，简单的圆形弹幕

### 配置参数

```
Config Name: "第一阶段攻击"
Loop Sequence: ✓
Delay After Loop: 2

Attack Sequence (1个攻击):
├── [0] 基础圆形
    ├── Attack Name: "圆形弹幕"
    ├── Delay Before Attack: 0.5
    ├── Emitter Type: MainCore
    ├── Use Multiple Emitters: ✗
    ├── Use Combo Attack: ✗
    ├── Attack Type: Circle
    ├── Bullet Config: (选择你的弹幕配置)
    ├── Circle Count: 12
    └── Circle Start Angle: 0
```

**效果**：Boss每2.5秒从核心发射一圈12发的圆形弹幕。

---

## 示例2：左右手交替攻击

**适用场景**：展示多发射源的基础用法

### 配置参数

```
Config Name: "左右手交替"
Loop Sequence: ✓
Delay After Loop: 1

Attack Sequence (2个攻击):
├── [0] 左手攻击
│   ├── Attack Name: "左手圆形"
│   ├── Delay Before Attack: 0.5
│   ├── Emitter Type: LeftHand
│   ├── Attack Type: Circle
│   ├── Circle Count: 8
│   └── Circle Start Angle: 0
│
└── [1] 右手攻击
    ├── Attack Name: "右手圆形"
    ├── Delay Before Attack: 0.5
    ├── Emitter Type: RightHand
    ├── Attack Type: Circle
    ├── Circle Count: 8
    └── Circle Start Angle: 45
```

**效果**：左手发射→0.5秒后→右手发射→1秒后→循环。

---

## 示例3：组合攻击（扇形+单发）

**适用场景**：你提到的需求 - 同时向下发射扇形，向左发射单发

### 配置参数

```
Config Name: "组合攻击示例"
Loop Sequence: ✓
Delay After Loop: 2

Attack Sequence (1个攻击):
├── [0] 扇形+单发组合
    ├── Attack Name: "下扇+左单"
    ├── Delay Before Attack: 1
    ├── Emitter Type: MainCore
    ├── Use Multiple Emitters: ✗
    ├── Use Combo Attack: ✓
    └── Sub Attacks (2个):
        ├── [0] 向下扇形
        │   ├── Attack Type: Fan
        │   ├── Bullet Config: (红色弹幕)
        │   ├── Fan Count: 8
        │   ├── Fan Spread Angle: 60
        │   └── Fan Center Angle: 270 (向下)
        │
        └── [1] 向左单发
            ├── Attack Type: Single
            ├── Bullet Config: (蓝色弹幕)
            └── Single Direction: 180 (向左)
```

**效果**：每3秒，Boss同时发射向下的扇形弹幕和向左的单发弹幕。

---

## 示例4：双手同时发射组合攻击

**适用场景**：高难度攻击，左右手同时发射复杂弹幕

### 配置参数

```
Config Name: "双手组合攻击"
Loop Sequence: ✓
Delay After Loop: 3

Attack Sequence (1个攻击):
├── [0] 双手组合
    ├── Attack Name: "左右手组合弹幕"
    ├── Delay Before Attack: 1
    ├── Use Multiple Emitters: ✓
    ├── Multiple Emitters: [LeftHand, RightHand]
    ├── Use Combo Attack: ✓
    └── Sub Attacks (2个):
        ├── [0] 圆形弹幕
        │   ├── Attack Type: Circle
        │   ├── Bullet Config: (红色弹幕)
        │   ├── Circle Count: 12
        │   └── Circle Start Angle: 0
        │
        └── [1] 扇形弹幕
            ├── Attack Type: Fan
            ├── Bullet Config: (蓝色弹幕)
            ├── Fan Count: 5
            ├── Fan Spread Angle: 90
            └── Fan Center Angle: 270
```

**效果**：左手和右手同时发射圆形+扇形的组合弹幕，形成复杂的弹幕网。

---

## 示例5：完整的三阶段Boss配置

**适用场景**：完整的Boss战，展示阶段切换

### 第一阶段配置（Phase0_AttackConfig）

```
Config Name: "第一阶段-简单模式"
Loop Sequence: ✓
Delay After Loop: 2

Attack Sequence (2个攻击):
├── [0] 圆形弹幕
│   ├── Emitter Type: MainCore
│   ├── Attack Type: Circle
│   ├── Circle Count: 12
│   └── Delay Before Attack: 1
│
└── [1] 左右手交替
    ├── Use Multiple Emitters: ✓
    ├── Multiple Emitters: [LeftHand, RightHand]
    ├── Attack Type: Fan
    ├── Fan Count: 5
    └── Delay Before Attack: 1
```

### 第二阶段配置（Phase1_AttackConfig）

```
Config Name: "第二阶段-中等难度"
Loop Sequence: ✓
Delay After Loop: 1.5

Attack Sequence (3个攻击):
├── [0] 快速圆形
│   ├── Emitter Type: MainCore
│   ├── Attack Type: Circle
│   ├── Circle Count: 16
│   └── Delay Before Attack: 0.5
│
├── [1] 左手组合
│   ├── Emitter Type: LeftHand
│   ├── Use Combo Attack: ✓
│   └── Sub Attacks: [Circle(8发), Fan(5发)]
│
└── [2] 右手组合
    ├── Emitter Type: RightHand
    ├── Use Combo Attack: ✓
    └── Sub Attacks: [Circle(8发), Fan(5发)]
```

### 第三阶段配置（Phase2_AttackConfig）

```
Config Name: "第三阶段-高难度"
Loop Sequence: ✓
Delay After Loop: 1

Attack Sequence (2个攻击):
├── [0] 全方位组合攻击
│   ├── Use Multiple Emitters: ✓
│   ├── Multiple Emitters: [MainCore, LeftHand, RightHand]
│   ├── Use Combo Attack: ✓
│   ├── Sub Attacks (3个):
│   │   ├── [0] Circle (20发)
│   │   ├── [1] Fan (8发, 向下)
│   │   └── [2] Single (向左)
│   └── Delay Before Attack: 0.5
│
└── [1] 快速连射
    ├── Emitter Type: MainCore
    ├── Attack Type: Circle
    ├── Circle Count: 24
    ├── Circle Start Angle: 15 (旋转效果)
    └── Delay Before Attack: 0.5
```

### ExampleBoss配置

在ExampleBoss组件的Inspector中：

```
Max Health: 1000
Boss Name: "示例Boss"
Phase Health Thresholds: [0.7, 0.4]
├── Attack Config: Phase0_AttackConfig
├── Phase1 Attack Config: Phase1_AttackConfig
└── Phase2 Attack Config: Phase2_AttackConfig
```

**效果**：
- 血量100%-70%：使用第一阶段配置（简单）
- 血量70%-40%：自动切换到第二阶段配置（中等）
- 血量40%-0%：自动切换到第三阶段配置（困难）

---

## 配置技巧

### 1. 弹幕密度控制

- **低密度**：Circle Count: 8-12, Fan Count: 3-5
- **中密度**：Circle Count: 16-20, Fan Count: 5-8
- **高密度**：Circle Count: 24-36, Fan Count: 8-12

### 2. 角度参考

```
      90° (上)
       |
180° ←-+→ 0° (右)
       |
     270° (下)
```

### 3. 组合攻击设计原则

- **限制走位 + 精准打击**：圆形弹幕限制走位，单发弹幕瞄准玩家
- **多方向封锁**：扇形弹幕覆盖多个方向，减少安全区
- **速度差异**：使用不同BulletConfig，创造快慢弹幕组合

### 4. 延迟时间建议

- **Delay Before Attack**：0.5-2秒（给玩家反应时间）
- **Delay After Loop**：1-3秒（循环间隔）
- 阶段越高，延迟越短，增加难度

---

## 常见问题

### Q1: 组合攻击中的子弹会重叠吗？
A: 不会。每个子攻击独立发射，可以使用不同的BulletConfig来区分颜色和速度。

### Q2: 如何实现旋转弹幕效果？
A: 在攻击序列中创建多个攻击，每个攻击的 `Circle Start Angle` 递增（如0°, 15°, 30°...）。

### Q3: 多发射源 + 组合攻击会很卡吗？
A: 取决于弹幕总数。建议单次攻击总弹幕数不超过100发，使用对象池优化性能。

### Q4: 如何让Boss移动的同时发射弹幕？
A: 在BossAttackData中配置 `Move Type` 和移动参数，系统会同时执行移动和攻击。

---

## 总结

**核心优势**：
- ✅ 配置驱动：无需写代码，在Inspector中可视化配置
- ✅ 灵活组合：单一攻击、多发射源、组合攻击任意组合
- ✅ 阶段切换：根据血量自动切换攻击配置
- ✅ 易于调试：每个攻击有名称，Console输出清晰

**推荐工作流**：
1. 先创建简单的单一攻击测试
2. 逐步添加多发射源
3. 最后设计组合攻击
4. 为不同阶段创建不同配置文件

祝你的弹幕音游开发顺利！🎮
