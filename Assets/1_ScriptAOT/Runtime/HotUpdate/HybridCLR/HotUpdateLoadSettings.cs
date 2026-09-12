using System;
using System.Collections.Generic;
using UnityEngine;

namespace AOT.HotUpdate
{
    /// <summary>
    /// 一个需要由 HybridCLR 加载的热更新程序集。
    /// 数组顺序就是运行时加载顺序：被依赖的基础程序集放在前面，
    /// 包含 IHotUpdateEntry 的入口程序集通常放在最后。
    /// </summary>
    [Serializable]
    public sealed class HotUpdateAssemblyLoadItem
    {
        [SerializeField] private string assemblyName = string.Empty;
        [SerializeField] private string dllAddress = string.Empty;

        public string AssemblyName => assemblyName ?? string.Empty;
        public string DllAddress => dllAddress ?? string.Empty;

        public HotUpdateAssemblyLoadItem(
            string assemblyName,
            string dllAddress)
        {
            this.assemblyName = assemblyName ?? string.Empty;
            this.dllAddress = dllAddress ?? string.Empty;
        }
    }

    /// <summary>
    /// HybridCLR 运行时加载地址配置。
    ///
    /// 重要：这个 ScriptableObject 必须独占同名脚本文件
    /// HotUpdateLoadSettings.cs。Unity 在 Player 构建后的脚本重载中，
    /// 依靠同名 MonoScript 稳定恢复资源类型。
    ///
    /// 构建工具会自动填写 DLL 与 AOT 地址，业务代码只读取，不直接修改。
    /// 磁盘文件使用 .dll.bytes，YooAsset 运行时地址不包含最后的 .bytes。
    /// </summary>
    [CreateAssetMenu(
        fileName = "HotUpdateLoadSettings",
        menuName = "Museum/Hot Update/Load Settings")]
    public sealed class HotUpdateLoadSettings : ScriptableObject
    {
        [Tooltip(
            "按依赖顺序填写：公共/基础 DLL 在前，入口 DLL 在后。" +
            "构建工具会依据 HybridCLR Settings 自动更新该列表。")]
        [SerializeField] private HotUpdateAssemblyLoadItem[]
            hotUpdateAssemblies =
            {
                new HotUpdateAssemblyLoadItem(
                    "HotUpdate",
                    "HotUpdate/HotUpdate.dll")
            };

        [Tooltip("包含 EntryTypeName 的热更新程序集名称。")]
        [SerializeField] private string entryAssemblyName =
            "HotUpdate";

        [SerializeField] private string entryTypeName =
            "HotUpdate.MuseumHotUpdateEntry";

        [Tooltip(
            "需要补充元数据的 AOT DLL 的 YooAsset 地址，" +
            "例如 AOT/mscorlib.dll。")]
        [SerializeField] private string[] aotMetadataAddresses =
            Array.Empty<string>();

        /// <summary>
        /// 返回构建工具生成的有序加载列表。
        /// </summary>
        public IReadOnlyList<HotUpdateAssemblyLoadItem>
            HotUpdateAssemblies =>
            hotUpdateAssemblies ??
            Array.Empty<HotUpdateAssemblyLoadItem>();

        public string EntryAssemblyName =>
            entryAssemblyName ?? string.Empty;

        public string EntryTypeName =>
            entryTypeName;

        public string[] AotMetadataAddresses =>
            aotMetadataAddresses ?? Array.Empty<string>();
    }
}
