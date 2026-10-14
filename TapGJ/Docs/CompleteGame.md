# 地漏 · 回流纪事试玩版

本轮把现有工程整理为可通关的一章。入口是 `Assets/Scene/BeginScene.unity`，随后进入 `GameScene`。旧砖墙地图已从实际游戏场景移除，原美术文件仍保留供回溯。

Windows 试玩入口：`Builds/WaterDrain/WaterDrain.exe`。保留旁边的 `WaterDrain_Data`、`UnityPlayer.dll` 和其他依赖，整目录一起使用。Unity Mono / Windows x64 正式构建成功，0 错误、0 警告；成品的无图形启动检查未发现运行异常。界面与通关验证在编辑器中通过 MCP 完成。

## 本轮内容

- 主菜单与地图统一为紫色洞穴、粉色管道、青色水晶和荧绿黏液的卡通风格。主菜单保留独立可编辑的标题、按钮和动画角色。
- 约 118 米的关卡包含废弃实验区、生态区、核心区；三座基因终端、三条支线冷库、冲刺模块、两处可跳越裂缝、恢复补给、普通遭遇和三名核心守卫。
- 老鼠和晶体近战怪取消碰撞伤害，改为 0.35 秒提示前摇、专用八帧攻击动作、约 0.15 秒后命中，普通攻击造成 2 点伤害；攻击会复核距离、方向和无敌状态。关卡内敌人的恢复间隔更长，便于首次试玩。
- 开场、维修日志、实验记录、样本获取、区域过渡、守卫战、最终选择和两个结局均有中文文案与实际 UI。目标面板只在游玩时显示，对话有逐字呈现、点击/空格继续和跳过本段。
- 安全区记录检查点并回血。死亡重试和主菜单继续会恢复基因实例、等级、累计份数、装配槽位与冲刺；已领固定奖励保持领取状态。失足会扣除至多一点生命并返回安全区，保留最后一点生命。
- 最终选择产生“水的归处”或“最后的回声”。完成后可再次探索或返回主菜单，继续按钮只用于尚未完成的存档。
- 增加轻柔的 24 秒洞穴氛围循环，以及水泡射击、拾取、受伤、冲刺、跳跃、落地和 UI 点击音效。由本地代码合成，复用原有音源；音乐和音效滑块控制实际播放音量。

## 操作

| 操作 | 按键 |
| --- | --- |
| 左右移动 | A / D |
| 跳跃 | 空格 / W |
| 穿下单向平台 | S + 空格 |
| 朝鼠标射击 | 按住左键 |
| 激活已装配属性阶段 | 右键 |
| 阅读、领取、编辑、最终阀门 | 靠近后 E |
| 朝鼠标方向冲刺 | 解锁后左 Shift |
| 暂停 | Esc |
| 对话补全/继续 | 空格或点击 |

## 验证证据

所有运行检查通过 Unity MCP 在实际编辑器中执行。原始结果存于 `Docs/CompleteGame/`：

- `lifecycle-verification.json`：31 项完整场景流程检查，覆盖开始、死亡重试、暂停返回、继续、两个结局与存档。
- `world-verification.json`：47 项实际 E/空格/Shift/鼠标输入检查，覆盖支线平台、裂缝、失足恢复、基因终端以及实际子弹击败核心守卫。该组关闭 AI 以独立检查几何和武器；AI 战斗另行验证。
- `story-verification.json`：23 项对白输入、强制选择、两个结果页和基因实例恢复检查。
- `gameplay-verification.json`、`editmode-verification.json` 与 `build-verification.json`：最终战斗回归、资产/代码检查和打包记录。
- 最终 86 项 EditMode 和 95 项游戏运行回归均通过；`audio-verification.json` 确认八段声音有非零信号、只有一个监听器、氛围循环播放及滑块控制真实音源。
- `framing-verification.json` 与最后的地图截图确认玩家脚部位于基因栏上方，避免出生点被底部 HUD 遮挡。

这里交付的是一章 GameJam 试玩闭环。角色动作沿用上一轮的专用姿态与程序反馈，近战怪使用已有八帧攻击素材；并非制作需求单里所有动作与全部章节的最终美术产量。

## 美术与维护

新增图像使用内置 `image_gen`。原始输出与十张拆分后的透明道具/地形 Sprite 都在 `Assets/ArtRes/WorldPolished/`，制作规格整理在 `Docs/CompleteGameArtPrompts.json`。主菜单文字与游戏文案使用工程里的 Noto Sans CJK SC Medium，字体授权随工程保留。

编辑器维护入口：`CompleteGameArtPreparation.Prepare()` 准备 Sprite，`EnemyCombatPolishSetup.Apply()` 配置近战 Prefab；打开对应场景后调用 `CompleteGameWorldBuilder.Apply()` 或 `CompleteGameMenuBuilder.Apply()`。地图重建后调用 `PlayerUIPolishSetup.Apply()`，再用 `StoryUIFactory.Ensure(StoryUIFactory.FindGameplayCanvas())` 创建屏幕剧情 UI 并保存场景。

详细剧情说明见 `Docs/CompleteGameStory.md`。截图包括 `menu.png`、`laboratory.png`、`story.png`、`choice.png` 和 `ending.png`。
