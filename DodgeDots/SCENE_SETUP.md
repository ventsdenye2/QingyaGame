# Boss战场景设置指南

本文档说明如何在Unity中快速搭建一个Boss战场景来验证弹幕游戏玩法。

## 场景结构

```
BossBattleScene
├── GameManager (空GameObject)
│   ├── LevelManager
│   └── BulletManager
├── Player (Sprite/Circle)
│   ├── PlayerController
│   ├── PlayerHealth
│   ├── PlayerWeapon
│   ├── Rigidbody2D
│   └── CircleCollider2D
├── Boss (Sprite/Square)
│   ├── ExampleBoss
│   ├── Rigidbody2D
│   └── BoxCollider2D
├── BattleLevel (空GameObject)
│   └── BossBattleLevel
└── Main Camera
```

## 详细设置步骤

### 1. 创建GameManager

1. 创建空GameObject，命名为"GameManager"
2. 添加 `LevelManager` 组件
3. 添加 `BulletManager` 组件
4. 在BulletManager中设置弹幕预制体（见下文）

### 2. 创建玩家

1. 创建2D Sprite对象，命名为"Player"
2. 设置Sprite为Circle（或自定义精灵）
3. 设置Position为 (0, -3, 0)
4. 添加以下组件：
   - `Rigidbody2D`:
     - Body Type: Dynamic
     - Gravity Scale: 0
     - Constraints: Freeze Rotation Z
   - `CircleCollider2D`:
     - Is Trigger: false
     - Radius: 0.5
   - `PlayerController`
   - `PlayerHealth`
   - `PlayerWeapon`

### 3. 创建Boss

1. 创建2D Sprite对象，命名为"Boss"
2. 设置Sprite为Square（或自定义精灵）
3. 设置Position为 (0, 3, 0)
4. 添加以下组件：
   - `Rigidbody2D`:
     - Body Type: Kinematic
     - Gravity Scale: 0
   - `BoxCollider2D`:
     - Is Trigger: false
     - Size: (1, 1)
   - `ExampleBoss`

### 4. 创建弹幕预制体

1. 创建2D Sprite对象，命名为"Bullet"
2. 设置Sprite为Circle（小圆点）
3. 设置Scale为 (0.2, 0.2, 1)
4. 添加以下组件：
   - `Rigidbody2D`:
     - Body Type: Dynamic
     - Gravity Scale: 0
   - `CircleCollider2D`:
     - Is Trigger: true
     - Radius: 0.5
   - `Bullet`
5. 将Bullet拖到Project窗口创建预制体
6. 删除场景中的Bullet对象

### 5. 创建BattleLevel管理器

1. 创建空GameObject，命名为"BattleLevel"
2. 添加 `BossBattleLevel` 组件
3. 在Inspector中设置引用：
   - Boss: 拖入Boss对象
   - Player: 拖入Player对象
   - Player Health: 拖入Player对象
   - Player Weapon: 拖入Player对象

### 6. 配置引用关系

#### LevelManager
- Player Controller: 拖入Player对象

#### BulletManager
- Bullet Prefab: 拖入弹幕预制体

#### PlayerWeapon
- Fire Point: 可以创建子对象作为发射点，或留空使用自身位置

### 7. 创建GameConfig资源

1. 在Project窗口右键 -> Create -> DodgeDots -> Game Config
2. 命名为"GameConfig"
3. 放在 Assets/Resources 文件夹下
4. 配置参数：
   - Player Move Speed: 5
   - Player Auto Shoot Interval: 0.2
   - Player Max Health: 100
   - Boss Battle Bounds: (10, 10)
   - Bullet Default Speed: 3
   - Bullet Pool Initial Size: 100

### 8. 设置摄像机

1. 选择Main Camera
2. 设置Position为 (0, 0, -10)
3. 设置Projection为Orthographic
4. 设置Size为6（根据需要调整）

## 测试场景

1. 按Play运行场景
2. 使用WASD或鼠标控制玩家移动
3. 玩家会自动向Boss射击
4. Boss会发射圆形弹幕
5. 躲避弹幕并击败Boss

## 控制说明

- **移动**: WASD键或鼠标（可在PlayerController中切换）
- **技能**: 空格键或鼠标左键（当前为占位实现）

## 扩展建议

1. **添加UI**: 显示玩家生命值、Boss生命值、技能冷却等
2. **添加特效**: 受伤特效、死亡特效、弹幕特效等
3. **添加音效**: 射击音效、受伤音效、背景音乐等
4. **创建新Boss**: 继承BossBase类，实现自定义攻击模式
5. **创建大世界场景**: 用于连接不同的Boss战关卡
