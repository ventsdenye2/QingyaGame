# 子弹系统使用指南

## 概述

新的子弹系统采用**组合（Composition）**架构，支持高度灵活的子弹配置和行为扩展。

### 核心组件

1. **BulletConfig** - ScriptableObject配置文件，定义子弹属性
2. **Bullet** - 基础子弹类，支持配置和行为系统
3. **IBulletBehavior** - 行为接口，用于实现特殊机制
4. **BulletManager** - 管理器，支持多种子弹类型和对象池

---

## 快速开始

### 1. 创建子弹配置

在Unity中：
1. 右键点击 Project 窗口
2. 选择 **Create → DodgeDots → Bullet Config**
3. 命名配置文件（例如：`NormalBulletConfig`）

### 2. 配置子弹属性

选中创建的配置文件，在Inspector中设置：

```
基础属性：
- Bullet Name: 普通子弹
- Sprite: 选择子弹精灵图
- Color: 白色
- Scale: (1, 1)

战斗属性：
- Default Speed: 5
- Default Damage: 10
- Lifetime: 10

碰撞属性：
- Collider Radius: 0.1
- Is Piercing: false

视觉效果：
- Rotation Speed: 0
- Face Direction: true
- Sorting Layer: Default
- Sorting Order: 0
```

### 3. 在BulletManager中注册

选中场景中的BulletManager对象：
1. 在Inspector中找到 **Bullet Types** 数组
2. 增加数组大小
3. 设置：
   - Config: 选择刚创建的BulletConfig
   - Prefab: 选择Bullet预制体

---

## 使用示例

### 示例1：发射普通子弹

```csharp
using DodgeDots.Bullet;
using DodgeDots.Core;

public class Example : MonoBehaviour
{
    [SerializeField] private BulletConfig normalBulletConfig;

    void ShootBullet()
    {
        Vector2 position = transform.position;
        Vector2 direction = Vector2.up;
        Team team = Team.Enemy;

        // 使用配置发射子弹
        BulletManager.Instance.SpawnBullet(position, direction, team, normalBulletConfig);
    }
}
```

### 示例2：发射圆形弹幕

```csharp
void ShootCirclePattern()
{
    Vector2 position = transform.position;
    int bulletCount = 12; // 12颗子弹
    Team team = Team.Enemy;

    // 使用配置发射圆形弹幕
    BulletManager.Instance.SpawnCirclePattern(
        position,
        bulletCount,
        team,
        normalBulletConfig
    );
}
```

### 示例3：发射扇形弹幕

```csharp
void ShootFanPattern()
{
    Vector2 position = transform.position;
    Vector2 direction = (player.position - position).normalized;
    int bulletCount = 5;
    float spreadAngle = 60f; // 60度扇形
    Team team = Team.Enemy;

    BulletManager.Instance.SpawnFanPattern(
        position,
        direction,
        bulletCount,
        spreadAngle,
        team,
        normalBulletConfig
    );
}
```

---

## 创建特殊子弹

### 反弹子弹

1. **创建反弹子弹配置**
   - 右键创建新的BulletConfig
   - 命名为 `BounceBulletConfig`
   - 设置属性（可以使用不同的sprite和颜色）

2. **创建反弹子弹预制体**
   - 复制现有的Bullet预制体
   - 重命名为 `BounceBullet`
   - 添加 **BulletBounceBehavior** 组件
   - 配置反弹参数：
     - Max Bounces: 3（最多反弹3次）
     - Speed Decay: 0.9（每次反弹速度衰减10%）
     - Change Color On Bounce: true

3. **在BulletManager中注册**
   - 添加到Bullet Types数组
   - Config: BounceBulletConfig
   - Prefab: BounceBullet

4. **使用反弹子弹**
```csharp
[SerializeField] private BulletConfig bounceBulletConfig;

void ShootBounceBullet()
{
    BulletManager.Instance.SpawnBullet(
        transform.position,
        Vector2.up,
        Team.Enemy,
        bounceBulletConfig
    );
}
```

### 穿透子弹

在BulletConfig中设置：
- Is Piercing: true
- Max Pierce Count: 3（穿透3个目标后消失，0表示无限穿透）

---

## 下一步

查看 [CUSTOM_BULLET_BEHAVIOR.md](CUSTOM_BULLET_BEHAVIOR.md) 了解如何创建自定义子弹行为。
