# UnitySY 开发约定

本文件是本仓库跨电脑、跨 Codex 会话的长期开发上下文。仓库内规则优先于聊天记录；重要决定必须落盘。

## 开工前必读

每次开发前必须依次阅读：

1. `AGENTS.md`
2. `README.md`
3. `CHANGELOG.md`
4. `docs/ARCHITECTURE.md`
5. 最近 10 条 Git 提交：`git log -10 --oneline --decorate`

随后运行 `git status --short --branch`，确认并保留用户已有修改。不得覆盖、回退或顺手整理与当前任务无关的改动。

## 环境与常用命令

- Unity：`2022.3.10f1`，通过 Unity Hub 安装；必须使用相同补丁版本。
- 首次打开前先修复 `Packages/manifest.json` 中 YooAsset 的本机绝对路径，详见 README。
- 编辑器运行：打开 `Assets/Scenes/Root.unity`，确认所需配置引用后点击 Play。该场景也是当前 Build Settings 的启动场景。
- 快速热更 DLL：Unity 菜单 `Museum > Build > 1. Compile Hot Update DLL (Fast)`。
- 正式热更生成：`Museum > Build > 2. Generate HybridCLR Files (Full)`。
- YooAsset 包：`Museum > Build > 3. Build YooAsset Package`。
- 全量内容构建：`Museum > Build > 4. Build All For Active Target`。
- 命令行等价入口：`Unity.exe -batchmode -quit -projectPath <仓库> -executeMethod <完整方法名> -logFile <日志路径>`；完整方法名见 README。
- 测试：`Unity.exe -batchmode -quit -projectPath <仓库> -runTests -testPlatform EditMode -testResults <结果.xml> -logFile <日志>`，再运行 PlayMode。当前没有项目自有测试，新增/修改功能必须补齐对应测试程序集与用例。

## 修改功能必须遵守

- 功能增加、修改或删除时，同步更新测试、README/架构说明（适用时）和 `CHANGELOG.md`。
- 改变持久化结构时必须升级数据版本，并提供旧数据迁移或向后兼容逻辑及迁移测试。当前持久化包括 YooAsset 缓存/清单以及 `PlayerPrefs` 中的已校验包版本。
- AOT 与热更边界要稳定：跨边界契约放在 `Assets/1_ScriptAOT/Runtime/Framework/Contracts`；业务热更逻辑放在 `Assets/2_ScriptHotUpdate`；不得让 AOT 代码直接依赖仅存在于热更程序集的具体实现。
- UI 只通过 `UpdateSceneBootstrap` 和框架接口驱动更新流程，不直接调用 YooAsset/HybridCLR。
- YooAsset 已发布版本目录不可覆盖；先递增内容版本，再生成新包。
- 平台相关 HybridCLR/AOT 产物不可跨平台复用；构建前确认 Unity 当前 Active Build Target。
- Unity 资产移动/重命名时保留并提交配套 `.meta` 文件；不要手工生成或提交 `Library`、`Temp`、`Logs` 等本机目录。

## 安全边界

- 不得提交密码、Token、私钥、个人数据、日志、本机运行数据或包含这些内容的配置。
- CDN 地址、内网 IP 和本地绝对路径必须通过不含秘密的示例配置或本地覆盖提供；提交前检查新增文本与序列化资产。
- 不运行来源不明的 Editor 菜单或脚本。构建工具会创建/复制 DLL、AOT 元数据、Bundles 和 StreamingAssets，执行前核对目标目录。
- 不覆盖已发布 YooAsset 版本；不要把 `Library` 内缓存当作源文件。
- 第三方源码/包的许可证和来源必须可追溯；不要直接修改缓存包，确需修改时将其纳入仓库并记录决策。

## 已知问题与维护注意事项

- `Packages/manifest.json` 的 YooAsset 使用 `D:/Software/...` 绝对路径，跨电脑不可恢复；应改为仓库内包、可访问的 Git/registry 依赖，或在新电脑上按 README 修复。
- `MuseumContentBuildPipeline`、`UpdateSceneBuilder` 与 HybridCLR 配置入口必须持续使用当前 `Assets/Scenes`、`Assets/2_ScriptHotUpdate`、`Assets/3_ResourceFile` 布局；重组目录时必须同步更新并验证这些常量。
- `ProjectSettings/EditorBuildSettings.asset` 当前以 `Assets/Scenes/Root.unity` 为启动场景；修改启动流程时必须同步验证该配置。
- 当前没有项目自有 EditMode/PlayMode 测试。
- `UnitySY.sln` 有重复的 `Unity.Timeline` 项目名，当前不能作为可靠的 `dotnet build` 入口；应由 Unity 重新生成并以 Unity 编译结果为准。
- `HybridCLRData/` 是可重建的本机目录并含第三方嵌套仓库，必须保持忽略；需要时通过 HybridCLR 配置/生成流程恢复。
- 仓库初始体积可能较大，包含 MRTK、SLATE、预构建 StreamingAssets 与 HybridCLR 数据；初始化远程前检查许可证和 GitHub 单文件/仓库大小限制。

## 完成定义

1. 更新实现、对应测试及仓库文档。
2. 使用目标 Unity 版本运行适当的 EditMode/PlayMode 测试和构建；无法运行时记录具体阻塞，不能声称通过。
3. 检查 `git status`、`git diff --check`、`git diff`，确认无秘密、日志、本机数据和无关改动。
4. 更新 `CHANGELOG.md`。
5. 再提交并推送。未配置远程时必须向用户索要 GitHub 仓库地址，不得猜测。

## 跨电脑恢复开发上下文

1. 克隆仓库并检出目标分支。
2. 阅读本文件、README、CHANGELOG、架构文档及最近 10 条提交。
3. 安装 README 指定的 Unity/模块及依赖，修复 YooAsset 的可移植来源。
4. 用 Unity 打开工程，让 Packages 导入完成；不得复制旧电脑的 `Library/Temp/Logs/UserSettings`。
5. 运行配置校验、EditMode/PlayMode 测试和一次目标平台构建。
6. 用 `git status` 确认初始化没有产生意外的受控文件变更，再开始开发。
