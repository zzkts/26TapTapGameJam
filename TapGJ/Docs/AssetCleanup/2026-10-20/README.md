# 场景和资源清理（2026-10-20）

当前正式场景只保留 `Assets/Scene/BeginScene.unity` 和 `Assets/Scene/GameScene.unity`。三个环境区域继续位于同一张 GameScene 中；没有修改关卡物体、碰撞、玩法、资源 GUID 或存档格式，没有生成安装包。

## 本次删除

- 未使用的 `Assets/Scene/test.unity`、URP 2D 模板场景及其场景模板资源。
- 当前场景、运行时加载、预制体、动画控制器和现用编辑工具均不依赖的旧鼠怪／水母动画与帧图。
- 不再使用的旧玩家参考图；保留当前玩家动作图及重新切图所需的源图。
- 旧菜单波浪动画／控制器，以及没有任何加载入口的黑色 CG 图。
- `AssetArchive/2026-10-15-unused`、`AssetArchive/2026-10-16-gene-spec` 两批先前已停用的归档，以及清理后产生的空目录。

共删除 **104 个未使用的 Assets 资源**；加上先前归档共 **1233 个已校验文件，147.53 MiB**，另清理 **69 个空目录**及其残留 meta。每个文件的原路径、大小和 SHA-256 记录在 [RemovedFiles.json](RemovedFiles.json)，空目录记录在 [RemovedEmptyFolders.txt](RemovedEmptyFolders.txt)。本次直接删除，不再转移到新的资源归档；删除前的文件已存在于 Git 历史中。

## 保留依据

以 Unity AssetDatabase 的真实依赖、序列化 GUID、正式构建场景、ProjectSettings 引用、Resources 动态加载、现用编辑工具和回归测试共同确定保留集合。保留当前角色、Monster4 治疗怪及其他敌人、全部基因、音乐和署名、场景外观、图集、地图拼装预制体、现用源图及字体许可证。

清理过程中恢复了审计脚本误写的 XR 设置；未保留该设置变更。

## 验证

- [StaticValidation.json](StaticValidation.json)：所有删除目标均已不存在，保留的序列化文件没有指向被删除 GUID 的引用。
- [BeforeValidation.json](BeforeValidation.json)、[AfterValidation.json](AfterValidation.json)：清理前后两个正式场景、19 个预制体、366 个 SpriteRenderer 均通过检查，未发现缺失脚本、断裂对象引用、丢失精灵或项目着色器编译错误。
- [RuntimeValidation.json](RuntimeValidation.json)：16 项运行检查全部通过。使用现有内存存档隔离入口，检查开始菜单、进入关卡、玩家／基因／Monster4、音乐、暂停设置、返回菜单及真实存档与结局记录保持不变。

保留只在编辑器生效的检查工具：`Tools/TapGJ/Project/Export Asset Dependency Audit`、`Tools/TapGJ/Project/Validate Retained Scenes And Prefabs`。测试输出位于被忽略的 `Temp/AssetCleanup`；清理审计及回归工具不会加入游戏的发行代码。
