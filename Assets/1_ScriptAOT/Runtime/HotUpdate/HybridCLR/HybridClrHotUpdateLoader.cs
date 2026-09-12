using AOT.HotUpdate.Experience;
using AOT.HotUpdate.Framework;
using HybridCLR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AOT.HotUpdate
{
    /// <summary>
    /// AOT 主程序中的 HybridCLR 加载器。
    /// Editor 下程序集已经被 Unity 加载，因此直接查找；
    /// Android/iOS IL2CPP 包中则从 YooAsset 读取 DLL 字节并 Assembly.Load。
    /// </summary>
    public sealed class HybridClrHotUpdateLoader
    {
        private readonly IContentAssetProvider assets;
        private readonly IAppLogger logger;
        private readonly List<Assembly> loadedAssemblies =
            new List<Assembly>();

        public IHotUpdateEntry LoadedEntry { get; private set; }

        /// <summary>
        /// 本次按配置顺序确认或加载成功的热更新程序集。
        /// Shutdown 只能关闭业务入口，CLR/IL2CPP 运行时不支持卸载 Assembly；
        /// 清空本列表仅表示该加载器不再持有本次会话状态。
        /// </summary>
        public IReadOnlyList<Assembly> LoadedAssemblies =>
            loadedAssemblies;

        public HybridClrHotUpdateLoader(
            IContentAssetProvider assets,
            IAppLogger logger)
        {
            this.assets = assets
                ?? throw new ArgumentNullException(nameof(assets));
            this.logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IHotUpdateEntry> LoadAndStartAsync(
            HotUpdateLoadSettings settings,
            IServiceRegistry services,
            CancellationToken cancellationToken)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (LoadedEntry != null)
            {
                throw new InvalidOperationException(
                    "Hot update entry has already been loaded.");
            }

            var assemblyItems = ValidateAssemblyItems(settings);
            IReadOnlyList<Assembly> sessionAssemblies;

#if UNITY_EDITOR
            // Editor 中所有 asmdef 已由 Unity 编译并装载，重复 Assembly.Load
            // 会造成相同全名却不是同一个 Type 的问题，因此只按名称查找。
            sessionAssemblies = FindEditorAssemblies(assemblyItems);
#else
            await LoadAotMetadataAsync(settings, cancellationToken);
            sessionAssemblies = await LoadHotUpdateAssembliesAsync(
                assemblyItems,
                cancellationToken);
#endif

            var entryAssembly = sessionAssemblies.FirstOrDefault(
                assembly => string.Equals(
                    assembly.GetName().Name,
                    settings.EntryAssemblyName,
                    StringComparison.Ordinal));

            if (entryAssembly == null)
            {
                throw new InvalidOperationException(
                    "Entry assembly is not in the configured hot update " +
                    "assembly list: " + settings.EntryAssemblyName);
            }

            var entryType = entryAssembly.GetType(
                settings.EntryTypeName,
                throwOnError: true);

            if (!(Activator.CreateInstance(entryType) is IHotUpdateEntry entry))
            {
                throw new InvalidCastException(
                    settings.EntryTypeName +
                    " does not implement IHotUpdateEntry.");
            }

            await entry.InitializeAsync(
                services,
                cancellationToken);

            loadedAssemblies.Clear();
            loadedAssemblies.AddRange(sessionAssemblies);
            LoadedEntry = entry;
            logger.Log(
                "HybridCLR hot update entry started: " +
                settings.EntryTypeName +
                ", assemblies=" + loadedAssemblies.Count);

            return entry;
        }

        public async Task ShutdownAsync(
            CancellationToken cancellationToken)
        {
            if (LoadedEntry == null)
                return;

            await LoadedEntry.ShutdownAsync(cancellationToken);
            LoadedEntry = null;
            loadedAssemblies.Clear();
        }

        private static IReadOnlyList<HotUpdateAssemblyLoadItem>
            ValidateAssemblyItems(HotUpdateLoadSettings settings)
        {
            var items = settings.HotUpdateAssemblies;
            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException(
                    "No hot update assemblies are configured.");
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            var addresses = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in items)
            {
                if (item == null ||
                    string.IsNullOrWhiteSpace(item.AssemblyName) ||
                    string.IsNullOrWhiteSpace(item.DllAddress))
                {
                    throw new InvalidOperationException(
                        "Hot update assembly name and DLL address " +
                        "cannot be empty.");
                }

                if (!names.Add(item.AssemblyName))
                {
                    throw new InvalidOperationException(
                        "Duplicated hot update assembly: " +
                        item.AssemblyName);
                }

                if (!addresses.Add(item.DllAddress))
                {
                    throw new InvalidOperationException(
                        "Duplicated hot update DLL address: " +
                        item.DllAddress);
                }
            }

            if (!names.Contains(settings.EntryAssemblyName))
            {
                throw new InvalidOperationException(
                    "Entry assembly must be included in the hot update " +
                    "assembly list: " + settings.EntryAssemblyName);
            }

            return items;
        }

#if UNITY_EDITOR
        private static IReadOnlyList<Assembly> FindEditorAssemblies(
            IReadOnlyList<HotUpdateAssemblyLoadItem> items)
        {
            var appDomainAssemblies =
                AppDomain.CurrentDomain.GetAssemblies();
            var result = new List<Assembly>(items.Count);

            foreach (var item in items)
            {
                var assembly = appDomainAssemblies.FirstOrDefault(x =>
                    string.Equals(
                        x.GetName().Name,
                        item.AssemblyName,
                        StringComparison.Ordinal));

                if (assembly == null)
                {
                    throw new InvalidOperationException(
                        "Hot update assembly is not loaded in Editor: " +
                        item.AssemblyName);
                }

                result.Add(assembly);
            }

            return result;
        }
#endif

#if !UNITY_EDITOR
        private async Task LoadAotMetadataAsync(
            HotUpdateLoadSettings settings,
            CancellationToken cancellationToken)
        {
            foreach (var address in settings.AotMetadataAddresses)
            {
                if (string.IsNullOrWhiteSpace(address))
                    continue;

                var metadata = await assets.LoadAsync<TextAsset>(
                    address,
                    cancellationToken);

                try
                {
                    var result =
                        RuntimeApi.LoadMetadataForAOTAssembly(
                            metadata.bytes,
                            HomologousImageMode.SuperSet);

                    if (result != LoadImageErrorCode.OK &&
                        result !=
                        LoadImageErrorCode.HOMOLOGOUS_ASSEMBLY_HAS_LOADED)
                    {
                        throw new InvalidOperationException(
                            "Load AOT metadata failed: " +
                            address + ", result=" + result);
                    }

                    logger.Log(
                        "AOT metadata loaded: " + address);
                }
                finally
                {
                    assets.Release(address);
                }
            }
        }

        private async Task<IReadOnlyList<Assembly>>
            LoadHotUpdateAssembliesAsync(
            IReadOnlyList<HotUpdateAssemblyLoadItem> items,
            CancellationToken cancellationToken)
        {
            var result = new List<Assembly>(items.Count);

            // Assembly.Load 会立即解析当前 DLL 的依赖。因此配置顺序必须是
            // Core/Common -> Modules -> Experience -> Entry。
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dllAsset = await assets.LoadAsync<TextAsset>(
                    item.DllAddress,
                    cancellationToken);

                try
                {
                    var assembly = Assembly.Load(dllAsset.bytes);
                    var actualName = assembly.GetName().Name;
                    if (!string.Equals(
                            actualName,
                            item.AssemblyName,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Hot update DLL name mismatch. Expected=" +
                            item.AssemblyName + ", Actual=" + actualName +
                            ", Address=" + item.DllAddress);
                    }

                    result.Add(assembly);
                    logger.Log(
                        "Hot update DLL loaded: " +
                        item.AssemblyName + " <- " + item.DllAddress);
                }
                finally
                {
                    // Assembly.Load 已复制 DLL 字节，资源句柄可以立即归还。
                    assets.Release(item.DllAddress);
                }
            }

            return result;
        }
#endif
    }
}
