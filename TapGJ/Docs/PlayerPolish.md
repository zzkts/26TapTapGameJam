# 玩家人物与界面打磨

2026 年 10 月 14 日。已接入 GameScene 和 Player prefab，可直接在 Unity 6000.3.8f1 中试玩。

本轮优先处理玩家造型不一致、HUD 占位色块、楷体可读性、动作被射击截断，以及交互文字压在地图上等问题。角色统一为参考图中的奶油色地漏幽灵、黑面罩黄眼睛、深灰盖帽和绿色泡泡。

## 完成内容

- 使用内置 image_gen 生成角色头像、紫色黏液 HUD 面板和 16 张人物姿态；素材已经保存在工程中，原图保留。
- 在 Unity 编辑器中拆分姿态图，统一为 320×320 画布和脚底锚点。九种动作使用专用素材；Idle / Run 各 4 帧，其余为专用动作姿态及程序运动反馈。不是需求单中全部动作的最终逐帧产量。
- 补上呼吸、跑步微弹、起跳伸缩、落地回弹、射击后坐力、受击压缩、轻微镜头震动和冲刺残影。表现变换作用于视觉子节点。
- 修正射击与移动的动作优先级；冲刺进入与退出立即同步碰撞层。死亡动画在暂停菜单下仍能完成并保持最终姿态。
- HUD 使用头像、分段液位、伤害损失拖尾、冲刺键帽与环形冷却、紧凑六槽、独特基因符号、当前属性指示、附魔/整链重载提示、获取卡片和角色附近的 E 交互提示。
- 基因页加入独立实例编号、实际装配槽位、选中反馈、高级标记、圆角库存卡片、滚动条及空状态引导。
- 暂停、设置和死亡菜单统一为紫色面板、奶油/薄荷按钮及角色头像。死亡页隐藏继续按钮并重新排布剩余按钮。
- 游戏界面使用 Noto Sans CJK SC Medium。字体与 SIL Open Font License 一并保存在 Assets/ArtRes/Fonts/PlayerUI。来源为 https://github.com/notofonts/noto-cjk/tree/main/Sans。

## 美术与维护入口

| 用途 | 工程位置 |
| --- | --- |
| HUD 与头像 | Assets/ArtRes/UI/Player/ |
| 角色生成源图 | Assets/ArtRes/Reference/Player/Player-polished-sheet.png |
| 统一人物姿态帧 | Assets/ArtRes/Animation/PlayerPolished/ |
| 九种动作与控制器 | Assets/ArtRes/Animation/Gameplay/Player/ |
| 最终生图提示词与输出路径 | Docs/PlayerArtPrompts.json |
| 编辑器检查原始结果 | Docs/PlayerPolish/editor-verification.json |
| 实际运行检查原始结果 | Docs/PlayerPolish/runtime-verification.json |
| 死亡重试结果 | Docs/PlayerPolish/death-retry-verification.json |
| 验证截图 | Docs/PlayerPolish/ |

`Tools/TapGJ/Polish Player UI` 只重建玩家 UI，保留现有世界布置。`Tools/TapGJ/Player/Apply Character Animation Polish` 重建新角色动作；源图拆分入口为 `Slice Approved Character Sheet`。

`Apply Gameplay Polish` 仍会重建世界配置，但在新人物素材存在时会最后接入新人物与 UI，避免回退成旧造型。后续手工调整地图时不要把这个入口当作普通刷新。

## Unity MCP 验证

通过本机 Unity MCP 连接到 TapGJ 实例实际执行：

- EditMode 70 / 70 通过，包括九种动作素材、画布和 Pivot、非循环死亡状态、碰撞体脚底、Shader、场景与预制体缺失引用检查。
- GameplayPolishPlayModeChecks 79 / 79 通过，包括移动中持续射击、后坐力、跳跃动作优先级、空中冲刺、碰撞层切换、受击时停、落地回弹、基因草稿取消/确认、属性轮换、子弹快照及实际元素渲染。
- 1366×768 和 1920×1080 的玩家 HUD / 基因编辑页已截图检查。截图中的装配属于验证示例。
- 实际按钮回调验证暂停 → 设置 → 返回保持暂停 → 继续恢复游戏；死亡 → 重新开始恢复 10 / 10 生命、存活和正常时间。
- 脚本 diff 空白检查通过。Unity 序列化文件仍使用编辑器的标准空字段格式。

地图和部分世界交互物仍使用当前原型美术；整套关卡、敌人、主菜单与叙事并未在本轮统一重画。人物逐帧动画后续可继续扩充，自动检查也不能代替完整关卡试玩与数值平衡。
