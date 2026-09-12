# Runtime Debug Dashboard 使用说明

## 位置

- 脚本：`Assets/1_ScriptAOT/Runtime/DebugDashboard/RuntimeDebugDashboard.cs`
- 预制体：`Assets/1_ScriptAOT/Prefabs/Debug/RuntimeDebugDashboard.prefab`

该模块位于 AOT 工程区域，不属于热更新程序集。

## 使用方法

把 `RuntimeDebugDashboard.prefab` 拖入最早启动的场景即可。默认启用 `Dont Destroy On Load`，切换场景后会继续存在；其它场景即使误放了重复实例，也会自动销毁后创建的副本。

发布包中的打开方式：

- PC：按 `F8`。
- PC/移动端：点击屏幕右上角的 `DBG` 按钮。
- 展开后点击“收起”或右上角“关闭”。

## 显示内容

运行信息页包含：

- FPS、运行时长、当前场景、Time Scale。
- 产品名、应用版本、Unity 版本、平台、语言。
- 网络可达状态。
- YooAsset 初始化状态、当前 Manifest 版本。
- 上次校验成功的离线版本。
- 设备型号、操作系统、CPU、GPU、系统内存和显存。
- 分辨率、DPI、Unity 内存、GC 内存和电量。

运行日志页包含：

- `Debug.Log`
- `Debug.LogWarning`
- `Debug.LogError`
- Exception 和 Assert
- 全部、警告、错误筛选
- 复制日志与清空按钮

“复制全部信息”会把设备信息、资源状态和当前筛选日志一起复制到系统剪贴板。

## Inspector 配置

- `Show On Start`：启动时直接展开。
- `Toggle Key`：PC 快捷键，默认 F8。
- `Dont Destroy On Load`：跨场景保留。
- `Package Name`：需要观察的 YooAsset 包名，默认 DefaultPackage。
- `Max Log Entries`：内存日志上限，默认 200。
- `Capture Normal Logs`：关闭后不保存普通 Log，只保留警告和错误。

## 发布注意事项

这个看板没有使用 `UNITY_EDITOR` 条件编译，因此普通 Release 包也会包含并显示。它会暴露设备、版本和错误日志，正式面向外部用户发布时建议采用隐藏入口，或在最终商店版本中移除预制体。
