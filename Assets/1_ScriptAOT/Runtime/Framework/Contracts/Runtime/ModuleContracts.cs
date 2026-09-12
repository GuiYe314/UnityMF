using System;
using System.Threading;
using System.Threading.Tasks;

namespace AOT.HotUpdate.Framework
{
    /// <summary>
    /// APP 级功能模块的生命周期状态。
    /// 这是“下载模块、主界面模块、MR 模块”等大模块的状态，
    /// 不要与点位工作流中的 IActionModule（视频、展品、特效动作）混用。
    /// </summary>
    public enum ModuleState
    {
        /// <summary>
        /// 未初始化。模块还没有被创建，或者已经被销毁。
        /// </summary>
        NotInitialized,
        /// <summary>
        /// 正在初始化。模块已经被创建，但还没有完成 InitializeAsync。
        /// </summary>
        Initializing,
        /// <summary>
        ///     初始化完成。模块已经可以使用，但还没有开始 ShutdownAsync。
        /// </summary>
        Ready,
        /// <summary>
        ///       正在关闭。模块已经开始 ShutdownAsync，但还没有完成。
        /// </summary>
        ShuttingDown,
        /// <summary>
        ///      已经关闭。模块已经完成 ShutdownAsync，或者在初始化过程中失败。
        /// </summary>
        Shutdown,
        /// <summary>
        ///     初始化失败。模块在 InitializeAsync 中抛出异常，或者在初始化过程中被取消。
        /// </summary>
        Failed
    }

    /// <summary>
    /// APP 级模块统一生命周期。
    /// 每个可独立开发的功能模块都通过该接口接入主程序，
    /// 这样测试时可以单独创建模块，不需要启动完整 APP。
    /// </summary>
    public interface IAppModule
    {
        string ModuleId { get; }
        ModuleState State { get; }

        Task InitializeAsync(AppModuleContext context,CancellationToken cancellationToken);

        Task ShutdownAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// 最小服务容器接口。
    /// 模块只声明“需要哪个接口”，不直接在场景中查找具体实现，
    /// 因而正式实现和测试替身可以自由替换。
    /// </summary>
    public interface IServiceRegistry
    {
        void Register<TService>(TService service) where TService : class;
        TService Resolve<TService>() where TService : class;
        bool TryResolve<TService>(out TService service) where TService : class;
    }

    /// <summary>
    /// 日志抽象。正式运行时可以使用 Unity Debug，
    /// 单元测试时也可以替换成只记录消息的测试日志器。
    /// </summary>
    public interface IAppLogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }

    /// <summary>
    /// 传给每个 APP 模块的共享上下文。
    /// 后续加入 YooAsset、配置仓储、埋点等服务时，
    /// 都注册到 Services 中，不需要不断修改 IAppModule 方法签名。
    /// </summary>
    public sealed class AppModuleContext
    {
        public IServiceRegistry Services { get; }
        public IAppLogger Logger { get; }

        public AppModuleContext(
            IServiceRegistry services,
            IAppLogger logger)
        {
            Services = services
                ?? throw new ArgumentNullException(nameof(services));
            Logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }
    }
}
