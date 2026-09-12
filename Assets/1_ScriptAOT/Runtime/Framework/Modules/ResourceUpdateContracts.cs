
using System;
using System.Threading;
using System.Threading.Tasks;
namespace AOT.HotUpdate.Framework
{
    /// <summary>
    /// 资源更新界面关心的稳定状态。
    /// YooAsset 内部的包初始化、版本请求、Manifest 更新等细节，
    /// 由 IResourceUpdateBackend 封装，不直接泄漏到 UI。
    /// </summary>
    public enum ResourceUpdateStage
    {
        Idle,
        Checking,
        AwaitingDownload,
        Downloading,
        Verifying,
        Ready,
        Cancelled,
        Failed
    }

    /// <summary>
    /// 检查更新后得到的下载计划。
    /// 后续接 YooAsset 时，可由 ResourceDownloaderOperation 的统计信息转换而来。
    /// </summary>
    public sealed class ResourceUpdatePlan
    {
        public string PackageVersion { get; }
        public int FileCount { get; }
        public long DownloadBytes { get; }

        // [OFFLINE FALLBACK] UI 可据此显示“离线版本”，而不是误报“已是最新版”。
        public bool IsOfflineFallback { get; }

        public bool RequiresDownload =>
            FileCount > 0 || DownloadBytes > 0;

        public ResourceUpdatePlan(
            string packageVersion,
            int fileCount,
            long downloadBytes,
            bool isOfflineFallback = false)
        {
            PackageVersion = packageVersion ?? string.Empty;
            FileCount = Math.Max(0, fileCount);
            DownloadBytes = Math.Max(0L, downloadBytes);
            IsOfflineFallback = isOfflineFallback;
        }
    }

    /// <summary>
    /// 后端在下载过程中上报的原始进度。
    /// 上报顺序必须与 DownloadAsync 的执行顺序一致。
    /// </summary>
    public sealed class ResourceDownloadProgress
    {
        public long DownloadedBytes { get; }
        public long TotalBytes { get; }
        public string CurrentFile { get; }

        public ResourceDownloadProgress(
            long downloadedBytes,
            long totalBytes,
            string currentFile)
        {
            DownloadedBytes = Math.Max(0L, downloadedBytes);
            TotalBytes = Math.Max(0L, totalBytes);
            CurrentFile = currentFile ?? string.Empty;
        }
    }

    /// <summary>
    /// 提供给界面的只读快照。
    /// UI 只根据快照更新文本、进度条和按钮，不直接操作 YooAsset 句柄。
    /// </summary>
    public sealed class ResourceUpdateSnapshot
    {
        public ResourceUpdateStage Stage { get; }
        public float Progress { get; }
        public long DownloadedBytes { get; }
        public long TotalBytes { get; }
        public string CurrentFile { get; }
        public string Message { get; }
        public string ErrorMessage { get; }

        public ResourceUpdateSnapshot(
            ResourceUpdateStage stage,
            float progress,
            long downloadedBytes,
            long totalBytes,
            string currentFile,
            string message,
            string errorMessage)
        {
            Stage = stage;
            Progress = Math.Max(0f, Math.Min(1f, progress));
            DownloadedBytes = Math.Max(0L, downloadedBytes);
            TotalBytes = Math.Max(0L, totalBytes);
            CurrentFile = currentFile ?? string.Empty;
            Message = message ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }

    /// <summary>
    /// 资源更新底层接口。
    /// 当前测试使用 FakeResourceUpdateBackend；
    /// 下一阶段实现 YooAssetResourceUpdateBackend 即可接入真实更新。
    /// </summary>
    public interface IResourceUpdateBackend
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// YooAsset 实现可在这里请求包版本、更新 Manifest 并创建下载器。
        /// </summary>
        Task<ResourceUpdatePlan> CheckAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// 执行下载并持续上报进度。
        /// </summary>
        Task DownloadAsync(
            ResourceUpdatePlan plan,
            IProgress<ResourceDownloadProgress> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// 下载后校验或清理无用缓存。
        /// </summary>
        Task VerifyAsync(
            ResourceUpdatePlan plan,
            CancellationToken cancellationToken);

        Task ShutdownAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// 更新场景调用的 APP 模块接口。
    /// InitializeAsync 只初始化底层包系统，是否开始检查和下载由场景流程决定。
    /// </summary>
    public interface IResourceUpdateModule : IAppModule
    {
        ResourceUpdateSnapshot Snapshot { get; }
        ResourceUpdatePlan CurrentPlan { get; }

        event Action<ResourceUpdateSnapshot> SnapshotChanged;

        Task<ResourceUpdatePlan> CheckAsync(
            CancellationToken cancellationToken);

        Task DownloadAsync(CancellationToken cancellationToken);
    }
}