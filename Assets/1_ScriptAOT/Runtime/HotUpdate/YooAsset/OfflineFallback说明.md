# YooAsset 断网降级修改说明

## 一、修改目的

原来的 Host 模式必须先成功连接 CDN，请求资源包版本并加载远端 Manifest。网络不可用时，检查流程直接进入 Failed，设备中即使已经存在完整缓存也无法进入热更新入口。

本次修改增加以下启动策略：

1. 优先连接 CDN，并继续使用原来的重试机制。
2. 版本文件或 Manifest 请求失败时，尝试上次校验成功的本地版本。
3. 没有历史版本时，尝试 StreamingAssets 中的首包版本。
4. 离线候选版本只有在所有文件都完整存在时才能启动。
5. 所有本地版本都不可用时，明确提示首次启动需要联网或提供首包。

新增代码均使用以下标记，方便全局搜索：

```csharp
// [OFFLINE FALLBACK]
```

## 二、涉及文件

### YooAssetRuntimeConfig.cs

路径：

```text
Assets/1_ScriptAOT/Runtime/HotUpdate/YooAsset/YooAssetRuntimeConfig.cs
```

修改内容：

- `useBuiltinPackage` 的代码默认值改为 `true`。
- 新增 `allowOfflineFallback`，控制 Host 模式是否允许断网降级。
- 新增 `builtinPackageVersion`，用于指定首包 Manifest 的实际包版本。
- 新增只读属性 `AllowOfflineFallback` 和 `BuiltinPackageVersion`。
- `builtinPackageVersion` 留空时自动使用 `AppVersion`。

当前配置资产：

```text
Use Builtin Package    = true
Allow Offline Fallback = true
Builtin Package Version = 空
App Version             = v1.0.3
```

因此当前首包候选版本会使用 `v1.0.3`。

### YooAssetRuntimeService.cs

路径：

```text
Assets/1_ScriptAOT/Runtime/HotUpdate/YooAsset/YooAssetRuntimeService.cs
```

主要修改：

#### CheckAsync

现在将“请求版本号”和“加载 Manifest”都视为远端检查阶段。

只要其中任意一步因网络或远端服务失败，并且满足以下条件，就会进入离线降级：

```text
PlayMode == Host
AllowOfflineFallback == true
```

取消操作 `OperationCanceledException` 不会被当成断网，不会触发降级。

#### CreatePlanForVersionAsync

统一负责：

- 加载指定版本 Manifest。
- 创建 YooAsset 下载器。
- 统计缺失文件。
- 生成 `ResourceUpdatePlan`。

离线模式下，如果下载器发现任何缺失文件或缺失字节，会拒绝该候选版本。这样不会运行下载到一半的新版本。

#### CreateOfflineFallbackPlanAsync

候选版本的尝试顺序：

1. `PlayerPrefs` 中记录的上次完整版本。
2. YooAsset 当前已经加载的包版本。
3. 配置中的 `BuiltinPackageVersion`。
4. 配置中的 `AppVersion`。

重复版本会自动去除。每个候选失败后会继续尝试下一个，并在 Console 输出原因。

#### RememberSuccessfulVersion

完整版本保存到：

```text
Museum.YooAsset.LastVerifiedVersion.{PackageName}
```

只有以下两种情况会记录：

- 检查完成后下载量为 0，说明本地已经完整。
- 下载及校验全部成功。

不会把待下载或下载失败的版本记录为离线可用版本。

#### VerifyAsync

资源下载、YooAsset 校验和可选缓存清理完成后，才登记新的离线可用版本。

### ResourceUpdateContracts.cs

路径：

```text
Assets/1_ScriptAOT/Runtime/Framework/Modules/ResourceUpdateContracts.cs
```

`ResourceUpdatePlan` 新增：

```csharp
public bool IsOfflineFallback { get; }
```

构造函数增加可选参数：

```csharp
bool isOfflineFallback = false
```

保留默认值是为了兼容项目中原有的构造调用和测试代码。

### ResourceUpdateModule.cs

路径：

```text
Assets/1_ScriptAOT/Runtime/Framework/Modules/ResourceUpdateModule.cs
```

当离线版本准备完成时，状态仍然是 `ResourceUpdateStage.Ready`，所以原有启动场景能够继续进入热更新入口。

界面提示改为：

```text
网络不可用，正在使用已校验的离线资源版本。
```

在线且无需更新时，仍显示原来的“当前资源已经是最新版本”。

### YooAssetRuntimeConfig.asset

路径：

```text
Assets/Scenes/YooAssetRuntimeConfig.asset
```

已把以下两个选项打开：

- `useBuiltinPackage = true`
- `allowOfflineFallback = true`

## 三、启动流程

```text
启动 YooAsset Host 模式
        |
        v
请求 CDN 版本号并加载 Manifest
        |
        +-- 成功 --> 创建下载计划 --> 下载或直接进入
        |
        +-- 失败 --> 读取上次完整版本
                         |
                         +-- 完整 --> 离线进入
                         |
                         +-- 不完整/不存在 --> 尝试当前包版本
                                                   |
                                                   +-- 完整 --> 离线进入
                                                   |
                                                   +-- 失败 --> 尝试首包版本
                                                                    |
                                                                    +-- 完整 --> 离线进入
                                                                    |
                                                                    +-- 失败 --> 提示无法启动
```

## 四、完整性保护

降级不是简单忽略网络异常。

加载候选 Manifest 后仍会调用 `CreateResourceDownloader`。只有满足下面条件时才认为该版本完整：

```text
TotalDownloadCount == 0
TotalDownloadBytes == 0
```

否则该版本会被拒绝，防止出现以下问题：

- 热更新 DLL 只下载了一部分。
- AOT 元数据缺失。
- 入口场景或基础资源缺失。
- Manifest 已切换但 Bundle 尚未全部下载。

## 五、首包构建要求

当前检查时，项目的 `Assets/StreamingAssets` 目录中没有 YooAsset 首包文件。

因为配置已经打开 `Use Builtin Package`，正式构建 App 前必须通过 YooAsset 构建流程生成 `v1.0.3` 基础包，并把内置 Manifest、热更新 DLL、AOT 元数据、入口场景和必要资源复制到 StreamingAssets。

如果 YooAsset 构建时的包版本不是 `v1.0.3`，必须在配置资产的 `Builtin Package Version` 中填写真实版本号。

首包至少应包含：

- 热更新入口程序集。
- HybridCLR 需要的 AOT 元数据。
- 启动后的第一个业务场景。
- 业务场景启动所需的基础 Prefab、材质和配置。
- 无网络时必须展示的 UI 和错误提示资源。

没有首包并且设备从未成功下载过资源时，首次断网启动不可能进入热更新业务。

## 六、运行情况

| 场景 | 结果 |
| --- | --- |
| 网络正常，远端没有更新 | 使用当前完整版本进入 |
| 网络正常，远端存在更新 | 显示下载计划并正常下载 |
| 网络断开，设备存在完整缓存 | 使用上次校验成功版本进入 |
| 网络断开，无缓存但首包完整 | 使用首包版本进入 |
| 网络断开，缓存下载不完整 | 拒绝不完整缓存，继续尝试首包 |
| 网络断开，无缓存且无首包 | 更新流程进入 Failed 并显示明确原因 |
| 用户主动取消 | 保持取消流程，不会误判为断网 |

## 七、当前限制

本次降级覆盖启动检查阶段，包括版本号请求失败和 Manifest 加载失败。

如果已经生成下载计划，随后在下载过程中断网，当前行为仍然是下载失败并保留原下载计划，由界面的重试按钮继续下载。暂未在下载中途自动切回旧版本，避免在用户已经确认更新后静默切换 Manifest。

## 八、建议测试

发布前至少执行以下真机测试：

1. 全新安装、开启网络，确认首包和远端更新都可正常启动。
2. 全新安装、关闭网络，确认可以从 StreamingAssets 首包启动。
3. 成功更新一次后关闭网络，确认从缓存版本启动。
4. 下载中途断网，确认显示下载失败并可以重试。
5. 人为删除部分缓存后断网，确认不会运行不完整版本。
6. 将 `Builtin Package Version` 填错，确认能够得到明确错误。
7. 恢复网络后重新启动，确认仍会优先检查远端最新版。

## 九、日志关键字

可在 Unity Console 或设备日志中搜索：

```text
[Museum.YooAsset]
CDN unavailable
Using verified offline package
Offline candidate
No active package version is available
```

代码定位可全局搜索：

```text
OFFLINE FALLBACK
```
