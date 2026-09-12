using System;
using System.Threading;
using System.Threading.Tasks;

namespace AOT.HotUpdate.Framework
{
    /// <summary>
    /// 可独立测试的资源更新模块。
    /// 它负责状态转换、并发保护、取消和错误处理；
    /// YooAsset 只作为 IResourceUpdateBackend 的一个实现存在。
    /// </summary>
    public sealed class ResourceUpdateModule : IResourceUpdateModule
    {
        private readonly SemaphoreSlim operationGate =
            new SemaphoreSlim(1, 1);

        private AppModuleContext context;
        private IResourceUpdateBackend backend;
        private CancellationTokenSource lifetime;

        public string ModuleId => "ResourceUpdate";
        public ModuleState State { get; private set; } =
            ModuleState.NotInitialized;

        public ResourceUpdateSnapshot Snapshot { get; private set; } =
            CreateSnapshot(
                ResourceUpdateStage.Idle,
                0f,
                0L,
                0L,
                string.Empty,
                "等待检查资源更新。",
                string.Empty);

        public ResourceUpdatePlan CurrentPlan { get; private set; }

        public event Action<ResourceUpdateSnapshot> SnapshotChanged;

        /// <summary>
        /// 初始化底层资源包，但不会自动下载。
        /// UpdateScene 可以在初始化完成后绑定 UI，再主动调用 CheckAsync。
        /// </summary>
        public async Task InitializeAsync(AppModuleContext moduleContext,CancellationToken cancellationToken)
        {
            if (moduleContext == null)
                throw new ArgumentNullException(nameof(moduleContext));
            if (State != ModuleState.NotInitialized)
            {
                throw new InvalidOperationException(
                    "ResourceUpdateModule can only be initialized once.");
            }

            context = moduleContext;
            State = ModuleState.Initializing;
            backend = context.Services.Resolve<IResourceUpdateBackend>();
            lifetime = new CancellationTokenSource();

            try
            {
                await backend.InitializeAsync(cancellationToken);
                State = ModuleState.Ready;
                Publish(
                    ResourceUpdateStage.Idle,
                    0f,
                    0L,
                    0L,
                    string.Empty,
                    "资源系统初始化完成，等待检查更新。",
                    string.Empty);
            }
            catch (Exception exception)
            {
                State = ModuleState.Failed;
                PublishFailure("资源系统初始化失败。", exception);
                throw;
            }
        }

        /// <summary>
        /// 检查远端版本和 Manifest，并生成是否需要下载的计划。
        /// 同一时间只允许一个检查或下载操作执行。
        /// </summary>
        public async Task<ResourceUpdatePlan> CheckAsync(
            CancellationToken cancellationToken)
        {
            EnsureOperational();

            var entered = false;
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(
                       cancellationToken,
                       lifetime.Token))
            {
                try
                {
                    await operationGate.WaitAsync(linked.Token);
                    entered = true;
                    EnsureOperational();

                    CurrentPlan = null;
                    Publish(
                        ResourceUpdateStage.Checking,
                        0f,
                        0L,
                        0L,
                        string.Empty,
                        "正在检查资源版本和更新清单……",
                        string.Empty);

                    var plan = await backend.CheckAsync(linked.Token);
                    CurrentPlan = plan
                        ?? throw new InvalidOperationException(
                            "Resource update backend returned a null plan.");

                    if (plan.RequiresDownload)
                    {
                        Publish(
                            ResourceUpdateStage.AwaitingDownload,
                            0f,
                            0L,
                            plan.DownloadBytes,
                            string.Empty,
                            "发现 " + plan.FileCount +
                            " 个文件需要下载。",
                            string.Empty);
                    }
                    else
                    {
                        Publish(
                            ResourceUpdateStage.Ready,
                            1f,
                            0L,
                            0L,
                            string.Empty,
                            // [OFFLINE FALLBACK] 明确告诉界面当前运行的是本地完整版本。
                            plan.IsOfflineFallback
                                ? "网络不可用，正在使用已校验的离线资源版本。"
                                : "当前资源已经是最新版本。",
                            string.Empty);
                    }

                    return plan;
                }
                catch (OperationCanceledException)
                {
                    PublishCancelled();
                    throw;
                }
                catch (Exception exception)
                {
                    PublishFailure("检查资源更新失败。", exception);
                    throw;
                }
                finally
                {
                    if (entered)
                        operationGate.Release();
                }
            }
        }

        /// <summary>
        /// 下载当前计划并执行校验。
        /// 下载失败后 CurrentPlan 会保留，因此界面可以再次调用本方法重试。
        /// </summary>
        public async Task DownloadAsync(
            CancellationToken cancellationToken)
        {
            EnsureOperational();

            var entered = false;
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(
                       cancellationToken,
                       lifetime.Token))
            {
                try
                {
                    await operationGate.WaitAsync(linked.Token);
                    entered = true;
                    EnsureOperational();

                    var plan = CurrentPlan;
                    if (plan == null)
                    {
                        throw new InvalidOperationException(
                            "CheckAsync must complete before DownloadAsync.");
                    }

                    if (!plan.RequiresDownload)
                    {
                        Publish(
                            ResourceUpdateStage.Ready,
                            1f,
                            0L,
                            0L,
                            string.Empty,
                            "没有需要下载的资源。",
                            string.Empty);
                        return;
                    }

                    Publish(
                        ResourceUpdateStage.Downloading,
                        0f,
                        0L,
                        plan.DownloadBytes,
                        string.Empty,
                        "开始下载资源。",
                        string.Empty);

                    var progress = new InlineProgress<ResourceDownloadProgress>(
                        value => PublishDownloadProgress(plan, value));

                    await backend.DownloadAsync(
                        plan,
                        progress,
                        linked.Token);

                    Publish(
                        ResourceUpdateStage.Verifying,
                        1f,
                        plan.DownloadBytes,
                        plan.DownloadBytes,
                        string.Empty,
                        "下载完成，正在校验资源。",
                        string.Empty);

                    await backend.VerifyAsync(plan, linked.Token);

                    Publish(
                        ResourceUpdateStage.Ready,
                        1f,
                        plan.DownloadBytes,
                        plan.DownloadBytes,
                        string.Empty,
                        "资源更新完成，可以进入主场景。",
                        string.Empty);
                }
                catch (OperationCanceledException)
                {
                    PublishCancelled();
                    throw;
                }
                catch (Exception exception)
                {
                    PublishFailure("下载或校验资源失败。", exception);
                    throw;
                }
                finally
                {
                    if (entered)
                        operationGate.Release();
                }
            }
        }

        /// <summary>
        /// 关闭时先取消正在执行的检查或下载，再释放底层资源包。
        /// </summary>
        public async Task ShutdownAsync(
            CancellationToken cancellationToken)
        {
            if (State == ModuleState.Shutdown)
                return;

            State = ModuleState.ShuttingDown;
            lifetime?.Cancel();

            var entered = false;
            try
            {
                await operationGate.WaitAsync(CancellationToken.None);
                entered = true;

                if (backend != null)
                    await backend.ShutdownAsync(cancellationToken);

                State = ModuleState.Shutdown;
            }
            catch
            {
                State = ModuleState.Failed;
                throw;
            }
            finally
            {
                if (entered)
                    operationGate.Release();

                lifetime?.Dispose();
                lifetime = null;
                backend = null;
            }
        }

        private void PublishDownloadProgress(
            ResourceUpdatePlan plan,
            ResourceDownloadProgress progress)
        {
            if (progress == null)
                return;

            var totalBytes = progress.TotalBytes > 0L
                ? progress.TotalBytes
                : plan.DownloadBytes;

            var normalized = totalBytes > 0L
                ? (float)Math.Min(progress.DownloadedBytes, totalBytes) /
                  totalBytes
                : 0f;

            Publish(
                ResourceUpdateStage.Downloading,
                normalized,
                progress.DownloadedBytes,
                totalBytes,
                progress.CurrentFile,
                "正在下载资源……",
                string.Empty);
        }

        private void PublishCancelled()
        {
            Publish(
                ResourceUpdateStage.Cancelled,
                Snapshot.Progress,
                Snapshot.DownloadedBytes,
                Snapshot.TotalBytes,
                Snapshot.CurrentFile,
                "资源更新已取消。",
                string.Empty);
        }

        private void PublishFailure(
            string message,
            Exception exception)
        {
            var error = exception == null
                ? "Unknown error."
                : exception.Message;

            context?.Logger.LogError(message + " " + error);

            Publish(
                ResourceUpdateStage.Failed,
                Snapshot.Progress,
                Snapshot.DownloadedBytes,
                Snapshot.TotalBytes,
                Snapshot.CurrentFile,
                message,
                error);
        }

        private void Publish(
            ResourceUpdateStage stage,
            float progress,
            long downloadedBytes,
            long totalBytes,
            string currentFile,
            string message,
            string errorMessage)
        {
            Snapshot = CreateSnapshot(
                stage,
                progress,
                downloadedBytes,
                totalBytes,
                currentFile,
                message,
                errorMessage);

            var handlers = SnapshotChanged;
            if (handlers == null)
                return;

            foreach (Action<ResourceUpdateSnapshot> handler
                     in handlers.GetInvocationList())
            {
                try
                {
                    handler(Snapshot);
                }
                catch (Exception exception)
                {
                    context?.Logger.LogWarning(
                        "Resource update UI listener failed: " +
                        exception.Message);
                }
            }
        }

        private void EnsureOperational()
        {
            if (State != ModuleState.Ready ||
                backend == null ||
                lifetime == null)
            {
                throw new InvalidOperationException(
                    "ResourceUpdateModule is not ready.");
            }
        }

        private static ResourceUpdateSnapshot CreateSnapshot(
            ResourceUpdateStage stage,
            float progress,
            long downloadedBytes,
            long totalBytes,
            string currentFile,
            string message,
            string errorMessage)
        {
            return new ResourceUpdateSnapshot(
                stage,
                progress,
                downloadedBytes,
                totalBytes,
                currentFile,
                message,
                errorMessage);
        }

        /// <summary>
        /// 同步转发进度，保证下载完成后不会再收到滞后的旧进度回调。
        /// 后端应按下载执行顺序调用 Report。
        /// </summary>
        private sealed class InlineProgress<T> : IProgress<T>
        {
            private readonly Action<T> callback;

            public InlineProgress(Action<T> callback)
            {
                this.callback = callback
                    ?? throw new ArgumentNullException(nameof(callback));
            }

            public void Report(T value)
            {
                callback(value);
            }
        }
    }
}
