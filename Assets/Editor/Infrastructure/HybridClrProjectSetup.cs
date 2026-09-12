using System;
using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Installer;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Museum.Infrastructure.EditorTools
{
    /// <summary>
    /// Museum 项目的 HybridCLR 一键配置入口。
    ///
    /// 为什么做成代码：
    /// 1. 每位开发者拉取项目后都可以重复执行并得到相同结果；
    /// 2. Android / iOS 的 IL2CPP 与 API 兼容级别不会依赖人工记忆；
    /// 3. 至少登记入口程序集 HotUpdate，同时保留团队添加的其他热更新 asmdef。
    /// </summary>
    public static class HybridClrProjectSetup
    {
        private const string HotUpdateAsmdefPath =
            "Assets/2_ScriptHotUpdate/HotUpdate.asmdef";

        private static readonly string[] DefaultPatchAotAssemblies =
        {
            "mscorlib",
            "System",
            "System.Core"
        };

        /// <summary>
        /// 配置项目级 HybridCLR 设置。该操作可安全重复执行。
        /// </summary>
        [MenuItem("Museum/Hot Update/Configure Project")]
        public static void ConfigureProject()
        {
            var hotUpdateAsmdef =
                AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(
                    HotUpdateAsmdefPath);

            if (hotUpdateAsmdef == null)
            {
                throw new FileNotFoundException(
                    "未找到热更新程序集定义。",
                    HotUpdateAsmdefPath);
            }

            // 开启 HybridCLR。入口程序集必须存在，但不覆盖团队已经添加的
            // Core、Modules 等其他热更新 asmdef，因此该菜单可反复执行。
            SettingsUtil.Enable = true;

            var settings = HybridCLRSettings.Instance;
            settings.enable = true;
            settings.useGlobalIl2cpp = false;
            var configuredAsmdefs =
                new System.Collections.Generic.List<AssemblyDefinitionAsset>();
            foreach (var asmdef in
                     settings.hotUpdateAssemblyDefinitions ??
                     Array.Empty<AssemblyDefinitionAsset>())
            {
                if (asmdef != null &&
                    !configuredAsmdefs.Contains(asmdef))
                {
                    configuredAsmdefs.Add(asmdef);
                }
            }

            if (!configuredAsmdefs.Contains(hotUpdateAsmdef))
                configuredAsmdefs.Add(hotUpdateAsmdef);

            settings.hotUpdateAssemblyDefinitions =
                configuredAsmdefs.ToArray();

            // 不改写 hotUpdateAssemblies 字符串列表。部分第三方 DLL 没有
            // asmdef，只能通过程序集名称登记；清空它会让这些热更新 DLL
            // 在再次执行本菜单后悄悄丢失。

            // 第一次配置时提供最小且常用的补充元数据列表；
            // 若团队后续已经按实际泛型使用情况调整，则保留已有设置。
            if (settings.patchAOTAssemblies == null ||
                settings.patchAOTAssemblies.Length == 0)
            {
                settings.patchAOTAssemblies =
                    DefaultPatchAotAssemblies;
            }

            HybridCLRSettings.Save();

            // HybridCLR 的移动端构建以 IL2CPP 为基础。
            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.Android,
                ScriptingImplementation.IL2CPP);

            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.iOS,
                ScriptingImplementation.IL2CPP);

            // Unity 2021+ 的 HybridCLR 官方流程要求使用
            // .NET Framework API 兼容级别。
            PlayerSettings.SetApiCompatibilityLevel(
                BuildTargetGroup.Android,
                ApiCompatibilityLevel.NET_4_6);

            PlayerSettings.SetApiCompatibilityLevel(
                BuildTargetGroup.iOS,
                ApiCompatibilityLevel.NET_4_6);

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[Museum] HybridCLR 项目配置完成：" +
                "热更新程序集数=" +
                SettingsUtil.HotUpdateAssemblyNamesExcludePreserved.Count +
                "，Android/iOS IL2CPP。");
        }

        /// <summary>
        /// 从已经准备好的官方源码目录安装本地 HybridCLR 版 libil2cpp。
        /// 参数必须指向包含 HybridCLR 改造代码的 libil2cpp 目录。
        /// </summary>
        public static string InstallFromLocal(
            string libil2cppSourceDirectory)
        {
            if (string.IsNullOrWhiteSpace(
                    libil2cppSourceDirectory))
            {
                throw new ArgumentException(
                    "libil2cpp 源码目录不能为空。",
                    nameof(libil2cppSourceDirectory));
            }

            var fullPath =
                Path.GetFullPath(libil2cppSourceDirectory);

            if (!Directory.Exists(fullPath))
            {
                throw new DirectoryNotFoundException(fullPath);
            }

            var controller = new InstallerController();
            controller.InstallFromLocal(fullPath);

            if (!controller.HasInstalledHybridCLR())
            {
                throw new InvalidOperationException(
                    "HybridCLR 本地运行时安装后校验失败。");
            }

            Debug.Log(
                "[Museum] HybridCLR 本地运行时安装成功：" +
                fullPath);

            return fullPath;
        }

        /// <summary>
        /// 菜单校验当前项目是否已经具备构建热更新包的基本条件。
        /// </summary>
        [MenuItem("Museum/Hot Update/Validate Setup")]
        public static void ValidateSetup()
        {
            var controller = new InstallerController();
            var settings = HybridCLRSettings.Instance;

            var configured =
                settings.enable &&
                settings.hotUpdateAssemblyDefinitions != null &&
                Array.Exists(
                    settings.hotUpdateAssemblyDefinitions,
                    asset => asset != null &&
                             asset.name == "HotUpdate");

            if (!controller.HasInstalledHybridCLR())
            {
                throw new InvalidOperationException(
                    "HybridCLR 本地运行时尚未安装。");
            }

            if (!configured)
            {
                throw new InvalidOperationException(
                    "HotUpdate 尚未登记到 HybridCLR 设置。");
            }

            Debug.Log(
                "[Museum] HybridCLR 配置校验通过。");
        }
    }
}
