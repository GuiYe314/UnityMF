using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOT.HotUpdate;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace Museum.Infrastructure.EditorTools
{
    /// <summary>
    /// 博物馆项目的“代码热更 + 资源热更”统一构建入口。
    ///
    /// 流程关系：
    /// 1. HybridCLR 编译 Settings 中登记的全部热更新 DLL；
    /// 2. 本工具按登记顺序复制 DLL 和 AOT 补充元数据为 TextAsset（*.bytes）；
    /// 3. YooAsset 把 Content/DefaultPackage 下的模型、视频、配置和 DLL 一起构建；
    /// 4. 最终把 YooAsset 输出目录发布到 CDN，手机 UpdateScene 就能下载。
    ///
    /// 所有菜单都只处理 Unity 当前激活平台，不会自动切换平台。
    /// Android 发布前先在 Build Settings 切到 Android；iOS 同理。
    /// </summary>
    public static class MuseumContentBuildPipeline
    {
        private const string EntryHotUpdateAssemblyName =
            "HotUpdate";
        private const string ContentRoot =
            "Assets/3_ResourceFile";
        private const string HotUpdateContentRoot =
            ContentRoot + "/HotUpdate";
        private const string AotContentRoot =
            ContentRoot + "/AOT";

        private const string RuntimeConfigPath =
            "Assets/Scenes/YooAssetRuntimeConfig.asset";
        private const string HotUpdateSettingsPath =
            "Assets/Scenes/HotUpdateLoadSettings.asset";

        /// <summary>
        /// 将内容版本最后一段数字加一，例如 v1.0 -> v1.1、v1.2.9 -> v1.2.10。
        ///
        /// YooAsset 的已发布版本目录不可覆盖：同一版本号必须永远对应同一份清单。
        /// 日常准备新内容包时先执行该菜单，再执行 Build YooAsset Package。
        /// 如果候选版本目录已经存在，会继续递增直到找到未使用版本。
        /// </summary>
        [MenuItem(
            "Museum/Build/0. Increment Content Version",
            priority = 90)]
        public static void IncrementContentVersion()
        {
            ValidateBuildEnvironment();

            var runtimeConfig =
                AssetDatabase.LoadAssetAtPath<YooAssetRuntimeConfig>(
                    RuntimeConfigPath);
            if (runtimeConfig == null)
            {
                throw new BuildFailedException(
                    "Missing YooAsset runtime config: " +
                    RuntimeConfigPath);
            }

            var target = EditorUserBuildSettings.activeBuildTarget;
            var nextVersion =
                IncrementLastNumericSegment(runtimeConfig.AppVersion);

            // 跳过当前平台中已经构建过的版本，避免再次撞到旧目录。
            while (Directory.Exists(
                       GetOutputPackageDirectory(
                           target,
                           runtimeConfig.PackageName,
                           nextVersion)))
            {
                nextVersion =
                    IncrementLastNumericSegment(nextVersion);
            }

            var serialized = new SerializedObject(runtimeConfig);
            serialized.FindProperty("appVersion").stringValue =
                nextVersion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(runtimeConfig);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Museum.Build] Content version advanced: " +
                nextVersion);
        }

        /// <summary>
        /// 快速迭代：编译全部热更 DLL 并复制到 YooAsset 内容目录。
        /// 不重新生成裁剪后的 AOT DLL，适合日常修改业务代码后快速验证。
        /// 真机正式包仍应执行下方的 Full 或 Build All。
        /// </summary>
        [MenuItem(
            "Museum/Build/1. Compile Hot Update DLL (Fast)",
            priority = 100)]
        public static void CompileHotUpdateDllFast()
        {
            ValidateBuildEnvironment();

            var target = EditorUserBuildSettings.activeBuildTarget;
            Debug.Log(
                "[Museum.Build] Compile hot update DLL. Target=" +
                target);

            CompileDllCommand.CompileDll(
                target,
                developmentBuild: true);

            var hotUpdateAssemblies = CopyHotUpdateDlls(target);
            ConfigureHotUpdateLoadSettings(
                hotUpdateAssemblies,
                Array.Empty<string>());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Museum.Build] Fast hot update DLL is ready: " +
                "count=" + hotUpdateAssemblies.Count +
                ", directory=" + HotUpdateContentRoot);
        }

        /// <summary>
        /// 正式生成：执行 HybridCLR 官方 GenerateAll。
        /// 这一步会生成桥接函数、link.xml、热更 DLL，并临时构建裁剪后的 AOT DLL，
        /// 因此耗时明显长于 Fast，但是真机发布前必须执行。
        /// </summary>
        [MenuItem(
            "Museum/Build/2. Generate HybridCLR Files (Full)",
            priority = 110)]
        public static void GenerateHybridClrFilesFull()
        {
            ValidateBuildEnvironment();

            var target = EditorUserBuildSettings.activeBuildTarget;
            Debug.Log(
                "[Museum.Build] Generate all HybridCLR files. Target=" +
                target);

            PrebuildCommand.GenerateAll();

            // GenerateAll 会临时执行 Player 构建。完成后统一进入 staging，
            // 将平台相关产物复制到 YooAsset 当前发布内容目录。
            StageGeneratedHybridClrFilesCore(target);
        }

        /// <summary>
        /// 只整理已经由 HybridCLR 生成的文件，不重复执行耗时的 GenerateAll。
        ///
        /// 用途：
        /// - GenerateAll 已成功，但后续资源导入或配置回写被中断；
        /// - 检查 AOT 文件后，需要重新同步到 YooAsset；
        /// - 自动化工具不适合等待长同步构建时，可把“生成”和“整理”拆开。
        /// </summary>
        [MenuItem(
            "Museum/Build/2b. Stage Generated HybridCLR Files",
            priority = 115)]
        public static void StageGeneratedHybridClrFiles()
        {
            ValidateBuildEnvironment();
            StageGeneratedHybridClrFilesCore(
                EditorUserBuildSettings.activeBuildTarget);
        }

        private static void StageGeneratedHybridClrFilesCore(
            BuildTarget target)
        {
            var hotUpdateAssemblies = CopyHotUpdateDlls(target);
            var aotAddresses = CopyAotMetadataDlls(target);

            // 强制同步导入刚复制的 .bytes，再读取并保存配置资源。
            // ScriptableObject 必须位于同名 HotUpdateLoadSettings.cs，
            // 才能在 Player 构建后的脚本重载中稳定恢复。
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport);
            ConfigureHotUpdateLoadSettings(
                hotUpdateAssemblies,
                aotAddresses);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Museum.Build] HybridCLR files are ready. Target=" +
                target + ", AOT metadata count=" +
                aotAddresses.Count +
                ", hot update assembly count=" +
                hotUpdateAssemblies.Count);
        }

        /// <summary>
        /// 构建当前版本的 YooAsset 包。
        /// 版本号读取 YooAssetRuntimeConfig.AppVersion，默认输出到项目 Bundles 目录。
        /// 该菜单不负责上传 CDN，避免开发期间误发布。
        /// </summary>
        [MenuItem(
            "Museum/Build/3. Build YooAsset Package",
            priority = 120)]
        public static void BuildYooAssetPackage()
        {
            ValidateBuildEnvironment();
            var runtimeConfig =
                AssetDatabase.LoadAssetAtPath<YooAssetRuntimeConfig>(
                    RuntimeConfigPath);

            if (runtimeConfig == null)
            {
                throw new BuildFailedException(
                    "Missing YooAsset runtime config: " +
                    RuntimeConfigPath +
                    ". Run Museum/Update/Build Editor Demo first.");
            }

            UpdateSceneBuilder.ConfigureYooAssetCollector(
                runtimeConfig.PackageName);

            var hotUpdateAssemblyNames =
                GetOrderedHotUpdateAssemblyNames();
            var missingHotUpdateDll = hotUpdateAssemblyNames
                .Select(GetHotUpdateContentFilePath)
                .FirstOrDefault(path => !File.Exists(path));
            if (!string.IsNullOrEmpty(missingHotUpdateDll))
            {
                throw new BuildFailedException(
                    "Hot update DLL has not been prepared: " +
                    missingHotUpdateDll + ". " +
                    "Run menu 1 (Fast) or menu 2 (Full) first.");
            }

            var target = EditorUserBuildSettings.activeBuildTarget;
            var packageName = runtimeConfig.PackageName;
            var packageVersion = runtimeConfig.AppVersion;

            var outputPackageDirectory =
                GetOutputPackageDirectory(
                    target,
                    packageName,
                    packageVersion);
            if (Directory.Exists(outputPackageDirectory))
            {
                throw new BuildFailedException(
                    "YooAsset version already exists and will not be " +
                    "overwritten: " + outputPackageDirectory +
                    ". Run Museum/Build/0. Increment Content Version " +
                    "or set a new AppVersion.");
            }

            Debug.Log(
                "[Museum.Build] Build YooAsset package. Target=" +
                target + ", package=" + packageName +
                ", version=" + packageVersion);

            var buildParameters = new ScriptableBuildParameters
            {
                BuildOutputRoot =
                    BundleBuilderHelper.GetDefaultBuildOutputRoot(),
                BundledFileRoot =
                    BundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline =
                    EBuildPipeline.ScriptableBuildPipeline.ToString(),
                BuildBundleType = (int)EBundleType.AssetBundle,
                BuildTarget = target,
                PackageName = packageName,
                PackageVersion = packageVersion,
                PackageNote =
                    "Museum content package generated by build pipeline.",
                EnableSharePackRule = true,
                VerifyBuildingResult = true,
                FileNameStyle = EFileNameStyle.BundleName_HashName,
                BundledCopyOption = EBundledCopyOption.None,
                BundledCopyParams = string.Empty,
                ClearBuildCacheFiles = false,
                UseAssetDependencyDB = true,
                CompressOption = ECompressOption.LZ4,
                BuiltinShadersBundleName =
                    GetBuiltinShadersBundleName(packageName)
            };

            var pipeline = new ScriptableBuildPipeline();
            var result = pipeline.Run(
                buildParameters,
                enableLog: true);

            if (!result.Success)
            {
                throw new BuildFailedException(
                    "YooAsset build failed. Task=" +
                    result.FailedTask + "\n" +
                    result.ErrorInfo);
            }

            Debug.Log(
                "[Museum.Build] YooAsset package ready: " +
                result.OutputPackageDirectory);
        }

        /// <summary>
        /// 发布前的一键完整构建。
        /// 顺序不能调换：先生成/复制 DLL，再把它们打入 YooAsset。
        /// </summary>
        [MenuItem(
            "Museum/Build/Build All for Active Target",
            priority = 200)]
        public static void BuildAllForActiveTarget()
        {
            GenerateHybridClrFilesFull();

            // HybridCLR staging 会复制并导入 DLL/AOT TextAsset。必须等待这些
            // 导入以及由此触发的脚本刷新完成，再进入 YooAsset 的环境校验；
            // 否则一键构建会在 EditorApplication.isUpdating 时稳定失败。
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport);
            BuildYooAssetPackage();

            Debug.Log(
                "[Museum.Build] Complete content build succeeded. " +
                "Target=" +
                EditorUserBuildSettings.activeBuildTarget);
        }

        private static void ValidateBuildEnvironment()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new BuildFailedException(
                    "Exit Play Mode before building content.");
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                throw new BuildFailedException(
                    "Wait for Unity compilation/import to finish.");
            }

            if (!SettingsUtil.Enable)
            {
                throw new BuildFailedException(
                    "HybridCLR is disabled. Enable it in " +
                    "Project Settings/HybridCLR Settings.");
            }

            if (EditorUserBuildSettings.activeBuildTarget ==
                BuildTarget.NoTarget)
            {
                throw new BuildFailedException(
                    "No active build target.");
            }

            Directory.CreateDirectory(HotUpdateContentRoot);
            Directory.CreateDirectory(AotContentRoot);
        }

        private static List<string> CopyHotUpdateDlls(
            BuildTarget target)
        {
            var assemblyNames =
                GetOrderedHotUpdateAssemblyNames();
            var sourceRoot =
                SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);

            foreach (var assemblyName in assemblyNames)
            {
                var source = Path.Combine(
                    sourceRoot,
                    assemblyName + ".dll");
                var destination =
                    GetHotUpdateContentFilePath(assemblyName);

                CopyRequiredFile(
                    source,
                    destination,
                    "hot update DLL " + assemblyName);
            }

            return assemblyNames;
        }

        private static List<string>
            GetOrderedHotUpdateAssemblyNames()
        {
            var assemblyNames = SettingsUtil
                .HotUpdateAssemblyNamesExcludePreserved
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (assemblyNames.Count == 0)
            {
                throw new BuildFailedException(
                    "HybridCLR has no hot update assemblies configured.");
            }

            if (!assemblyNames.Contains(
                    EntryHotUpdateAssemblyName,
                    StringComparer.Ordinal))
            {
                throw new BuildFailedException(
                    "Entry hot update assembly is not configured in " +
                    "HybridCLR Settings: " +
                    EntryHotUpdateAssemblyName);
            }

            return assemblyNames;
        }

        private static string GetHotUpdateContentFilePath(
            string assemblyName)
        {
            return HotUpdateContentRoot + "/" +
                   assemblyName + ".dll.bytes";
        }

        private static List<string> CopyAotMetadataDlls(
            BuildTarget target)
        {
            var sourceRoot =
                SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            var addresses = new List<string>();

            foreach (var assemblyName in
                     SettingsUtil.AOTAssemblyNames.Distinct())
            {
                var fileName = assemblyName.EndsWith(
                        ".dll",
                        StringComparison.OrdinalIgnoreCase)
                    ? assemblyName
                    : assemblyName + ".dll";

                var source = Path.Combine(sourceRoot, fileName);
                var destination =
                    AotContentRoot + "/" + fileName + ".bytes";

                CopyRequiredFile(
                    source,
                    destination,
                    "AOT metadata DLL");

                addresses.Add("AOT/" + fileName);
            }

            return addresses;
        }

        private static void CopyRequiredFile(
            string source,
            string destination,
            string description)
        {
            if (!File.Exists(source))
            {
                throw new BuildFailedException(
                    "Missing " + description + ": " + source);
            }

            var destinationDirectory =
                Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            File.Copy(
                source,
                destination,
                overwrite: true);

            AssetDatabase.ImportAsset(
                destination,
                ImportAssetOptions.ForceUpdate);

            Debug.Log(
                "[Museum.Build] Copied " + description + ": " +
                source + " -> " + destination);
        }

        private static void ConfigureHotUpdateLoadSettings(
            IReadOnlyList<string> hotUpdateAssemblyNames,
            IReadOnlyList<string> aotAddresses)
        {
            var settings =
                AssetDatabase.LoadAssetAtPath<HotUpdateLoadSettings>(
                    HotUpdateSettingsPath);

            if (settings == null)
            {
                throw new BuildFailedException(
                    "Missing hot update settings: " +
                    HotUpdateSettingsPath +
                    ". Run Museum/Update/Build Editor Demo first.");
            }

            // SerializedObject 只在编辑器构建工具中使用。
            // 运行时设置类仍保持只读属性，业务代码无法随意修改发布地址。
            var serialized = new SerializedObject(settings);

            var assembliesProperty =
                serialized.FindProperty("hotUpdateAssemblies");
            assembliesProperty.arraySize =
                hotUpdateAssemblyNames.Count;
            for (var i = 0; i < hotUpdateAssemblyNames.Count; i++)
            {
                var assemblyName = hotUpdateAssemblyNames[i];
                var item = assembliesProperty
                    .GetArrayElementAtIndex(i);
                item.FindPropertyRelative("assemblyName")
                    .stringValue = assemblyName;
                item.FindPropertyRelative("dllAddress")
                    .stringValue =
                    "HotUpdate/" + assemblyName + ".dll";
            }

            serialized.FindProperty("entryAssemblyName")
                .stringValue = EntryHotUpdateAssemblyName;

            serialized.FindProperty("entryTypeName")
                .stringValue =
                    "HotUpdate.MuseumHotUpdateEntry";

            var aotProperty =
                serialized.FindProperty("aotMetadataAddresses");
            aotProperty.arraySize = aotAddresses.Count;
            for (var i = 0; i < aotAddresses.Count; i++)
            {
                aotProperty
                    .GetArrayElementAtIndex(i)
                    .stringValue = aotAddresses[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static string IncrementLastNumericSegment(
            string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return "v1.0";

            var trimmed = version.Trim();
            var prefix = trimmed.StartsWith(
                    "v",
                    StringComparison.OrdinalIgnoreCase)
                ? trimmed.Substring(0, 1)
                : string.Empty;
            var numericPart = prefix.Length == 0
                ? trimmed
                : trimmed.Substring(1);
            var segments = numericPart.Split('.');

            if (segments.Length == 0 ||
                !int.TryParse(
                    segments[segments.Length - 1],
                    out var lastNumber))
            {
                throw new BuildFailedException(
                    "AppVersion must end with a number, for example " +
                    "v1.0 or v1.2.3. Current value: " + version);
            }

            segments[segments.Length - 1] =
                (lastNumber + 1).ToString();
            return prefix + string.Join(".", segments);
        }

        private static string GetOutputPackageDirectory(
            BuildTarget target,
            string packageName,
            string packageVersion)
        {
            return Path.Combine(
                    BundleBuilderHelper.GetDefaultBuildOutputRoot(),
                    target.ToString(),
                    packageName,
                    packageVersion)
                .Replace('\\', '/');
        }

        private static string GetBuiltinShadersBundleName(
            string packageName)
        {
            var uniqueBundleName =
                BundleCollectorSettingData.Setting.UniqueBundleName;
            var packRuleResult =
                DefaultBundlePackRule.CreateShadersPackRuleResult();

            return packRuleResult.GetBundleName(
                packageName,
                uniqueBundleName);
        }
    }
}
