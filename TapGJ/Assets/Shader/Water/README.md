# 游戏场景中使用水弹

在自己的射击代码中引用命名空间 `TapGJ.WaterProjectiles`，调用：

`WaterProjectile.Fire(position, direction, damage, speed, owner)`

- position：枪口世界坐标 Vector2，游戏在 XY 平面，子弹生成于 z = 0。
- direction：世界坐标方向 Vector2，例如目标位置减去枪口位置，无需提前归一化。
- damage：命中敌人的伤害，默认 1。
- speed：飞行速度，默认 12 米/秒。
- owner：玩家根 GameObject，用于忽略自己和自己的子碰撞体，可省略。

一次调用发射一颗，函数返回生成的 WaterProjectile。零方向或非正速度不会发射，返回 null。
按键、鼠标瞄准、冷却和连发由自己的玩家/武器脚本控制；不需要额外发射组件或演示场景。
Prefab 自动从 `Assets/Resources/WaterProjectiles/WaterBullet.prefab` 加载。

## 碰撞和扣血

项目已创建 Enemy、Wall 两个 Layer。敌人和墙壁需要 Collider2D：

- 将敌人实际挂 Collider2D 的 GameObject 设置为 Enemy 层，支持 Trigger。
- 将墙壁实际挂 Collider2D 的 GameObject 设置为 Wall 层，TilemapCollider2D 也可使用。
- 命中 Enemy 或 Wall 播放水花并销毁子弹，其它 Layer 不触发命中。
- Wall 不扣血。Enemy 在其自身或父级查找实现 IWaterDamageable 的血量组件，扣血一次。

在已有敌人血量组件上实现 `IWaterDamageable`，添加方法 `public void TakeWaterDamage(float damage)`，
方法内调用你们自己的扣血函数。没有实现接口的 Enemy 仍显示水花，但不会扣血。
本模块不提供另一套敌人血量系统。

## 外观参数

在 WaterBullet Prefab 的 WaterProjectile 组件上调整 Radius、Lifetime、Gravity Scale、Water Color、
Add Trail、Add Droplets、Splash Scale 和 Sorting Order。
Gravity Scale 为 0 时直线飞行，提高后水弹下坠。视觉参数在生成时读取，调整 Prefab 后发射新子弹观察。
WaterVfx、贴图、材质和着色器是水弹正常显示所需的资源；试玩、HUD、编辑器生成工具及示例血量均已移除。
