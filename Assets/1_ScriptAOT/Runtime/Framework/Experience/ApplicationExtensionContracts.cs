using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AOT.HotUpdate.Experience
{
    /// <summary>
    /// 业务层可选择的场景加载方式。
    /// 这里定义自己的枚举，而不把 UnityEngine.SceneManagement.LoadSceneMode
    /// 暴露给热更新业务，便于测试，也避免业务程序集直接绑定具体加载插件。
    /// </summary>
    public enum AppSceneLoadMode
    {
        Single = 0,
        Additive = 1
    }

    /// <summary>
    /// 场景资源加载的稳定边界。
    ///
    /// 热更新层决定“进入哪个场景”；AOT 中的 YooAsset 适配器只负责执行。
    /// 这样以后更换场景地址、首页路由或 MR 流程时，只更新热更 DLL/资源包，
    /// 不需要重新发布 Android APK 或 iOS IPA。
    /// </summary>
    public interface IAppSceneLoader
    {
        Task LoadSceneAsync(
            string sceneAddress,
            AppSceneLoadMode loadMode,
            CancellationToken cancellationToken);

        Task UnloadSceneAsync(
            string sceneAddress,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// APP 首页/功能入口与业务场景之间的稳定边界。
    /// </summary>
    public interface IFeatureRouter  {
        Task EnterFeatureAsync(CancellationToken cancellationToken);
        Task ExitCurrentFeatureAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// 常用功能 ID。字符串可以来自远端配置，同时用常量减少手写错误。
    /// </summary>
    public static class AppFeatureIds
    {
      
    }

    /// <summary>
    /// 真实 MR SDK（AR Foundation、厂商 SDK 等）的适配接口。
    /// 点位和工作流只依赖这个稳定合同；未来接入 SDK 时新增实现即可，
    /// 本阶段故意不提供假 MR 实现。
    /// </summary>
    public interface IMrPlatformAdapter
    {
        bool IsSupported { get; }
        bool IsRunning { get; }
        Transform Observer { get; }

        event Action<MrTrackingState> TrackingStateChanged;

        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }

    public enum MrTrackingState
    {
        Unavailable = 0,
        Initializing = 1,
        Tracking = 2,
        Limited = 3,
        Lost = 4
    }
}
