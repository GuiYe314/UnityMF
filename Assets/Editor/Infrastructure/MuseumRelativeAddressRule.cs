using System;
using System.IO;
using YooAsset.Editor;

namespace Museum.Infrastructure.EditorTools
{
    /// <summary>
    /// 使用“相对收集根目录的路径（去掉最后一个扩展名）”作为 YooAsset 地址。
    ///
    /// 示例：
    /// Assets/Museum/Content/DefaultPackage/UpdateProbe.txt
    ///     -> UpdateProbe
    /// Assets/Museum/Content/DefaultPackage/HotUpdate/Museum.HotUpdate.dll.bytes
    ///     -> HotUpdate/Museum.HotUpdate.dll
    /// Assets/Museum/Content/DefaultPackage/AOT/mscorlib.dll.bytes
    ///     -> AOT/mscorlib.dll
    ///
    /// 这样地址仍然可读，同时不同目录中的同名文件不会互相冲突。
    /// .bytes 只是让 Unity 把 DLL 当作 TextAsset 导入，不属于运行时地址。
    /// </summary>
    [global::YooAsset.Editor.DisplayName("Museum: 相对路径（去最后扩展名）")]
    public sealed class MuseumRelativeAddressRule : IAddressRule
    {
        string IAddressRule.GetAssetAddress(AddressRuleData data)
        {
            var assetPath = Normalize(data.AssetPath);
            var collectRoot = Normalize(data.CollectPath)
                .TrimEnd('/') + "/";

            var relativePath = assetPath.StartsWith(
                    collectRoot,
                    StringComparison.OrdinalIgnoreCase)
                ? assetPath.Substring(collectRoot.Length)
                : Path.GetFileName(assetPath);

            // 只去掉最后一个扩展名：
            // xxx.dll.bytes -> xxx.dll；UpdateProbe.txt -> UpdateProbe。
            var lastSlash = relativePath.LastIndexOf('/');
            var lastDot = relativePath.LastIndexOf('.');
            if (lastDot > lastSlash)
                relativePath = relativePath.Substring(0, lastDot);

            return relativePath;
        }

        private static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/');
        }
    }
}
