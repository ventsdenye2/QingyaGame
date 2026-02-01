# Unity大世界关卡选择系统创建教程

本教程将指导你如何在Unity中创建一个类似超级马里奥的大世界关卡选择地图系统。

## 目录
1. [场景基础设置](#1-场景基础设置)
2. [Tilemap地图创建](#2-tilemap地图创建)
3. [创建关卡节点](#3-创建关卡节点)
4. [配置ScriptableObject](#4-配置scriptableobject)
5. [设置管理器](#5-设置管理器)
6. [玩家控制器配置](#6-玩家控制器配置)
7. [测试和调试](#7-测试和调试)

---

## 1. 场景基础设置

### 1.1 创建世界地图场景

1. 在Unity中创建新场景：`File > New Scene`
2. 保存场景为 `WorldMap.unity`
3. 设置场景为2D模式：
   - 在Scene视图右上角点击2D按钮
   - 或在 `Edit > Project Settings > Editor` 中设置默认行为模式为2D

### 1.2 设置相机

1. 选中Main Camera
2. 在Inspector中设置：
   - **Projection**: Orthographic
   - **Size**: 10（根据地图大小调整）
   - **Background**: 选择合适的背景颜色
   - **Culling Mask**: Everything

### 1.3 创建基础GameObject层级

在Hierarchy中创建以下空GameObject来组织场景：

```
WorldMap (Scene)
├── Main Camera
├── --- Map ---
│   ├── Grid (Tilemap容器)
│   └── LevelNodes (关卡节点容器)
├── --- Managers ---
│   ├── WorldMapManager
│   └── DialogueManager
└── --- UI ---
    ├── Canvas
    └── DialogueUI
```

创建方法：
- 右键 Hierarchy > Create Empty
- 重命名为对应名称
- 使用 `---` 作为分隔符（这些是空GameObject，用于组织）

---

## 2. Tilemap地图创建

### 2.1 创建Grid和Tilemap

1. 右键点击 `Map` GameObject
2. 选择 `2D Object > Tilemap > Rectangular`
3. 这会自动创建：
   - Grid组件（父对象）
   - Tilemap组件（子对象）

### 2.2 准备Tile资源

1. 导入地图瓦片图片到 `Assets/Sprites/Tiles` 文件夹
2. 选中图片，在Inspector中设置：
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Multiple（如果是图集）
   - **Pixels Per Unit**: 16或32（根据你的美术资源）
   - 点击 **Apply**

3. 如果是图集，点击 **Sprite Editor** 进行切片：
   - 选择 **Slice** > **Grid By Cell Size**
   - 设置合适的Cell Size
   - 点击 **Slice** 然后 **Apply**

### 2.3 创建Tile Palette

1. 打开Tile Palette窗口：`Window > 2D > Tile Palette`
2. 点击 **Create New Palette**
3. 命名为 `WorldMapPalette`
4. 保存到 `Assets/Palettes` 文件夹
5. 将切好的Sprite拖入Palette窗口，自动创建Tile资源

### 2.4 绘制地图

1. 在Tile Palette窗口中：
   - 选择画笔工具
   - 选择要绘制的Tile
   - 在Scene视图中绘制地图

2. 可以创建多个Tilemap图层：
   - Background（背景层）
   - Ground（地面层）
   - Decoration（装饰层）
   - 调整 **Order in Layer** 来控制渲染顺序

### 2.5 设置Tilemap碰撞（可选）

如果需要地图碰撞：
1. 选中Tilemap GameObject
2. 添加组件：`Tilemap Collider 2D`
3. 如果需要优化，添加：`Composite Collider 2D`
   - 勾选Tilemap Collider 2D的 **Used By Composite**
   - 在Rigidbody 2D中设置 **Body Type** 为 Static

---

## 3. 创建关卡节点

### 3.1 创建关卡节点预制体

1. 在Hierarchy中创建新的空GameObject，命名为 `LevelNode`
2. 添加必要的组件：

#### 添加视觉组件
- 添加 `Sprite Renderer` 组件（背景）：
  - 设置Sprite为圆形或方形图标
  - 设置Color为白色
  - Order in Layer: 1

- 创建子对象 `Icon`：
  - 添加 `Sprite Renderer` 组件
  - 设置Sprite为关卡图标
  - Order in Layer: 2

#### 添加交互组件
- 添加 `Circle Collider 2D` 组件：
  - 勾选 **Is Trigger**
  - 调整Radius覆盖整个节点

- 添加 `LevelNode` 脚本组件：
  - 将背景的Sprite Renderer拖到 **Background Renderer** 字段
  - 将Icon的Sprite Renderer拖到 **Icon Renderer** 字段

### 3.2 配置关卡节点

在LevelNode组件的Inspector中：

1. **Node Data**: 稍后创建并赋值（见第4节）
2. **Next Nodes**: 设置连接的下一个关卡节点（稍后配置）
3. **Icon Renderer**: 拖入Icon子对象的Sprite Renderer
4. **Background Renderer**: 拖入背景的Sprite Renderer

### 3.3 创建预制体

1. 将配置好的LevelNode拖到 `Assets/Prefabs` 文件夹
2. 创建预制体后，可以删除Hierarchy中的原始对象
3. 从预制体实例化多个关卡节点到场景中

### 3.4 放置关卡节点

1. 在 `LevelNodes` 容器下实例化关卡节点预制体
2. 在Scene视图中将节点放置到地图上合适的位置
3. 命名节点为有意义的名称，如：
   - `Level_1-1`
   - `Level_1-2`
   - `Boss_1`

### 3.5 连接关卡节点

在每个关卡节点的Inspector中：
1. 找到 **Next Nodes** 数组
2. 设置数组大小为下一个关卡的数量
3. 将下一个关卡节点拖入数组元素中

示例：
```
Level_1-1 的 Next Nodes: [Level_1-2]
Level_1-2 的 Next Nodes: [Level_1-3, Level_1-4] (分支)
Level_1-3 的 Next Nodes: [Boss_1]
```

---

## 4. 配置ScriptableObject

### 4.1 创建关卡节点数据

1. 在Project窗口中右键点击 `Assets/Configs` 文件夹
2. 选择 `Create > DodgeDots > World Map > Level Node Data`
3. 命名为对应的关卡ID，如 `Level_1-1_Data`

4. 在Inspector中配置：
   - **Level Id**: `level_1-1`（唯一标识）
   - **Level Name**: `关卡 1-1`
   - **Node Type**: Normal/Boss/Special等
   - **Scene Name**: 关卡场景名称（如 `Level1-1`）
   - **Description**: 关卡描述
   - **Prerequisite Level Ids**: 前置关卡ID数组（如果需要）
   - **Unlocked By Default**: 是否默认解锁（第一关设为true）
   - **Difficulty**: 难度星级（1-5）
   - **Node Icon**: 关卡图标Sprite
   - **Node Color**: 节点颜色

5. 为每个关卡节点创建对应的LevelNodeData

### 4.2 创建世界地图配置

1. 右键点击 `Assets/Configs` 文件夹
2. 选择 `Create > DodgeDots > World Map > World Map Config`
3. 命名为 `World1_Config`

4. 在Inspector中配置：
   - **Map Name**: `世界 1`
   - **Map Description**: 世界描述
   - **Level Nodes**: 将所有创建的LevelNodeData拖入数组
   - **Initial Unlocked Level Id**: `level_1-1`（初始解锁的关卡）
   - **Map Bounds**: 设置地图边界（用于限制相机）
   - **Camera Speed**: 相机移动速度

### 4.3 关联节点数据到场景对象

1. 在Hierarchy中选择每个关卡节点GameObject
2. 在LevelNode组件中：
   - 将对应的LevelNodeData拖到 **Node Data** 字段
3. 确保每个节点都关联了正确的数据

---

## 5. 设置管理器

### 5.1 配置WorldMapManager

1. 在Hierarchy中选择 `WorldMapManager` GameObject
2. 添加 `WorldMapManager` 脚本组件
3. 在Inspector中配置：

   - **Map Config**: 拖入创建的WorldMapConfig
   - **Level Nodes In Scene**: 设置数组大小为场景中关卡节点的数量
   - 将所有场景中的LevelNode GameObject拖入数组

4. 确保WorldMapManager是场景中唯一的实例（单例模式）

### 5.2 配置DialogueManager（如果使用对话系统）

1. 在Hierarchy中选择 `DialogueManager` GameObject
2. 添加 `DialogueManager` 脚本组件
3. 这个管理器会自动初始化，无需额外配置

### 5.3 收集场景中的关卡节点

快速方法：
1. 在Hierarchy中选择 `LevelNodes` 容器
2. 在Inspector中，WorldMapManager的 **Level Nodes In Scene** 数组
3. 锁定Inspector（点击右上角的锁图标）
4. 依次选择每个关卡节点，拖到数组中

或使用脚本自动收集（可选）：
```csharp
// 在Editor脚本中
LevelNode[] nodes = FindObjectsOfType<LevelNode>();
```

---

## 6. 玩家控制器配置

### 6.1 创建玩家对象

1. 在Hierarchy中创建新的GameObject，命名为 `Player`
2. 添加视觉组件：
   - 添加 `Sprite Renderer` 组件
   - 设置玩家的Sprite
   - Order in Layer: 10（确保在地图上方）

3. 添加 `Circle Collider 2D` 组件：
   - 调整Radius适配玩家大小
   - 勾选 **Is Trigger**（如果不需要物理碰撞）

### 6.2 添加PlayerWorldMapController组件

1. 选中Player GameObject
2. 添加 `PlayerWorldMapController` 脚本组件
3. 在Inspector中配置：

   - **Move Speed**: 5（移动速度）
   - **Use Keyboard Input**: true（使用键盘输入）
   - **World Map Camera**: 拖入Main Camera
   - **Camera Follow Player**: true（相机跟随玩家）
   - **Camera Follow Speed**: 5（相机跟随速度）
   - **Current Node**: 拖入初始关卡节点（如Level_1-1）

### 6.3 设置玩家标签

1. 选中Player GameObject
2. 在Inspector顶部的Tag下拉菜单中：
   - 如果没有Player标签，点击 `Add Tag...`
   - 创建新标签 `Player`
   - 返回Player GameObject，设置Tag为 `Player`

### 6.4 配置输入系统

确保项目中配置了输入轴：
1. 打开 `Edit > Project Settings > Input Manager`
2. 确认存在以下输入轴：
   - **Horizontal**: A/D键或左右箭头
   - **Vertical**: W/S键或上下箭头

### 6.5 设置玩家初始位置

1. 在Scene视图中将Player移动到初始关卡节点位置
2. 或在PlayerWorldMapController中设置 **Current Node** 为初始节点
3. 运行时会自动将玩家传送到该节点位置

---

## 7. 测试和调试

### 7.1 基础测试

1. 点击Unity编辑器的Play按钮
2. 测试以下功能：
   - 玩家是否出现在初始节点位置
   - 使用WASD或方向键移动玩家
   - 相机是否跟随玩家移动
   - 点击关卡节点是否有响应

### 7.2 关卡解锁测试

1. 运行游戏后，检查：
   - 初始关卡是否显示为已解锁（黄色或配置的颜色）
   - 其他关卡是否显示为锁定（灰色）

2. 测试关卡完成：
   - 在代码中调用 `WorldMapManager.Instance.CompleteLevel("level_1-1")`
   - 检查下一个关卡是否自动解锁
   - 检查完成的关卡是否变为绿色

### 7.3 进度保存测试

1. 完成几个关卡
2. 停止游戏
3. 重新运行游戏
4. 检查进度是否保存（已完成和已解锁的关卡状态）

### 7.4 调试技巧

#### 使用Debug.Log
在关键位置添加日志：
```csharp
Debug.Log($"关卡 {levelId} 已解锁");
Debug.Log($"当前节点: {currentNode.LevelId}");
```

#### 使用Gizmos
在Scene视图中可视化：
- 关卡节点的交互范围（已在LevelNode中实现）
- 玩家的移动路径
- 相机边界

#### 检查事件订阅
确保事件正确订阅：
```csharp
// 在WorldMapManager的Awake中
Debug.Log($"注册了 {_nodeDict.Count} 个关卡节点");
```

### 7.5 常见问题排查

#### 问题1: 点击关卡节点没有反应
- 检查LevelNode是否有Collider 2D组件
- 确认Collider的Is Trigger已勾选
- 检查EventSystem是否存在（Canvas会自动创建）
- 确认WorldMapManager已正确注册节点

#### 问题2: 关卡状态不正确
- 检查LevelNodeData的配置
- 确认WorldMapManager的Level Nodes In Scene数组已填充
- 检查PlayerPrefs中的数据（可以清除测试）

#### 问题3: 相机不跟随玩家
- 确认PlayerWorldMapController的Camera Follow Player已勾选
- 检查World Map Camera字段是否已赋值
- 确认相机的Z坐标正确（通常为-10）

#### 问题4: 玩家移动不流畅
- 调整Move Speed参数
- 检查是否有碰撞体阻挡
- 确认Input Manager配置正确

### 7.6 性能优化建议

对于大型地图（上千个瓦片）：

1. **使用Tilemap Collider优化**
   - 添加Composite Collider 2D合并碰撞体
   - 减少碰撞检测计算

2. **分块加载（可选）**
   - 将地图分成多个区域
   - 只加载玩家附近的区域
   - 使用Addressables系统

3. **对象池**
   - 如果有大量动态对象，使用对象池
   - 避免频繁的Instantiate和Destroy

4. **减少Draw Call**
   - 使用Sprite Atlas合并图集
   - 启用Batching

---

## 8. 扩展功能

### 8.1 添加路径线

在关卡节点之间绘制连接线：
1. 使用Line Renderer组件
2. 或使用Tilemap绘制路径
3. 根据解锁状态改变线条颜色

### 8.2 添加动画效果

为关卡节点添加动画：
1. 使用Animator组件
2. 创建闲置、解锁、完成等动画状态
3. 添加粒子效果

### 8.3 添加音效

1. 在WorldMapManager中添加AudioSource
2. 在关卡解锁、完成时播放音效
3. 添加背景音乐

### 8.4 小地图系统

创建小地图显示：
1. 使用第二个相机渲染地图
2. 设置Render Texture
3. 在UI上显示小地图

---

## 9. 总结

现在你已经创建了一个完整的大世界关卡选择系统！

**已实现的功能：**
- ✅ Tilemap地图绘制
- ✅ 关卡节点系统
- ✅ 关卡解锁逻辑
- ✅ 进度保存
- ✅ 玩家移动和相机跟随
- ✅ 配置驱动的设计

**下一步建议：**
1. 创建实际的关卡场景
2. 实现关卡完成后返回世界地图
3. 添加UI界面（关卡信息、星级评价等）
4. 集成对话系统（NPC交互）
5. 添加更多视觉效果和动画

**相关文档：**
- Boss配置系统：参考BossBase.cs和BossAttackConfig.cs
- 对话系统：参考Dialogue文件夹中的脚本
- 子弹系统：参考Bullet文件夹中的脚本

祝你开发顺利！🎮
