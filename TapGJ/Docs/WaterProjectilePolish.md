# 水弹的液体质感

2026-10-15，更新 Unity 项目，未打包。

基础水弹改用独立的 `TapGJ/Liquid Drop 2D` 材质，不再被通用元素着色器覆盖。水体有半透明内部、较深的体积色、边缘反光、弯曲高光与轻微流动的内部光弧；轮廓随飞行方向旋转，随速度拉伸，并有小幅液体晃动。

拖尾使用短暂的透明水丝，飞行沿途脱落小水滴。撞击使用真正的接触点和表面法线，水滴向外散开后受重力下落，接触面出现短暂水膜；空中寿命结束只散成水滴，不生成接触面水膜。特效在短时间内自动销毁。

修正 `DefaultWaterBallWeaponData` 错误引用 `FireProjectile` 的配置，现在开局使用 `WaterProjectile`。水弹的视觉变形只作用于子物体，伤害、速度、圆形碰撞范围以及基因快照保持原有规则。反弹时让弹体离开碰撞接触边缘，避免下一帧被同一表面再次拦截。

这是适配当前二维卡通画风的液体表现，使用材质、粒子和现有运动规则，不是完整流体模拟。

## MCP 验证

针对子弹、元素和基础武器配置执行 EditMode 回归；运行检查使用当前场景主角的实际鼠标连射，再检查临时墙体撞击、反弹、环绕和渲染画面。敌人 AI 暂停以隔离视觉验证，临时对象与输入设备在检查后清理。

记录与实际渲染画面位于 `Docs/WaterVisualValidation/`。

- [EditMode 回归：38 / 38 通过](WaterVisualValidation/EditMode.json)
- [运行时水弹检查：21 / 21 通过](WaterVisualValidation/WaterPlayMode.json)
- [主角实际鼠标射击](WaterVisualValidation/Held-Water-Fire.png)
- [撞击水膜与飞溅](WaterVisualValidation/Wall-Splash.png)
- [实际着色器水体近景](WaterVisualValidation/Water-Surface-Closeup.png)

最后读取 Unity Console，游戏错误与警告均为 0。Unity 返回 BeginScene，点击 Play 后可从“开始故事”体验新的默认水弹，也可继续现有章节。
