# 自定义子弹行为开发指南

## 概述

通过实现 `IBulletBehavior` 接口，你可以为子弹添加任何自定义机制。

---

## IBulletBehavior 接口

```csharp
public interface IBulletBehavior
{
    void Initialize(Bullet bullet);           // 初始化
    void OnUpdate();                          // 每帧更新
    bool OnBoundaryHit(Vector2 normal);       // 碰到边界
    bool OnTargetHit(Collider2D target);      // 碰到目标
    void Reset();                             // 重置状态
}
```

### 返回值说明

- `OnBoundaryHit` 和 `OnTargetHit` 返回 `true` 表示行为已处理碰撞，子弹不会被销毁
- 返回 `false` 表示使用默认行为（销毁子弹）

---

## 示例1：追踪子弹

创建一个会追踪玩家的子弹：

```csharp
using UnityEngine;
using DodgeDots.Bullet;

public class BulletHomingBehavior : MonoBehaviour, IBulletBehavior
{
    [Header("追踪设置")]
    [SerializeField] private float rotationSpeed = 180f; // 转向速度（度/秒）
    [SerializeField] private float trackingDelay = 0.5f; // 延迟追踪时间

    private Bullet _bullet;
    private Transform _target;
    private float _elapsedTime;

    public void Initialize(Bullet bullet)
    {
        _bullet = bullet;
        _elapsedTime = 0f;

        // 查找玩家目标
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _target = player.transform;
        }
    }

    public void OnUpdate()
    {
        if (_target == null || _bullet == null) return;

        _elapsedTime += Time.deltaTime;

        // 延迟后开始追踪
        if (_elapsedTime < trackingDelay) return;

        // 计算朝向目标的方向
        Vector2 currentPos = _bullet.transform.position;
        Vector2 targetPos = _target.position;
        Vector2 desiredDirection = (targetPos - currentPos).normalized;

        // 平滑转向
        Vector2 currentDirection = _bullet.Direction;
        Vector2 newDirection = Vector2.RotateTowards(
            currentDirection,
            desiredDirection,
            rotationSpeed * Mathf.Deg2Rad * Time.deltaTime,
            0f
        );

        _bullet.SetDirection(newDirection);
    }

    public bool OnBoundaryHit(Vector2 normal)
    {
        return false; // 碰到边界就销毁
    }

    public bool OnTargetHit(Collider2D target)
    {
        return false; // 碰到目标就销毁
    }

    public void Reset()
    {
        _elapsedTime = 0f;
        _target = null;
    }
}
```

---

## 示例2：分裂子弹

碰到目标后分裂成多个小子弹：

```csharp
using UnityEngine;
using DodgeDots.Bullet;
using DodgeDots.Core;

public class BulletSplitBehavior : MonoBehaviour, IBulletBehavior
{
    [Header("分裂设置")]
    [SerializeField] private int splitCount = 4;
    [SerializeField] private BulletConfig splitBulletConfig;
    [SerializeField] private float splitSpeedMultiplier = 0.7f;

    private Bullet _bullet;
    private bool _hasSplit = false;

    public void Initialize(Bullet bullet)
    {
        _bullet = bullet;
        _hasSplit = false;
    }

    public void OnUpdate()
    {
        // 分裂行为不需要每帧更新
    }

    public bool OnBoundaryHit(Vector2 normal)
    {
        return false; // 碰到边界不分裂
    }

    public bool OnTargetHit(Collider2D target)
    {
        if (_hasSplit) return false;

        _hasSplit = true;

        // 在碰撞位置生成分裂子弹
        Vector2 position = _bullet.transform.position;
        float angleStep = 360f / splitCount;

        for (int i = 0; i < splitCount; i++)
        {
            float angle = angleStep * i;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            var splitBullet = BulletManager.Instance.SpawnBullet(
                position,
                direction,
                _bullet.Team,
                splitBulletConfig
            );

            if (splitBullet != null)
            {
                splitBullet.SetSpeed(_bullet.Speed * splitSpeedMultiplier);
            }
        }

        return false; // 分裂后销毁原子弹
    }

    public void Reset()
    {
        _hasSplit = false;
    }
}
```

---

## 示例3：加速子弹

随时间加速的子弹：

```csharp
using UnityEngine;
using DodgeDots.Bullet;

public class BulletAccelerateBehavior : MonoBehaviour, IBulletBehavior
{
    [Header("加速设置")]
    [SerializeField] private float acceleration = 2f; // 加速度
    [SerializeField] private float maxSpeed = 15f;    // 最大速度

    private Bullet _bullet;
    private float _currentSpeed;

    public void Initialize(Bullet bullet)
    {
        _bullet = bullet;
        _currentSpeed = bullet.Speed;
    }

    public void OnUpdate()
    {
        if (_bullet == null) return;

        // 逐渐加速
        _currentSpeed += acceleration * Time.deltaTime;
        _currentSpeed = Mathf.Min(_currentSpeed, maxSpeed);

        _bullet.SetSpeed(_currentSpeed);
    }

    public bool OnBoundaryHit(Vector2 normal)
    {
        return false;
    }

    public bool OnTargetHit(Collider2D target)
    {
        return false;
    }

    public void Reset()
    {
        _currentSpeed = 0f;
    }
}
```

---

## 使用自定义行为

### 1. 创建行为脚本
将上述代码保存为 `.cs` 文件，放在 `Scripts/Bullet/` 目录下。

### 2. 创建子弹预制体
1. 复制现有的Bullet预制体
2. 添加自定义行为组件
3. 配置行为参数

### 3. 注册到BulletManager
在BulletManager的Bullet Types中添加新的配置和预制体。

### 4. 使用
```csharp
[SerializeField] private BulletConfig homingBulletConfig;

void ShootHomingBullet()
{
    BulletManager.Instance.SpawnBullet(
        transform.position,
        Vector2.up,
        Team.Enemy,
        homingBulletConfig
    );
}
```

---

## 组合多个行为

一个子弹可以同时拥有多个行为组件：

```
BouncingHomingBullet预制体：
- Bullet组件
- BulletBounceBehavior组件
- BulletHomingBehavior组件
```

这样就创建了一个既会反弹又会追踪的子弹！

---

## 最佳实践

1. **性能优化**：避免在OnUpdate中进行昂贵的计算
2. **空值检查**：始终检查_bullet和其他引用是否为null
3. **状态重置**：在Reset()中正确重置所有状态
4. **返回值**：正确使用OnBoundaryHit和OnTargetHit的返回值
5. **配置驱动**：使用SerializeField暴露参数，便于调整
