# UnitySY 架构

## 总览

UnitySY 将稳定的启动/资源基础设施放在 AOT 程序集中，将可替换的博物馆业务放在 HybridCLR 热更程序集中；YooAsset 同时分发业务场景、资源、热更 DLL 与 AOT 元数据。

```text
Unity Player / Root 场景
  -> UpdateSceneBootstrap（流程编排、UI 边界）
     -> ServiceRegistry
     -> ResourceUpdateModule（状态机、并发/取消/失败）
        -> YooAssetRuntimeService（版本、Manifest、下载、校验、加载）
     -> HybridClrHotUpdateLoader
        -> Museum.HotUpdate / MuseumHotUpdateEntry
           -> FeatureRouter
           -> MuseumPointsController
              -> Point / Trigger / FunctionControl / Function
              -> 模型、动画、材质、视频及 MR 交互
```

## 模块关系

### AOT 层：`Assets/1_ScriptAOT`

- `Framework/Contracts`：AOT 与热更层共享的稳定契约。
- `Framework/Core`：`AppModuleHost`、`ServiceRegistry`、日志。
- `Framework/Modules`：资源更新状态机；通过接口隔离 YooAsset。
- `Runtime/HotUpdate/YooAsset`：YooAsset 初始化、远端/离线更新、资源和场景加载。
- `Runtime/HotUpdate/HybridCLR`：AOT 元数据与热更程序集装载。
- `UpdateSceneBootstrap` / `UpdateSceneView`：启动流程与界面适配边界。
- `RuntimeDebugDashboard`：运行状态诊断。

### 热更层：`Assets/2_ScriptHotUpdate`

- `MuseumHotUpdateEntry`：HybridCLR 业务入口。
- `FeatureRouter`：功能路由。
- `Museum/Point`：点位控制、触发策略、输入路由和动作执行。
- `MRTK`：相机、跟随和对象操作扩展。
- `SlateCinematicSequencer`：展陈演出序列控制。

### 内容与第三方

- `Assets/3_ResourceFile`：博物馆模型、UI、场景等业务内容。
- `Assets/MRTK`、`Assets/XR`：MR/OpenXR 配置与第三方实现。
- `Assets/ParadoxNotion`：SLATE 等第三方内容。
- `Assets/StreamingAssets/yoo`：随 Player 发布、由内容构建流程生成的 YooAsset 内置包；不纳入 Git。
- `Assets/Editor`：项目配置、HybridCLR/YooAsset 构建和发布工具。

## 关键业务流程

### 启动与资源更新

1. `Root` 场景中的 `UpdateSceneBootstrap` 创建服务注册表、YooAsset 服务和资源更新模块。
2. `ResourceUpdateModule` 初始化包并检查远端版本/Manifest；同一时刻只允许一个检查或下载操作。
3. 有更新时等待用户确认，下载并校验；网络失败时仅在本地存在已完整校验版本的前提下离线回退。
4. 加载配置中声明的 AOT 补充元数据和热更 DLL。
5. 创建 `MuseumHotUpdateEntry`，再进入热更业务场景/功能。
6. 生命周期结束时逆序关闭模块并释放句柄；跨场景启动对象可继续持有 YooAsset 包。

### 内容发布

1. 确认 Active Build Target 和 HybridCLR 配置。
2. 快速迭代仅编译热更 DLL；正式发布运行 HybridCLR `GenerateAll`。
3. 将热更 DLL 和裁剪后的 AOT DLL 复制为 YooAsset 内容（`.bytes`）。
4. 递增不可变的内容版本，构建 YooAsset 包到 `Bundles/`。
5. 人工核验后发布到 CDN；工具不得覆盖已有版本目录。

编辑器构建工具的路径必须与 `Assets/Scenes`、`Assets/2_ScriptHotUpdate` 和 `Assets/3_ResourceFile` 的当前布局保持一致；任何目录重组都要同步验证发布链路。

## Museum/N 模块化点位交互

Museum/N 是旧 Museum/Point 的并行替代实现，当前不自动迁移旧预制体。第一阶段只实现点位级距离触发，内部展品仍通过 MRTK 处理移入、移出和点击。

~~~text
DistancePointTrigger
  -> MuseumPointCoordinator
       -> 同一点位触发来源聚合
       -> 同组互斥/并行仲裁
       -> MuseumPoint
            -> PointRuntimeController
                 -> MuseumPointSelectionModule
                 -> PointVideoModule
                 -> MuseumArtifactGroupModule
                 -> MuseumOptionAction
~~~

- DistancePointTrigger 使用独立进入和退出距离形成滞回区间，避免边界抖动；由 Coordinator 统一轮询，点位数量增加时不需要每个触发器各自 Update。
- PointActivationRegistry 以“来源类型 + 来源实例”记录有效来源。同一点位未来同时被距离、碰撞和手势触发时，只有最后一个来源退出才关闭点位。
- Exclusive 点位按 interactionGroup 仲裁，默认选择来源优先级更高、点位优先级更高、距离更近的候选；切换时先退出旧点位。Parallel 点位可独立并行。
- PointRuntimeController 自动收集 IPointModule，按初始化顺序进入、逆序退出。视频停止、文物旋转停止、选中清空、按钮隐藏及扩展技能清理由统一 ExitPoint 触发。
- MuseumPointSelectionModule 用一条 MuseumOption 同时保存展品 View、VideoClip、文物组和扩展 Actions；选中状态优先于移入状态，因此选中物体移出后仍保持高亮和缩放。
- 碰撞、手势和手动点位触发尚未实现；后续适配器只调用 Coordinator 的 ReportEnter/ReportExit，不进入视频、UI 或文物业务模块。

具体预制体挂载方法见 Assets/2_ScriptHotUpdate/Museum/N/README.md。

## 外部服务和系统接口

- YooAsset：资源包、Manifest、缓存、下载器和场景加载接口。
- 内容 CDN：运行配置模板默认为 `http://127.0.0.1/CDN/{platform}/{appVersion}`；编辑器发布工具中还存在示例内网地址。正式地址应通过环境化配置提供，不得提交凭据。
- HybridCLR：通过 Gitee Git 包及 `HybridCLRData` 本地运行时/生成目录工作。
- MRTK/OpenXR：头显、手柄、手势、空间感知等 MR 输入与运行时接口。
- Unity `PlayerPrefs`：保存已完整校验的 YooAsset 包版本，仅用于离线回退标记，不应存储秘密或个人数据。
- 文件系统：YooAsset 使用 `StreamingAssets` 和 `persistentDataPath`；编辑器构建会写入 `Assets` 的 staging 路径、`HybridCLRData` 和 `Bundles`。

## 重要技术决策

1. **AOT/热更分层**：稳定契约与启动基础设施留在 AOT，业务行为进入热更层，以降低内容发布对 Player 重打包的依赖。
2. **接口隔离资源系统**：`ResourceUpdateModule` 只依赖 `IResourceUpdateBackend`，UI 只依赖 Bootstrap/快照事件，便于测试和替换实现。
3. **串行更新与可取消生命周期**：使用信号量与 CancellationToken 阻止重复点击造成并发更新，并在退出时统一取消。
4. **离线回退必须可证明完整**：仅记录并使用已加载 Manifest 且资源完整的版本，避免把半下载缓存当作可运行版本。
5. **内容版本不可变**：同一版本号始终对应同一份清单，发布工具拒绝覆盖旧目录。
6. **平台产物隔离**：HybridCLR 热更/AOT 元数据按 Active Build Target 生成，不跨平台复用。
7. **仓库是上下文真源**：规则、架构、构建方式、已知问题和设计决定写入版本控制，不依赖 Codex 对话。

## 待决策事项

- 选择可移植的 YooAsset 依赖方式（仓库内 package、固定 commit 的 Git URL 或私有 registry）。
- 统一 `Assets/Museum/...` 与现有 `Assets/Scenes`、`Assets/3_ResourceFile` 布局，并迁移编辑器常量与序列化引用。
- 明确 Player 启动场景、目标平台、输出目录和 CI 构建入口。
- 将现有 Museum/N 测试模式扩展到框架、资源更新和场景流程。
- 决定大型二进制资源、第三方源码和预构建 Bundles 的 Git LFS/发布制品策略。
- `HybridCLRData/` 作为可重建本机数据保持忽略；第三方来源由包配置和初始化步骤追踪。
