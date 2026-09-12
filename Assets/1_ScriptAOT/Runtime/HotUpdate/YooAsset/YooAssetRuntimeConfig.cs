using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AOT.HotUpdate
{
    /// <summary>
    /// YooAsset 运行模式。
    /// EditorSimulate 用于编辑器开发，Offline 用于纯本地包，
    /// Host 用于 Android/iOS 从 CDN 检查并下载更新。
    /// </summary>
    public enum MuseumYooAssetPlayMode
    {
        EditorSimulate,
        Offline,
        Host
    }

    /// <summary>
    /// YooAsset 的项目级运行配置。
    ///
    /// 编辑器和真机使用不同的模式：
    /// - 编辑器默认 EditorSimulate，不需要先构建资源包，开发速度最快；
    /// - Android/iOS 默认 Host，启动后会检查远端清单并下载差异资源。
    ///
    /// URL 支持 {platform}、{appVersion} 两个占位符。
    /// </summary>
    [CreateAssetMenu(
        fileName = "YooAssetRuntimeConfig",
        menuName = "Museum/YooAsset/Runtime Config")]
    public sealed class YooAssetRuntimeConfig : ScriptableObject
    {
        // 两个字段分别只在 Editor/Player 条件分支中读取；
        // Unity 序列化仍会使用它们，因此关闭“已赋值但未使用”的编译提示。
#pragma warning disable 0414
        [SerializeField] private string packageName = "DefaultPackage";

        [Header("运行模式")]
        [Tooltip("Unity 编辑器内运行时使用。通常保持 EditorSimulate。")]
        [FormerlySerializedAs("playMode")]
        [SerializeField] private MuseumYooAssetPlayMode editorPlayMode =
            MuseumYooAssetPlayMode.EditorSimulate;

        [Tooltip("Android/iOS/PC 真正构建出的 App 使用。热更新项目通常选择 Host。")]
        [SerializeField] private MuseumYooAssetPlayMode playerPlayMode =
            MuseumYooAssetPlayMode.Host;

        [Header("版本与 CDN")]
        [Tooltip("同时作为 YooAsset 资源包版本；发布新内容时需要递增。")]
        [SerializeField] private string appVersion = "v1.0.1";

        [Tooltip("例如：http://127.0.0.1/CDN/{platform}/{appVersion}")]
        [SerializeField] private string primaryHostTemplate =
            "http://127.0.0.1/CDN/{platform}/{appVersion}";

        [SerializeField] private string fallbackHostTemplate =
            "http://127.0.0.1/CDN/{platform}/{appVersion}";

        [Tooltip("为空时自动使用 Android、IPhone 或 PC。")]
        [SerializeField] private string platformFolderOverride =
            string.Empty;

        [Header("首包")]
        [Tooltip(
            "开启后 Host 模式会先读取 StreamingAssets 内置清单；" +
            "断网时可用它启动安装包内的基础版本。")]
        // [OFFLINE FALLBACK] 真机默认携带最小可运行首包，避免首次启动无网时直接失败。
        [SerializeField] private bool useBuiltinPackage = true;

        [Header("离线降级")]
        [Tooltip("Host 模式连接 CDN 失败时，允许使用上次校验成功或首包内置的完整版本。")]
        // [OFFLINE FALLBACK] 只接受下载器统计为 0 的完整版本，不会运行下载到一半的缓存。
        [SerializeField] private bool allowOfflineFallback = true;

        [Tooltip(
            "首包 Manifest 的包版本。留空时使用 App Version；" +
            "如果 YooAsset 构建时使用了不同版本，请在这里明确填写。")]
        [SerializeField] private string builtinPackageVersion = string.Empty;

        [Header("下载")]
        [SerializeField] private int downloadMaxConcurrency = 8;
        [SerializeField] private int downloadRetryCount = 3;
        [SerializeField] private int manifestTimeoutSeconds = 60;
        [SerializeField] private bool clearUnusedCacheAfterDownload = true;
#pragma warning restore 0414

        public string PackageName => string.IsNullOrWhiteSpace(packageName)
            ? "DefaultPackage"
            : packageName;

        /// <summary>
        /// 编辑器始终读取 editorPlayMode；真机构建读取 playerPlayMode。
        /// 因此无需每次打包前手工切换配置，也避免把 EditorSimulate 带到手机。
        /// </summary>
        public MuseumYooAssetPlayMode PlayMode
        {
            get
            {
#if UNITY_EDITOR
                return editorPlayMode;               //return editorPlayMode;
#else
                return playerPlayMode;
#endif
            }
        }

        public string AppVersion => string.IsNullOrWhiteSpace(appVersion)
            ? "v1.0"
            : appVersion.Trim();

        public bool UseBuiltinPackage =>
            useBuiltinPackage;

        // [OFFLINE FALLBACK] 运行时降级开关与首包候选版本。
        public bool AllowOfflineFallback =>
            allowOfflineFallback;

        public string BuiltinPackageVersion =>
            string.IsNullOrWhiteSpace(builtinPackageVersion)
                ? AppVersion
                : builtinPackageVersion.Trim();

        public int DownloadMaxConcurrency =>
            Math.Max(1, downloadMaxConcurrency);

        public int DownloadRetryCount =>
            Math.Max(0, downloadRetryCount);

        public int ManifestTimeoutSeconds =>
            Math.Max(10, manifestTimeoutSeconds);

        public bool ClearUnusedCacheAfterDownload =>
            clearUnusedCacheAfterDownload;

        public string PrimaryHostUrl =>
            ResolveHostUrl(primaryHostTemplate);

        public string FallbackHostUrl =>
            ResolveHostUrl(fallbackHostTemplate);

        private string ResolveHostUrl(string template)
        {
            var safeTemplate = template ?? string.Empty;
            return safeTemplate
                .TrimEnd('/')
                .Replace("{platform}", ResolvePlatformFolder())
                .Replace("{appVersion}", AppVersion);
        }

        private string ResolvePlatformFolder()
        {
            if (!string.IsNullOrWhiteSpace(platformFolderOverride))
                return platformFolderOverride.Trim();

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "IPhone";
                default:
                    return "PC";
            }
        }
    }
}