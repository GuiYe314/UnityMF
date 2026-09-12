using AOT.HotUpdate.Experience;
using AOT.HotUpdate.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Object = UnityEngine.Object;

namespace AOT.HotUpdate
{
    /// <summary>
    /// YooAsset 的统一运行时适配器。
    /// 同一个实例既负责资源版本更新，也负责视频、模型、特效等资源加载，
    /// 从而确保所有模块共用同一个 ResourcePackage 和缓存。
    /// </summary>
    public sealed class YooAssetRuntimeService :
        IResourceUpdateBackend,
        IContentAssetProvider,
        IAppSceneLoader
    {
        private sealed class RetainedAsset
        {
            public AssetHandle Handle;
            public int ReferenceCount;
        }

        private readonly YooAssetRuntimeConfig config;

        private readonly Dictionary<string, RetainedAsset> retainedAssets =new Dictionary<string, RetainedAsset>(StringComparer.Ordinal);

        // 场景必须保留 YooAsset SceneHandle。Single 模式切换场景时，
        // YooAsset 会在旧场景卸载后自动释放旧句柄；这里仍保存当前有效句柄，
        // 以便热更新路由可以显式卸载 Additive 场景。
        private readonly Dictionary<string, SceneHandle> retainedScenes =
            new Dictionary<string, SceneHandle>(StringComparer.Ordinal);

        private ResourcePackage package;
        private ResourceDownloaderOperation downloader;
        private string currentDownloadFile = string.Empty;

        public ResourcePackage Package => package;

        public YooAssetRuntimeService(YooAssetRuntimeConfig config)
        {
            this.config = config
                ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 初始化 YooAssets 和默认资源包。
        /// EditorSimulate 不需要提前构建 AssetBundle；
        /// Android/iOS 正式包使用 Host 模式。
        /// </summary>
        public async Task InitializeAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!YooAssets.IsInitialized)
                YooAssets.Initialize();

            if (!YooAssets.TryGetPackage(
                    config.PackageName,
                    out package))
            {
                package =
                    YooAssets.CreatePackage(config.PackageName);
            }

            if (package.InitializeStatus ==
                EOperationStatus.Succeeded)
            {
                return;
            }

            InitializePackageOperation operation;
            switch (config.PlayMode)
            {
                case MuseumYooAssetPlayMode.EditorSimulate:
                    operation = CreateEditorSimulateOperation();
                    break;

                case MuseumYooAssetPlayMode.Offline:
                    operation = CreateOfflineOperation();
                    break;

                case MuseumYooAssetPlayMode.Host:
                    operation = CreateHostOperation();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            await operation;
            cancellationToken.ThrowIfCancellationRequested();

            ThrowIfFailed(
                operation.Status,
                operation.Error,
                "初始化 YooAsset 资源包失败");
        }

        /// <summary>
        /// 请求远端版本、加载 Manifest，并创建下载器。
        /// </summary>
        public async Task<ResourceUpdatePlan> CheckAsync(
            CancellationToken cancellationToken)
        {
            EnsurePackageReady();
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // 版本与 Manifest 都属于远端检查阶段，任一步骤断网都允许进入离线降级。
                var packageVersion =
                    await RequestPackageVersionWithRetryAsync(
                        cancellationToken);

                if (!string.Equals(
                        packageVersion,
                        config.AppVersion,
                        StringComparison.Ordinal))
                {
                    Debug.LogWarning(
                        $"[Museum.YooAsset] CDN directory version is '{config.AppVersion}', " +
                        $"but {config.PackageName}.version contains '{packageVersion}'. " +
                        "This is allowed by YooAsset, but usually means the wrong build output " +
                        "was copied into the version directory.");
                }

                return await CreatePlanForVersionAsync(
                    packageVersion,
                    false,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception remoteException)
            {
                // [OFFLINE FALLBACK] 只有 Host 模式并且明确开启开关时才降级。
                // EditorSimulate/Offline 的本地错误仍应直接暴露，避免掩盖构建问题。
                if (config.PlayMode != MuseumYooAssetPlayMode.Host ||
                    !config.AllowOfflineFallback)
                {
                    throw;
                }

                return await CreateOfflineFallbackPlanAsync(
                    remoteException,
                    cancellationToken);
            }
        }

        private async Task<string> RequestPackageVersionWithRetryAsync(
            CancellationToken cancellationToken)
        {
            var maximumAttempts = config.DownloadRetryCount + 1;
            var lastError = string.Empty;

            for (var attempt = 1; attempt <= maximumAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var operation = package.RequestPackageVersionAsync();
                await operation;
                cancellationToken.ThrowIfCancellationRequested();

                if (operation.Status == EOperationStatus.Succeeded)
                    return operation.PackageVersion;

                lastError = operation.Error;
                if (attempt >= maximumAttempts)
                    break;

                Debug.LogWarning(
                    $"[Museum.YooAsset] Package version request failed " +
                    $"({attempt}/{maximumAttempts}): {lastError}. Retrying...");
                await Task.Delay(500 * attempt, cancellationToken);
            }

            throw new InvalidOperationException(
                "请求 YooAsset 资源版本失败：" + lastError);
        }

        private async Task<ResourceUpdatePlan> CreatePlanForVersionAsync(
            string packageVersion,
            bool isOfflineFallback,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(packageVersion))
                throw new InvalidOperationException("YooAsset 包版本不能为空。");

            var manifestOptions =
                new LoadPackageManifestOptions(
                    packageVersion,
                    config.ManifestTimeoutSeconds);

            var manifestOperation =
                package.LoadPackageManifestAsync(
                    manifestOptions);

            await manifestOperation;
            cancellationToken.ThrowIfCancellationRequested();

            ThrowIfFailed(
                manifestOperation.Status,
                manifestOperation.Error,
                "加载 YooAsset Manifest 失败");

            var downloaderOptions =
                new ResourceDownloaderOptions(
                    config.DownloadMaxConcurrency,
                    config.DownloadRetryCount);

            downloader =
                package.CreateResourceDownloader(
                    downloaderOptions);

            // [OFFLINE FALLBACK] 离线时绝不尝试运行“只下载了一部分”的新版本。
            // 下载器仍发现缺失文件，说明缓存或首包不完整，必须继续寻找其它候选版本。
            if (isOfflineFallback &&
                (downloader.TotalDownloadCount > 0 ||
                 downloader.TotalDownloadBytes > 0))
            {
                var missingCount = downloader.TotalDownloadCount;
                var missingBytes = downloader.TotalDownloadBytes;
                downloader = null;
                throw new InvalidOperationException(
                    $"离线候选版本 '{packageVersion}' 不完整，" +
                    $"仍缺少 {missingCount} 个文件（{missingBytes} bytes）。");
            }

            var plan = new ResourceUpdatePlan(
                packageVersion,
                downloader.TotalDownloadCount,
                downloader.TotalDownloadBytes,
                isOfflineFallback);

            // [OFFLINE FALLBACK] 只有本地文件已经完整时才记录为下次可降级版本。
            if (!plan.RequiresDownload)
                RememberSuccessfulVersion(packageVersion);

            return plan;
        }

        private async Task<ResourceUpdatePlan> CreateOfflineFallbackPlanAsync(
            Exception remoteException,
            CancellationToken cancellationToken)
        {
            var candidateVersions = new List<string>();

            void AddCandidate(string version)
            {
                if (string.IsNullOrWhiteSpace(version))
                    return;

                version = version.Trim();
                if (!candidateVersions.Contains(version))
                    candidateVersions.Add(version);
            }

            // [OFFLINE FALLBACK] 优先上次完整版本，其次当前已加载清单，
            // 最后尝试配置的首包版本与 App 目录版本。
            AddCandidate(ReadSuccessfulVersion());

            try
            {
                AddCandidate(package.GetPackageVersion());
            }
            catch (Exception exception)
            {
                // [OFFLINE FALLBACK] 首次启动尚无活动 Manifest 时属于正常候选缺失。
                Debug.LogWarning(
                    "[Museum.YooAsset] No active package version is available: " +
                    exception.Message);
            }

            if (config.UseBuiltinPackage)
                AddCandidate(config.BuiltinPackageVersion);

            AddCandidate(config.AppVersion);

            var fallbackErrors = new List<string>();
            foreach (var candidateVersion in candidateVersions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var plan = await CreatePlanForVersionAsync(
                        candidateVersion,
                        true,
                        cancellationToken);

                    Debug.LogWarning(
                        $"[Museum.YooAsset] CDN unavailable. " +
                        $"Using verified offline package '{candidateVersion}'. " +
                        $"Remote error: {remoteException.Message}");
                    return plan;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception fallbackException)
                {
                    fallbackErrors.Add(
                        candidateVersion + ": " + fallbackException.Message);
                    Debug.LogWarning(
                        $"[Museum.YooAsset] Offline candidate " +
                        $"'{candidateVersion}' is unavailable: " +
                        fallbackException.Message);
                }
            }

            throw new InvalidOperationException(
                "网络不可用，并且没有找到完整的本地资源版本。" +
                "首次启动必须联网，或在构建 App 前把基础资源包复制到 StreamingAssets。" +
                (fallbackErrors.Count == 0
                    ? string.Empty
                    : " 候选检查：" + string.Join(" | ", fallbackErrors)),
                remoteException);
        }

        private string GetSuccessfulVersionKey()
        {
            return "Museum.YooAsset.LastVerifiedVersion." +
                   config.PackageName;
        }

        private string ReadSuccessfulVersion()
        {
            return PlayerPrefs.GetString(
                GetSuccessfulVersionKey(),
                string.Empty);
        }

        private void RememberSuccessfulVersion(string packageVersion)
        {
            if (string.IsNullOrWhiteSpace(packageVersion))
                return;

            // [OFFLINE FALLBACK] PlayerPrefs 只保存“清单已加载且资源完整”的版本号。
            PlayerPrefs.SetString(
                GetSuccessfulVersionKey(),
                packageVersion.Trim());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 启动 YooAsset 下载器，并把 3.0.5 的事件参数转换为框架统一进度。
        /// </summary>
        public async Task DownloadAsync(
            ResourceUpdatePlan plan,
            IProgress<ResourceDownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (downloader == null)
            {
                throw new InvalidOperationException(
                    "CheckAsync must create a downloader first.");
            }

            currentDownloadFile = string.Empty;

            void OnFileStarted(
                DownloadFileStartedEventArgs args)
            {
                currentDownloadFile = args.FileName;
            }

            void OnProgress(
                DownloadProgressChangedEventArgs args)
            {
                progress?.Report(
                    new ResourceDownloadProgress(
                        args.CurrentDownloadBytes,
                        args.TotalDownloadBytes,
                        currentDownloadFile));
            }

            downloader.DownloadFileStarted += OnFileStarted;
            downloader.DownloadProgressChanged += OnProgress;

            using (cancellationToken.Register(
                       () => downloader?.CancelDownload()))
            {
                try
                {
                    downloader.StartDownload();
                    await downloader;
                    cancellationToken.ThrowIfCancellationRequested();

                    ThrowIfFailed(
                        downloader.Status,
                        downloader.Error,
                        "YooAsset 资源下载失败");
                }
                finally
                {
                    downloader.DownloadFileStarted -=
                        OnFileStarted;
                    downloader.DownloadProgressChanged -=
                        OnProgress;
                }
            }
        }

        /// <summary>
        /// 下载完成后的缓存整理。
        /// YooAsset 自身已经校验下载文件，这里主要清理不再被 Manifest 使用的旧 Bundle。
        /// </summary>
        public async Task VerifyAsync(
            ResourceUpdatePlan plan,
            CancellationToken cancellationToken)
        {
            EnsurePackageReady();
            cancellationToken.ThrowIfCancellationRequested();

            if (config.ClearUnusedCacheAfterDownload)
            {
                var options =
                    new ClearCacheOptions(
                        ClearCacheMethods.ClearUnusedBundleFiles);

                var operation =
                    package.ClearCacheAsync(options);

                await operation;
                cancellationToken.ThrowIfCancellationRequested();

                ThrowIfFailed(
                    operation.Status,
                    operation.Error,
                    "清理 YooAsset 旧缓存失败");
            }

            // [OFFLINE FALLBACK] 下载与校验全部完成后才把新版本登记为可离线启动。
            RememberSuccessfulVersion(plan.PackageVersion);
        }

        /// <summary>
        /// 停止当前下载并释放本服务持有的资源句柄。
        /// 不调用 YooAssets.Destroy，因为主场景还要继续使用同一资源包。
        /// </summary>
        public Task ShutdownAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            downloader?.CancelDownload();
            downloader = null;

            foreach (var item in retainedAssets.Values)
                item.Handle?.Release();

            retainedAssets.Clear();
            retainedScenes.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 通过当前已经初始化并更新过 Manifest 的 YooAsset 包加载场景。
        /// 目标地址由热更新业务传入，本类不写死任何业务场景名称。
        /// </summary>
        public async Task LoadSceneAsync(
            string sceneAddress,
            AppSceneLoadMode loadMode,
            CancellationToken cancellationToken)
        {
            EnsurePackageReady();
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(sceneAddress))
            {
                throw new ArgumentException(
                    "YooAsset scene address cannot be empty.",
                    nameof(sceneAddress));
            }

            RemoveInvalidSceneHandles();
            if (retainedScenes.TryGetValue(
                    sceneAddress,
                    out var retained) &&
                retained.IsValid &&
                retained.SceneObject.IsValid() &&
                retained.SceneObject.isLoaded)
            {
                retained.ActivateScene();
                return;
            }

            var unityLoadMode = loadMode == AppSceneLoadMode.Additive
                ? LoadSceneMode.Additive
                : LoadSceneMode.Single;
            var handle = package.LoadSceneAsync(
                sceneAddress,
                unityLoadMode);

            try
            {
                await handle;

                ThrowIfFailed(
                    handle.Status,
                    handle.Error,
                    "加载 YooAsset 场景失败：" + sceneAddress);

                // Single 会让 Unity 卸载原来的普通场景/资源场景。
                // 被卸载的 YooAsset 场景句柄由 YooAsset 自动释放，
                // 所以这里只清除本地索引，不重复 Release。
                if (loadMode == AppSceneLoadMode.Single)
                    retainedScenes.Clear();

                retainedScenes[sceneAddress] = handle;
            }
            catch
            {
                if (handle.IsValid)
                    handle.Release();
                throw;
            }
        }

        /// <summary>
        /// 卸载之前由 YooAsset 加载的场景。
        /// UnloadSceneAsync 成功后 YooAsset 会自动释放 SceneHandle。
        /// </summary>
        public async Task UnloadSceneAsync(
            string sceneAddress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RemoveInvalidSceneHandles();

            if (!retainedScenes.TryGetValue(
                    sceneAddress ?? string.Empty,
                    out var handle))
            {
                return;
            }

            var operation = handle.UnloadSceneAsync();
            await operation;

            ThrowIfFailed(
                operation.Status,
                operation.Error,
                "卸载 YooAsset 场景失败：" + sceneAddress);

            retainedScenes.Remove(sceneAddress);
        }

        private void RemoveInvalidSceneHandles()
        {
            if (retainedScenes.Count == 0)
                return;

            var invalidAddresses = new List<string>();
            foreach (var pair in retainedScenes)
            {
                if (!pair.Value.IsValid)
                    invalidAddresses.Add(pair.Key);
            }

            foreach (var address in invalidAddresses)
                retainedScenes.Remove(address);
        }

        public async Task<T> LoadAsync<T>(
            string address,
            CancellationToken cancellationToken)
            where T : Object
        {
            EnsurePackageReady();

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(
                    "YooAsset address cannot be empty.",
                    nameof(address));
            }

            if (retainedAssets.TryGetValue(
                    address,
                    out var retained))
            {
                var cached = retained.Handle.GetAssetObject<T>();
                if (cached == null)
                {
                    throw new InvalidCastException(
                        "Cached asset type does not match: " +
                        address);
                }

                retained.ReferenceCount++;
                return cached;
            }

            var handle =
                package.LoadAssetAsync<T>(address);

            try
            {
                await handle;
                cancellationToken.ThrowIfCancellationRequested();

                ThrowIfFailed(
                    handle.Status,
                    handle.Error,
                    "加载 YooAsset 资源失败：" + address);

                var asset = handle.GetAssetObject<T>();
                if (asset == null)
                {
                    throw new InvalidCastException(
                        "Loaded asset type does not match: " +
                        address);
                }

                retainedAssets.Add(
                    address,
                    new RetainedAsset
                    {
                        Handle = handle,
                        ReferenceCount = 1
                    });

                return asset;
            }
            catch
            {
                handle.Release();
                throw;
            }
        }

        public async Task PreloadAsync(
            IEnumerable<string> addresses,
            CancellationToken cancellationToken)
        {
            if (addresses == null)
                return;

            var loadedInThisCall =
                new List<string>();

            try
            {
                foreach (var address in addresses)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(address))
                        continue;

                    await LoadAsync<Object>(
                        address,
                        cancellationToken);

                    loadedInThisCall.Add(address);
                }
            }
            catch
            {
                // 本批预加载失败时回滚本次新增的引用计数。
                foreach (var address in loadedInThisCall)
                    Release(address);

                throw;
            }
        }

        public void Release(string address)
        {
            if (!retainedAssets.TryGetValue(
                    address ?? string.Empty,
                    out var retained))
            {
                return;
            }

            retained.ReferenceCount--;
            if (retained.ReferenceCount > 0)
                return;

            retained.Handle.Release();
            retainedAssets.Remove(address);
        }

        private InitializePackageOperation
            CreateEditorSimulateOperation()
        {
#if UNITY_EDITOR
            var buildResult =
                EditorSimulateBuildInvoker.Build(
                    config.PackageName,
                    (int)EBundleType.VirtualAssetBundle);

            var options =
                new EditorSimulateModeOptions();

            options.EditorFileSystemParameters =
                FileSystemParameters
                    .CreateDefaultEditorFileSystemParameters(
                        buildResult.PackageRootDirectory);

            return package.InitializePackageAsync(options);
#else
            throw new PlatformNotSupportedException(
                "EditorSimulate mode is only available in Unity Editor.");
#endif
        }

        private InitializePackageOperation
            CreateOfflineOperation()
        {
            var options = new OfflinePlayModeOptions();

            options.BuiltinFileSystemParameters =
                FileSystemParameters
                    .CreateDefaultBuiltinFileSystemParameters();

            return package.InitializePackageAsync(options);
        }

        private InitializePackageOperation CreateHostOperation()
        {
            Debug.Log(
                $"[Museum.YooAsset] Host mode. Primary={config.PrimaryHostUrl}, " +
                $"Fallback={config.FallbackHostUrl}, AppVersion={config.AppVersion}");

            var remoteService = new RemoteService(
                config.PrimaryHostUrl,
                config.FallbackHostUrl);

            var options = new HostPlayModeOptions();

            if (config.UseBuiltinPackage)
            {
                // 有首包时先从 StreamingAssets 复制内置清单和资源，
                // 然后再由缓存文件系统从 CDN 获取差异内容。
                options.BuiltinFileSystemParameters =
                    FileSystemParameters
                        .CreateDefaultBuiltinFileSystemParameters();

                options.BuiltinFileSystemParameters.AddParameter(
                    EFileSystemParameter.CopyBuiltinPackageManifest,
                    true);
            }
            else
            {
                // 纯远端模式不创建 BuiltinFileSystem。
                // 否则 YooAsset 会先访问不存在的 StreamingAssets 清单，
                // 在真正请求 CDN 之前就以 404 结束初始化。
                options.BuiltinFileSystemParameters = null;
            }

            options.CacheFileSystemParameters =
                FileSystemParameters
                    .CreateDefaultSandboxFileSystemParameters(
                        remoteService);

            options.CacheFileSystemParameters.AddParameter(
                EFileSystemParameter.DownloadMaxConcurrency,
                config.DownloadMaxConcurrency);

            return package.InitializePackageAsync(options);
        }

        private void EnsurePackageReady()
        {
            if (package == null ||
                package.InitializeStatus !=
                EOperationStatus.Succeeded)
            {
                throw new InvalidOperationException(
                    "YooAsset package is not initialized.");
            }
        }

        private static void ThrowIfFailed(
            EOperationStatus status,
            string error,
            string message)
        {
            if (status == EOperationStatus.Succeeded)
                return;

            throw new InvalidOperationException(
                message + "：" + error);
        }

        /// <summary>
        /// YooAsset 3.x 远端文件地址提供器，支持主 CDN 和备用 CDN。
        /// </summary>
        private sealed class RemoteService : IRemoteService
        {
            private readonly string primaryUrl;
            private readonly string fallbackUrl;

            public RemoteService(
                string primaryUrl,
                string fallbackUrl)
            {
                this.primaryUrl =
                    (primaryUrl ?? string.Empty).TrimEnd('/');
                this.fallbackUrl =
                    (fallbackUrl ?? string.Empty).TrimEnd('/');
            }

            public IReadOnlyList<string> GetRemoteUrls(
                string fileName)
            {
                var primary = primaryUrl + "/" + fileName;
                var fallback = fallbackUrl + "/" + fileName;
                if (string.Equals(primary, fallback, StringComparison.Ordinal))
                    return new[] { primary };

                return new[]
                {
                    primary,
                    fallback
                };
            }
        }
    }
}
