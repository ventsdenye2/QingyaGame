# Unity项目设置指南

## 必需的标签设置

### 创建标签

1. 在Unity编辑器中，点击菜单 **Edit → Project Settings**
2. 在左侧选择 **Tags and Layers**
3. 在 **Tags** 部分，点击 **+** 按钮添加以下标签：

| 标签名称 | 用途 |
|---------|------|
| `Player` | 玩家对象 |
| `Boss` | Boss敌人 |
| `Enemy` | 普通敌人（可选，未来扩展用） |
| `Boundary` | 边界对象（用于子弹反弹） |

### 设置对象标签

#### 1. 设置玩家标签
- 在Hierarchy中选择 **Player** 对象
- 在Inspector顶部的 **Tag** 下拉菜单中选择 **Player**

#### 2. 设置Boss标签
- 在Hierarchy中选择 **Boss** 对象
- 在Inspector顶部的 **Tag** 下拉菜单中选择 **Boss**

#### 3. 设置边界标签（如果使用反弹子弹）
- 在Hierarchy中选择边界对象（如果有）
- 设置Tag为 **Boundary**

---

## 图层设置（可选）

如果需要更精细的碰撞控制，可以设置图层：

### 创建图层
在 **Project Settings → Tags and Layers** 中的 **Layers** 部分添加：
- Layer 8: `Player`
- Layer 9: `Enemy`
- Layer 10: `PlayerBullet`
- Layer 11: `EnemyBullet`

### 配置碰撞矩阵
在 **Project Settings → Physics 2D** 中配置Layer Collision Matrix：
- PlayerBullet 只与 Enemy 层碰撞
- EnemyBullet 只与 Player 层碰撞

---

## 排序层设置

为了正确显示子弹和角色，建议设置排序层：

1. 打开 **Project Settings → Tags and Layers**
2. 在 **Sorting Layers** 部分添加：

| 排序层名称 | 顺序 | 用途 |
|-----------|------|------|
| Background | 0 | 背景 |
| Default | 1 | 默认 |
| Bullet | 2 | 子弹 |
| Character | 3 | 角色 |
| UI | 4 | UI元素 |

---

## 子弹预制体设置

### 基础子弹预制体

1. **创建空对象**
   - Hierarchy右键 → Create Empty
   - 命名为 `Bullet`

2. **添加必需组件**
   - Add Component → Rigidbody 2D
     - Body Type: Dynamic
     - Gravity Scale: 0
     - Constraints: Freeze Rotation Z

   - Add Component → Circle Collider 2D
     - Is Trigger: ✓ (勾选)
     - Radius: 0.1

   - Add Component → Sprite Renderer
     - Sprite: 选择子弹精灵图
     - Color: 白色
     - Sorting Layer: Bullet
     - Order in Layer: 0

3. **添加脚本**
   - Add Component → Bullet (脚本)

4. **保存为预制体**
   - 将Bullet对象拖到Project窗口的 `Prefabs/` 文件夹
   - 删除Hierarchy中的Bullet对象

---

## BulletManager设置

1. **选中BulletManager对象**（在Hierarchy中）

2. **配置Inspector**
   - Default Bullet Prefab: 拖入Bullet预制体
   - Initial Pool Size: 100
   - Max Pool Size: 500
   - Game Config: 拖入GameConfig资源

3. **注册子弹类型**（如果有多种子弹）
   - Bullet Types数组大小设为需要的数量
   - 为每个条目设置：
     - Config: 对应的BulletConfig资源
     - Prefab: 对应的Bullet预制体

---

## 常见问题

### Q: 报错 "Tag: Enemy is not defined"
**A:** 按照上面的步骤创建Enemy标签。如果暂时不需要Enemy标签，可以只创建Player和Boss标签。

### Q: 子弹不可见
**A:** 检查：
1. Bullet预制体是否有SpriteRenderer组件
2. Sprite字段是否设置了精灵图
3. Color的Alpha是否为255（不透明）
4. Sorting Layer是否正确

### Q: 子弹没有碰撞
**A:** 检查：
1. Bullet预制体的Collider 2D的Is Trigger是否勾选
2. 目标对象（Player/Boss）是否有Collider 2D组件
3. 目标对象的Tag是否正确设置

### Q: Boss不发射子弹
**A:** 检查：
1. BulletManager是否在场景中
2. BulletManager的Bullet Prefab是否设置
3. BossBattleLevel是否启动了战斗（Auto Start Battle勾选）

---

## 下一步

完成设置后，查看：
- [BULLET_SYSTEM_GUIDE.md](BULLET_SYSTEM_GUIDE.md) - 学习如何使用子弹系统
- [CUSTOM_BULLET_BEHAVIOR.md](CUSTOM_BULLET_BEHAVIOR.md) - 学习如何创建自定义子弹行为
