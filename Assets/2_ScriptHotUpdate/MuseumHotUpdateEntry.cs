using AOT.HotUpdate.Experience;
using AOT.HotUpdate.Framework;
using HotUpdate.Experience;
using System.Threading;
using System.Threading.Tasks;

namespace HotUpdate
{
    /// <summary>
    /// HybridCLR 加载 DLL 后创建的第一个对象。
    /// 后续 MR 页面流程、点位业务编排可以逐步迁入该程序集，
    /// AOT 主程序仍然只通过 IHotUpdateEntry 与它通信。
    /// </summary>
    public sealed class MuseumHotUpdateEntry : IHotUpdateEntry
    {
        private IAppLogger logger;

        public Task InitializeAsync(
            IServiceRegistry services,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger = services.Resolve<IAppLogger>();

            FeatureRouter featureRouter = new FeatureRouter(services, logger);
            services.Register<IFeatureRouter>(featureRouter);
            // 到这里才代表热更新 DLL 已被真正执行，而不只是下载到了缓存。
            logger.Log("HotUpdate 已启动，场景路由注册完成。");

            return Task.CompletedTask;
        }

        public async Task ShutdownAsync(
            CancellationToken cancellationToken)
        {
            
        }
    }
}
