# 水特效

`TapGJ.WaterProjectiles.WaterVfx` 只负责生成水花表现，不再包含独立的子弹、碰撞或扣血逻辑。

子弹由 BattleSystem 中的通用 `Projectile` 负责；贴图、材质和着色器仍作为 `WaterVfx` 的表现资源保留。
