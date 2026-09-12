using AOT.HotUpdate.Experience;
using AOT.HotUpdate.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AOT.HotUpdate
{
    /// <summary>
    /// “资源更新场景”的组合入口。
    ///
    /// 该组件只在 APP 启动阶段存在，职责是按顺序组装：
    /// ServiceRegistry -> YooAssetRuntimeService ->
    /// ResourceUpdateModule -> HybridClrHotUpdateLoader。
    ///
    /// UI 不直接调用 YooAsset 或 HybridCLR，只调用本组件公开的方法并监听事件。
    /// 因此下一步替换界面、添加进度条或重试弹窗时，不需要修改底层更新代码。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpdateSceneBootstrap : MonoBehaviour
    {
        [Header("基础配置")]
        [SerializeField] private YooAssetRuntimeConfig yooAssetConfig;
        [SerializeField] private HotUpdateLoadSettings hotUpdateSettings;

        [Header("启动行为")]
        [Tooltip("启动后自动初始化 YooAsset 并检查版本。")]
        [SerializeField] private bool checkOnStart = true;

        [Tooltip("跨场景保留本对象，使主场景继续复用同一个 YooAsset 包和资源缓存。")]
        [SerializeField] private bool persistAcrossScenes = true;

        [Tooltip("资源更新 Ready 后实际加载该地址，用于验证 YooAsset 寻址和 Manifest。留空可禁用。")]
        [SerializeField] private string startupProbeAddress ="UpdateProbe";

        // 防止“检查、下载、重试”被按钮连续点击后并发执行。
        private readonly SemaphoreSlim flowGate =
            new SemaphoreSlim(1, 1);

        private CancellationTokenSource lifetime;
        private ServiceRegistry services;
        private UnityAppLogger logger;
        private YooAssetRuntimeService yooAssetService;
        private ResourceUpdateModule updateModule;
        private HybridClrHotUpdateLoader hotUpdateLoader;
        private bool initialized;
        private bool hotUpdateStarted;
        private bool shuttingDown;

        /// <summary>
        /// 当前更新快照。界面可读取 Stage、Progress、Message 和 ErrorMessage。
        /// </summary>
        public ResourceUpdateSnapshot Snapshot => updateModule?.Snapshot;

        public  IServiceRegistry Services => services;

        public string CurrentPackageVersion =>
            updateModule?.CurrentPlan?.PackageVersion ??
            string.Empty;

        public bool IsHotUpdateStarted =>
            hotUpdateStarted;

        /// <summary>
        /// 供编辑器场景生成器或自动化测试注入配置。
        /// 普通项目也可以直接在 Inspector 中拖拽两个配置资源。
        /// </summary>
        public void Configure(
            YooAssetRuntimeConfig resourceConfig,
            HotUpdateLoadSettings hotSettings,
            string probeAddress = "UpdateProbe")
        {
            yooAssetConfig = resourceConfig;
            hotUpdateSettings = hotSettings;
            startupProbeAddress =
                probeAddress ?? string.Empty;
        }

        /// <summary>
        /// 更新状态变化事件。UI 只需要订阅它刷新文字、进度条和按钮。
        /// </summary>
        public event Action<ResourceUpdateSnapshot> SnapshotChanged;

        /// <summary>
        /// 热更新入口完成初始化后触发。
        /// 下一步可以在这里加载主场景或打开主界面。
        /// </summary>
        public event Action HotUpdateStarted;

        /// <summary>
        /// 统一失败事件。错误同时也会写入 Snapshot 和 Unity Console。
        /// </summary>
        public event Action<string> FlowFailed;

        private async void Start()
        {
            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);

            if (!checkOnStart)
                return;

            await RunUiCommandAsync(InitializeAndCheckAsync);
        }

        /// <summary>
        /// 初始化 YooAsset 并检查远端版本。
        /// 若不需要下载，会直接启动 HybridCLR 热更新入口；
        /// 若需要下载，会停在 AwaitingDownload，等待界面让用户确认。
        /// </summary>
        public async Task InitializeAndCheckAsync()
        {
            await flowGate.WaitAsync();

            try
            {
                EnsureNotShuttingDown();

                if (!initialized)
                {
                    CreateRuntimeServices();

                    await updateModule.InitializeAsync(new AppModuleContext(services, logger), lifetime.Token);

                    initialized = true;
                }

                var plan = await updateModule.CheckAsync(
                    lifetime.Token);

                if (!plan.RequiresDownload)
                    await StartHotUpdateEntryAsync();
            }
            finally
            {
                flowGate.Release();
            }
        }

        /// <summary>
        /// 供“确认下载”按钮调用。
        /// 下载、缓存整理完成后再启动热更新入口。
        /// </summary>
        public async void ConfirmDownloadAndEnter()
        {
            await RunUiCommandAsync(DownloadAndEnterAsync);
        }

        public async Task DownloadAndEnterAsync()
        {
            await flowGate.WaitAsync();

            try
            {
                EnsureInitialized();
                EnsureNotShuttingDown();

                await updateModule.DownloadAsync(
                    lifetime.Token);

                await StartHotUpdateEntryAsync();
            }
            finally
            {
                flowGate.Release();
            }
        }

        /// <summary>
        /// 供“重试”按钮调用。
        /// 已有下载计划时重试下载；没有计划时重新检查版本。
        /// </summary>
        public async void Retry()
        {
            await RunUiCommandAsync(RetryInternalAsync);
        }

        private async Task RetryInternalAsync()
        {
            if (updateModule != null &&
                updateModule.CurrentPlan != null &&
                updateModule.CurrentPlan.RequiresDownload)
            {
                await DownloadAndEnterAsync();
                return;
            }

            await InitializeAndCheckAsync();
        }

        private void CreateRuntimeServices()
        {
            if (yooAssetConfig == null)
            {
                throw new InvalidOperationException(
                    "UpdateSceneBootstrap 未指定 YooAssetRuntimeConfig。");
            }

            if (hotUpdateSettings == null)
            {
                throw new InvalidOperationException(
                    "UpdateSceneBootstrap 未指定 HotUpdateLoadSettings。");
            }

            lifetime = new CancellationTokenSource();
            services = new ServiceRegistry();
            logger = new UnityAppLogger();
            yooAssetService =
                new YooAssetRuntimeService(yooAssetConfig);
            updateModule = new ResourceUpdateModule();
            hotUpdateLoader =
                new HybridClrHotUpdateLoader(yooAssetService, logger);

            // 同一个 YooAssetRuntimeService 同时实现“更新后端”和“资源提供器”。
            // 这样下载场景与主场景共享同一个包、句柄表和磁盘缓存。
            services.Register<IAppLogger>(logger);
            services.Register<IResourceUpdateBackend>(yooAssetService);
            services.Register<IContentAssetProvider>(yooAssetService);
            services.Register<IAppSceneLoader>(yooAssetService);
            services.Register<IResourceUpdateModule>(updateModule);

            updateModule.SnapshotChanged += ForwardSnapshot;
        }

        private async Task StartHotUpdateEntryAsync()
        {
            if (hotUpdateStarted)
                return;

            if (updateModule.Snapshot.Stage !=
                ResourceUpdateStage.Ready)
            {
                throw new InvalidOperationException(
                    "资源更新尚未进入 Ready，不能启动热更新入口。");
            }

            await VerifyStartupAssetAsync();

            await hotUpdateLoader.LoadAndStartAsync(
                hotUpdateSettings,
                services,
                lifetime.Token);

            hotUpdateStarted = true;
            HotUpdateStarted?.Invoke();
        }

        /// <summary>
        /// 更新界面的“进入 MR”按钮只提交功能 ID。
        /// 场景地址和加载方式均由 HotUpdate 中的 IAppFeatureRouter 实现决定，
        /// 因此 AOT 启动层不再依赖 ExperienceDemo 等业务场景名称。
        /// </summary>
        public async void EnterMrFeature()
        {
            await RunUiCommandAsync(
                () => EnterFeatureAsync());
        }

        private async Task EnterFeatureAsync()
        {
            if (!hotUpdateStarted)
            {
                throw new InvalidOperationException(
                    "热更新入口尚未启动，不能进入业务功能。");
            }

            var router = services.Resolve<IFeatureRouter>();
            await router.EnterFeatureAsync(
                lifetime.Token);
        }

        /// <summary>
        /// Ready 后通过与业务资源相同的 IContentAssetProvider 加载探针。
        /// 这一步能发现“包初始化成功但地址或收集器配置错误”的问题。
        /// </summary>
        private async Task VerifyStartupAssetAsync()
        {
            if (string.IsNullOrWhiteSpace(
                    startupProbeAddress))
            {
                return;
            }

            var probe = await yooAssetService.LoadAsync<TextAsset>(
                startupProbeAddress,
                lifetime.Token);

            try
            {
                logger.Log(
                    "[Museum.Update] YooAsset probe loaded: " +
                    probe.text);
            }
            finally
            {
                yooAssetService.Release(
                    startupProbeAddress);
            }
        }

        private void ForwardSnapshot(
            ResourceUpdateSnapshot snapshot)
        {
            SnapshotChanged?.Invoke(snapshot);
        }

        /// <summary>
        /// Unity Button 无法直接等待 Task，因此公开的按钮方法使用 async void。
        /// 所有异常都在此处收口，避免成为未观察异常。
        /// </summary>
        private async Task RunUiCommandAsync( Func<Task> command)
        {
            try
            {
                await command();
            }
            catch (OperationCanceledException)
            {
                // 场景卸载或应用退出属于正常取消，不显示错误弹窗。
            }
            catch (Exception exception)
            {
                logger?.LogError(
                    "[Museum.Update] " + exception);
                FlowFailed?.Invoke(exception.Message);
            }
        }

        private void EnsureInitialized()
        {
            if (!initialized || updateModule == null)
            {
                throw new InvalidOperationException(
                    "请先执行 InitializeAndCheckAsync。");
            }
        }

        private void EnsureNotShuttingDown()
        {
            if (shuttingDown)
            {
                throw new ObjectDisposedException( nameof(UpdateSceneBootstrap));
            }
        }

        private async void OnDestroy()
        {
            if (shuttingDown)
                return;

            shuttingDown = true;
            lifetime?.Cancel();

            try
            {
                await flowGate.WaitAsync();

                if (hotUpdateLoader != null)
                {
                    await hotUpdateLoader.ShutdownAsync(
                        CancellationToken.None);
                }

                if (updateModule != null)
                {
                    updateModule.SnapshotChanged -=
                        ForwardSnapshot;

                    await updateModule.ShutdownAsync(
                        CancellationToken.None);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                flowGate.Release();
                lifetime?.Dispose();
                lifetime = null;
                flowGate.Dispose();
            }
        }
    }
}
