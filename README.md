# UnitySY

UnitySY 是一个面向博物馆/展陈混合现实体验的 Unity 工程。它使用 MRTK 与 OpenXR 提供 MR 交互，使用 HybridCLR 加载业务热更程序集，并通过 YooAsset 管理场景、模型、视频、配置和热更 DLL 的内容更新。

## 主要功能

- 博物馆点位触发：碰撞、距离、MRTK 输入与按钮路由。
- Museum/N 提供点位私有强类型消息总线：输入适配器、单选、视觉、视频、文物、旋转及双物体切换模块可独立组合，并保留统一退出生命周期。
- 点位动作：物体显隐、自动旋转、缩放、材质闪烁和视频播放。
- MR 相机/物体跟随与交互控制。
- AOT 模块宿主、服务注册和资源更新状态机。
- YooAsset 版本检查、下载、校验、离线回退和场景加载。
- HybridCLR 热更程序集与 AOT 补充元数据加载。
- 编辑器内热更 DLL 整理、YooAsset 内容构建和本地发布工具。
- 运行时调试面板。

## 环境要求

- Windows 10/11。
- Unity Editor `2022.3.10f1`（项目记录的 revision：`ff3792e53c62`）。
- 通过 Unity Hub 安装目标平台模块；移动 MR 发布通常需要 Android Build Support（SDK/NDK/OpenJDK），Windows MR 构建需要相应 Windows/Visual Studio 工具链。
- Git；大二进制资源建议在创建远程前评估 Git LFS。
- 网络可访问 HybridCLR 的 Gitee Git 依赖。
- YooAsset 3.0.5。目前 `Packages/manifest.json` 指向旧电脑的 `D:/Software/YooAsset-3.0.5/.../Assets/YooAsset`，新电脑必须把 YooAsset 放到同一路径，或将依赖改为团队认可的仓库内/Git/registry 来源并提交该可移植修改。

## 安装与新电脑初始化

1. `git clone <GitHub 仓库地址>`，进入仓库。
2. 阅读 `AGENTS.md`、本文件、`CHANGELOG.md`、`docs/ARCHITECTURE.md` 和 `git log -10 --oneline --decorate`。
3. 用 Unity Hub 安装 `2022.3.10f1` 及目标平台模块。
4. 解决上述 YooAsset 路径，确认 `Packages/manifest.json` 不再依赖不存在的本机目录。
5. 用 Unity Hub 打开仓库根目录，等待包解析与脚本编译完成。
6. 在 Unity 执行 `Museum > Hot Update > Validate Setup`；若 HybridCLR 运行时未安装，按团队约定准备官方源码后再执行配置/安装流程。
7. 检查 Build Settings。当前启动场景为 `Assets/Scenes/Root.unity`；不要凭空采用编辑器工具里已经失效的 `Assets/Museum/...` 路径。
8. 运行 EditMode、PlayMode 测试以及目标平台构建，最后检查 `git status`。

不要从旧电脑复制 `Library`、`Temp`、`Logs`、`UserSettings` 或 IDE 生成文件；Unity 会重新生成它们。

## 运行

当前可检查的启动场景为 `Assets/Scenes/Root.unity`，其配置资源位于同目录。使用 Unity 打开该场景，确认 Inspector 引用与运行模式后点击 Play。`Assets/3_ResourceFile/Scenes/museum.unity` 是现有博物馆内容场景。

注意：旧的 `UpdateSceneBuilder` 仍引用不存在的 `Assets/Museum/...`。在修复路径并验证前，不要执行它来覆盖 Build Settings。

## 构建

先在 Build Settings 切换到目标平台。Unity 菜单入口：

- `Museum > Build > 0. Increment Content Version`
- `Museum > Build > 1. Compile Hot Update DLL (Fast)`：日常热更代码迭代。
- `Museum > Build > 2. Generate HybridCLR Files (Full)`：正式 Player 发布前执行。
- `Museum > Build > 2b. Stage Generated HybridCLR Files`
- `Museum > Build > 3. Build YooAsset Package`
- `Museum > Build > 4. Build All For Active Target`

命令行示例（PowerShell，先设置本机 Unity 路径）：

```powershell
# 本机当前检测到的安装路径；Unity Hub 默认安装通常位于
# C:\Program Files\Unity\Hub\Editor\2022.3.10f1\Editor\Unity.exe
$Unity = 'C:\Program Files\Unity 2022.3.10f1\Editor\Unity.exe'
& $Unity -batchmode -quit -projectPath (Get-Location) -executeMethod Museum.Infrastructure.EditorTools.MuseumContentBuildPipeline.BuildAllForActiveTarget -logFile .\Artifacts\build.log
```

该全量入口生成 HybridCLR/YooAsset 内容，不等同于已配置输出路径的最终 Player 构建；当前项目未提供稳定的命令行 Player 构建方法。新增自动构建时应实现显式 `BuildPlayerOptions`、输出目录和目标平台校验。

## 测试

Unity Test Framework 已安装。Museum/N 当前有 `Museum.N.Tests`（EditMode）和 `Museum.N.PlayMode.Tests`（PlayMode）两个测试程序集；全项目标准命令如下：

```powershell
& $Unity -batchmode -quit -projectPath (Get-Location) -runTests -testPlatform EditMode -testResults .\Artifacts\editmode-results.xml -logFile .\Artifacts\editmode.log
& $Unity -batchmode -quit -projectPath (Get-Location) -runTests -testPlatform PlayMode -testResults .\Artifacts\playmode-results.xml -logFile .\Artifacts\playmode.log
```

修改功能时必须新增/更新测试；在 CI 中把 XML 测试结果和日志保留为构建产物，但不要提交到 Git。

## 数据与配置位置

- Unity/产品配置：`ProjectSettings/`。
- 包依赖与锁文件：`Packages/manifest.json`、`Packages/packages-lock.json`。
- AOT 框架和更新启动：`Assets/1_ScriptAOT/`。
- 热更业务代码：`Assets/2_ScriptHotUpdate/`。
- 模型、场景和业务资源：`Assets/3_ResourceFile/`。
- 当前启动配置：`Assets/Scenes/YooAssetRuntimeConfig.asset`、`Assets/Scenes/HotUpdateLoadSettings.asset`。
- YooAsset 收集配置：`Assets/BundleCollectorSetting.asset`。
- 本机生成的 YooAsset 内置包：`Assets/StreamingAssets/yoo/`（不提交，由内容构建流程重新生成）。
- HybridCLR 生成代码：`Assets/HybridCLRGenerate/`；本地运行时与中间数据：`HybridCLRData/`（不提交，由 HybridCLR 安装/生成流程恢复）。
- 运行缓存：Unity/YooAsset 的 `persistentDataPath`；已校验包版本还写入 `PlayerPrefs`。这些均为本机数据，不提交。
- 编辑器产物：默认 YooAsset 输出为仓库根的 `Bundles/`；日志/测试结果建议放 `Artifacts/`，二者均忽略。

## 当前已知限制

- YooAsset 本地绝对路径导致跨电脑包恢复失败。
- 编辑器构建路径已对齐当前 `Assets/Scenes`、`Assets/2_ScriptHotUpdate` 和 `Assets/3_ResourceFile` 布局。
- Build Settings 已将 `Assets/Scenes/Root.unity` 配置为启动场景。
- 除 Museum/N 外，其他业务模块仍没有项目自有自动化测试。
- Museum/N 已新增独立 EditMode/PlayMode 测试，但新方案尚未迁移现有 SmallModel 预制体；挂载步骤见 Assets/2_ScriptHotUpdate/Museum/N/README.md。
- `UnitySY.sln` 当前含两个同名 `Unity.Timeline` 项目，不能直接用 `dotnet build UnitySY.sln`；应以 Unity 编译/测试为准并重新生成解决方案。
- `HybridCLRData/` 含可重建产物和第三方嵌套仓库，已整体忽略；新电脑必须按 HybridCLR 配置流程重新生成。
- GitHub 远程已配置为 `origin`（`https://github.com/GuiYe314/UnityMF.git`）；推送前仍须检查暂存区，避免带入本机数据或无关修改。

更多模块关系和发布流程见 `docs/ARCHITECTURE.md`，长期开发规则见 `AGENTS.md`。
