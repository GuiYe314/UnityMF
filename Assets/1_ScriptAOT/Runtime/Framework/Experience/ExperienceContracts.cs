using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AOT.HotUpdate.Experience
{
    /// <summary>
    /// 内容节点与工作流的数据入口。
    /// 调度器只依赖这个接口，不关心数据来自 ScriptableObject、SQLite 还是网络。
    /// 后续切换数据库时，新增 SQLitePointContentRepository 即可。
    /// </summary>
    public interface IPointContentRepository
    {
        Task InitializeAsync(CancellationToken cancellationToken);
        IReadOnlyList<InteractionNodeDefinition> GetPointsByZone(string zoneId);
        InteractionNodeDefinition GetPoint(string pointId);

        /// <summary>
        /// 根据点位、事件和当前状态选出应该执行的工作流。
        /// </summary>
        WorkflowDefinition ResolveWorkflow(
            string pointId,
            InteractionEventType eventType,
            InteractionState state);

        Task ShutdownAsync();
    }

    /// <summary>
    /// 加载内容内部模型/按钮对工作流系统的唯一调用入口。
    /// 空间点位触发由 PointStreamingManager 负责，不应直接调用这里。
    /// </summary>
    public interface IInteractionService
    {
        InteractionState State { get; }

        Task TriggerAsync(
            string nodeId,
            InteractionEventType eventType,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 一个可复用功能模块。
    /// PrepareAsync 负责提前加载资源，ExecuteAsync 负责真正改变场景状态，
    /// CancelAsync 用于用户快速切换点位时停止尚未完成的旧操作。
    /// </summary>
    public interface IActionModule
    {
        ExperienceModuleId ModuleId { get; }
        Task PrepareAsync(WorkflowActionDefinition action, CancellationToken cancellationToken);

        Task ExecuteAsync(
            WorkflowActionDefinition action,
            InteractionContext context,
            CancellationToken cancellationToken);

        Task CancelAsync();
    }

    /// <summary>
    /// 可选的模块生命周期接口。
    /// CancelAsync 只取消当前工作流，不应该删除已经稳定展示的内容；
    /// ShutdownAsync 在退出主场景时彻底释放实例、资源句柄和播放器引用。
    /// </summary>
    public interface IActionModuleLifetime
    {
        Task ShutdownAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// 用逻辑 TargetId 查找场景 GameObject。
    /// 数据中不保存场景对象引用，因此点位配置能够跨场景和版本复用。
    /// </summary>
    public interface ITargetRegistry
    {
        bool TryGet(string targetId, out GameObject target);
    }

    /// <summary>
    /// 统一资源加载接口。正式运行由 YooAssetRuntimeService 实现，
    /// 测试通过 FakeContentAssetProvider 实现。
    /// </summary>
    public interface IContentAssetProvider
    {
        Task<T> LoadAsync<T>(string address, CancellationToken cancellationToken)where T : Object;

        /// <summary>
        /// 启动时预加载一批地址。YooAsset 实现可以在这里保留资源句柄。
        /// </summary>
        Task PreloadAsync(IEnumerable<string> addresses, CancellationToken cancellationToken);

        void Release(string address);
    }


    /// <summary>
    /// 模块执行时需要的上下文。
    /// 模块之间不互相调用，而是通过共享状态与场景目标注册表完成协作。
    /// </summary>
    public sealed class InteractionContext
    {
        public string PointId { get; }
        public InteractionState State { get; }
        public ITargetRegistry Targets { get; }

        public InteractionContext(string pointId, InteractionState state, ITargetRegistry targets)
        {
            PointId = pointId;
            State = state;
            Targets = targets;
        }
    }
}
