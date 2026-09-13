# Changelog

本文件记录 UnitySY 的用户可见功能及重要工程变更。以后每次增加、修改或删除功能，都必须在同一提交中更新本文件。格式遵循 Keep a Changelog 的 Added / Changed / Fixed / Removed / Security 分类；尚未发布的修改写入 `Unreleased`。

## [Unreleased]

### Added

- 在 Assets/2_ScriptHotUpdate/Museum/N 增加第一版模块化点位交互框架：距离滞回触发、同点多来源聚合接口、相邻点位仲裁、同组互斥/并行运行模式及统一 Enter/Exit/Shutdown 生命周期。
- 增加展品 Normal/Hovered/Selected 状态、单选合集、共享视频播放器、视频/文物切换、文物旋转暂停恢复和可组合 MuseumOptionAction。
- 增加 Museum/N 的 EditMode 与 PlayMode 测试程序集，覆盖距离滞回、重复来源、多个来源延迟退出、相邻点位切换和运行时退出通知。
- 建立 `AGENTS.md`、`README.md` 和架构文档，作为跨电脑、跨 Codex 会话的长期开发上下文。
- 增加标准 Unity Git 忽略规则，隔离 Library、Temp、Logs、构建、本机 IDE 数据及生成的 `Assets/StreamingAssets/yoo/` 内容包。

### Fixed

- 将 `Assets/Scenes/Root.unity` 加入 Build Settings，避免 HybridCLR 临时 Player 构建因无场景而失败。
- 将 HybridCLR/YooAsset 编辑器工具的旧 `Assets/Museum/...` 路径和 `Museum.HotUpdate` 程序集名对齐到当前资产布局与 `HotUpdate` 程序集。
- 在 HybridCLR staging 与 YooAsset 构建之间强制完成资产导入，避免一键构建因导入尚未结束而失败。
- 移除构建工具拥有的旧 `MuseumStartup` 重复收集组，保留现有 AOT/HotUpdate、Points 和 Common 团队收集配置。

### Known issues

- YooAsset 包依赖使用 `D:/Software/...` 本机绝对路径。
- 除 Museum/N 新点位模块外，其他模块尚无项目自有自动化测试。
- Unity 生成的解决方案含重复 `Unity.Timeline` 项目名，MSBuild 无法加载。

## [0.1.0] - 2026-09-12

### Added

- 基于 MRTK/OpenXR 的博物馆混合现实项目基础结构。
- 点位、碰撞/距离触发、MRTK 输入路由及多种物体/视频交互动作。
- AOT 模块宿主、服务注册、日志和运行时调试面板。
- YooAsset 资源版本检查、下载、校验、离线回退、内容加载与场景切换流程。
- HybridCLR 热更 DLL 与 AOT 补充元数据加载流程，热更入口为 `Museum.HotUpdate`。
- 编辑器内 HybridCLR 配置校验、DLL 整理、YooAsset 构建及本地发布工具。
- 内置 `MuseumPackage` StreamingAssets 内容包。

[Unreleased]: https://github.com/OWNER/REPOSITORY/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/OWNER/REPOSITORY/releases/tag/v0.1.0

> 配置 GitHub 远程后，将以上占位链接替换为真实仓库地址。
