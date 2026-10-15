# 涌现：本轮玩法扩展

此前的主要问题是：射击、移动和场景互不影响，玩家向右清怪就能得到几乎全部体验。本轮把它们接到同一套材料和环境规则上，让构筑改变通路，通路改变战斗位置，小变化能传递成连锁结果。

## 实际加入的内容

- 章节由约 118 米拓展到 268 米，保留原故事、守卫战和两种结局。新增相变管网、活体菌落、惯性回流三个实验区域，共六个安全终端、七个三选一基因冷库、二十八个敌人。
- 三个区域任意完成两个就可接通核心。管网可以湿润导体形成电路，也可以借蒸汽抵达高处检修节点；菌落可以准备火毒连锁破坏隔板，也可以绕上层台阶抵达标本节点；重量区域可以让人物、箱子或敌人压下阀板，也可绕行后锁住控制台。
- 基础水球能润湿导体、向箱子传递冲量、唤醒菌落、为蓄压罐储水。环境电源沿湿润材料传导；热量与水产生可实际抬起人物及箱子的蒸汽；火与毒雾引发有上限的连锁爆炸。
- F 回流脉冲向瞄准方向推动湿润箱体，S + F 拉回；也可搬运附近已有水场。范围 3.2 米，冷却 2 秒，单次水场移动最多 0.75 米，墙体阻挡，不延长水场寿命。
- 土狼时间 0.14 秒、跳跃缓冲 0.16 秒，按住跳跃略增高度，下降加快。蒸汽弹射单独保留原重力，避免输入释放中断环境推力。
- 原先误放在平台之间、看起来像梯子的竖管，改为线路上方的回流接收信标，不再阻挡跳跃。菌落隔板两侧补齐绕行台阶。
- Monster4 恢复到实验区、生态区和菌落区的治疗小队中。治疗范围 6 米、周期 5 秒、最多治疗 4 个目标，无接触伤害；修复巡逻时治疗动画结束后的状态恢复。

## 构筑与信息反馈

基因编辑界面改成双栏实验台：库存筛选与滚动列表、六格有编号的构筑卡、选中详情与进化进度、附魔顺序、组合反应提示和确认/取消分开显示。每个独立实例仍只能占一格，取消不会提交草稿。三选一冷库提供明确选择，每次选择获得该基因的两份样本。

场景 HUD 显示三条通路状态、回流动作冷却与真实作用结果。TAB 打开涌现笔记，记录实际触发的导电、相变、推力、生长、爆炸、破坏和惯性规则。区域故事采用“观察→修正认识”的文案，与已存在的故事对话界面和两种结局相连。

BeginScene 新增“回流试验”，使用可保存的种子选择高频电路、旺盛菌落或追猎回流规则，并增加对应遭遇。同一种子可以复现规则。本轮仍是手工制作的章节布局，尚未实现随机房间生成或跨局永久成长。

## 玩家操作

| 操作 | 功能 |
|---|---|
| A / D | 移动 |
| 空格 / W | 跳跃；按住略增高度 |
| S + 空格 | 从单向平台下落 |
| 左键 | 射击 |
| 右键 | 依次开启装配的属性阶段 |
| 左 Shift | 获得模块后的瞄准方向冲刺 |
| F / S + F | 推流 / 吸回湿润箱体与水场 |
| E | 终端、冷库、控制台、出口交互 |
| TAB | 涌现笔记 |
| ESC | 关闭当前模态界面或暂停 |

## 存档与验证

材料通路、实际破坏的结构、发现记录、冷库选择和试验种子进入现有检查点存档。走替代路线不会把未破坏的结构误记成已破坏。旧版核心区检查点随地图后移一次，保留基因实例和进化数据。

本轮 MCP 验证记录和截图保存在 [EmergenceValidation](EmergenceValidation/)。全部通过，共 90 项 EditMode 测试和 278 项运行检查，最终控制台无错误或警告。

| 检查 | 通过数 | 记录 |
|---|---:|---|
| 配置、基因、子弹、存档、内容 | 90 | [EditMode](EmergenceValidation/EditMode.json) |
| 土狼时间、缓冲、跳跃弧线与回流边界 | 17 | [PlayerFeel](EmergenceValidation/PlayerFeel.json) |
| Monster4 治疗、连线、动画与死亡中断 | 17 | [Monster4](EmergenceValidation/Monster4.json) |
| 材料连锁、三条通路、替代解法与试验存档 | 48 | [Emergence](EmergenceValidation/Emergence.json) |
| 原战斗、基因编辑、属性与表现回归 | 95 | [Gameplay](EmergenceValidation/Gameplay.json) |
| 实际移动、平台、裂缝、射击和守卫战 | 47 | [WorldTraversal](EmergenceValidation/WorldTraversal.json) |
| 菜单、死亡重试、继续、两种结局 | 31 | [Lifecycle](EmergenceValidation/Lifecycle.json) |
| 剧情交互、独立存档实例与结局界面 | 23 | [Story](EmergenceValidation/Story.json) |

运行检查通过 MCP 在真实 Unity Play Mode 中执行。流程检查会传送到待检节点；平台、跳跃、材料和战斗分别使用真实输入、碰撞或子弹验证，并非连续手动游玩全程。检查和视觉复核结束后恢复原有存档与结局偏好。

已复核 [主菜单](EmergenceValidation/BeginScene.png)、[少量库存](EmergenceValidation/GeneWorkbench-SmallInventory.png)、[完整构筑](EmergenceValidation/GeneWorkbench-FullBuild.png)、[管网跳跃通路](EmergenceValidation/Duct-NoBlockingLadder.png) 和 [新区域剧情](EmergenceValidation/Emergence-Story.png)。主菜单“回流试验”的真实按钮也已验证能进入带种子的试验章节。

Windows 构建已更新：[WaterDrain.exe](../Builds/WaterDrain/WaterDrain.exe)，完整目录约 559.46 MB，构建 0 错误 / 0 警告，见 [Build.json](EmergenceValidation/Build.json)。运行时需保留同目录的 `WaterDrain_Data`、`UnityPlayer.dll` 和 Mono 依赖。

已启动新构建的可见窗口，进程 `WaterDrain` 响应正常，窗口标题为 `Water Drain`。启动日志已完成 Direct3D、Mono、物理与输入初始化，未发现异常或错误，见 [StartupSmoke.log](../Builds/WaterDrain/StartupSmoke.log)。Unity 编辑器停在 BeginScene，独立游戏窗口保留供游玩。

## 后续拓展优先级

1. 先用本轮三组共用规则做多种遭遇排列，增加敌人和机关互相影响的局面。避免只增加血量和地图长度。
2. 为可重复试验增加小房间池、可选风险与奖励，让玩家在“稳定通路”和“更强构筑”之间做取舍。
3. 通过测试玩家发现率、通关时间和死亡原因调整提示、冷却与遭遇；继续保证必要操作可读、通路可达和反应有界。
