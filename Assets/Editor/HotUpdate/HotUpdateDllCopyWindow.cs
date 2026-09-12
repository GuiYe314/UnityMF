#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Museum.Editor
{
    /// <summary>
    /// 热更新 DLL 复制工具配置。
    ///
    /// 配置会保存到：
    ///
    /// ProjectSettings/
    /// MuseumHotUpdateDllCopySettings.asset
    ///
    /// 因此：
    /// 1. 关闭 Unity 后不会丢失。
    /// 2. 重新打开项目后仍然记得路径。
    /// 3. 不依赖 EditorPrefs。
    /// </summary>
    [FilePath(
        "ProjectSettings/MuseumHotUpdateDllCopySettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class HotUpdateDllCopySettings
        : ScriptableSingleton<HotUpdateDllCopySettings>
    {
        /// <summary>
        /// 当前操作的平台。
        /// </summary>
        [SerializeField]
        private BuildTarget buildTarget =
            BuildTarget.StandaloneWindows64;

        /// <summary>
        /// 是否使用 HybridCLR 默认输出目录。
        /// </summary>
        [SerializeField]
        private bool useAutoSourceDirectory = true;

        /// <summary>
        /// 手动指定的 DLL 来源目录。
        /// </summary>
        [SerializeField]
        private string customSourceDirectory = string.Empty;

        /// <summary>
        /// DLL 复制到 Unity Assets 下的目标目录。
        ///
        /// 推荐保存项目相对路径，例如：
        ///
        /// </summary>
        [SerializeField]
        private string targetDirectory =
            "Assets/ResourceFile/HotUpdate";

        /// <summary>
        /// 已勾选的 DLL 文件名。
        ///
        /// 例如：
        ///
        /// Museum.HotUpdate.dll
        /// Museum.Gameplay.dll
        /// </summary>
        [SerializeField]
        private List<string> selectedDlls = new();

        public BuildTarget BuildTarget
        {
            get => buildTarget;
            set => buildTarget = value;
        }

        public bool UseAutoSourceDirectory
        {
            get => useAutoSourceDirectory;
            set => useAutoSourceDirectory = value;
        }

        public string CustomSourceDirectory
        {
            get => customSourceDirectory;
            set => customSourceDirectory = value;
        }

        public string TargetDirectory
        {
            get => targetDirectory;
            set => targetDirectory = value;
        }

        public IReadOnlyList<string> SelectedDlls =>
            selectedDlls;

        /// <summary>
        /// 判断某个 DLL 是否已经选中。
        /// </summary>
        public bool IsSelected(string dllName)
        {
            return selectedDlls.Contains(dllName);
        }

        /// <summary>
        /// 修改 DLL 选择状态。
        /// </summary>
        public void SetSelected(
            string dllName,
            bool selected)
        {
            if (selected)
            {
                if (!selectedDlls.Contains(dllName))
                {
                    selectedDlls.Add(dllName);
                }
            }
            else
            {
                selectedDlls.Remove(dllName);
            }
        }

        /// <summary>
        /// 清空全部选择。
        /// </summary>
        public void ClearSelection()
        {
            selectedDlls.Clear();
        }

        /// <summary>
        /// 保存配置。
        /// </summary>
        public void SaveSettings()
        {
            Save(true);
        }
    }


    /// <summary>
    /// HybridCLR 热更新 DLL 复制窗口。
    /// </summary>
    public sealed class HotUpdateDllCopyWindow : EditorWindow
    {
        private readonly List<string> availableDlls = new();

        private Vector2 dllScrollPosition;

        private HotUpdateDllCopySettings Settings =>
            HotUpdateDllCopySettings.instance;


        // ============================================================
        // Unity Menu
        // ============================================================

        [MenuItem(
            "Tools/HotUpdate/DLL拷贝到Assets")]
        private static void OpenWindow()
        {
            var window =
                GetWindow<HotUpdateDllCopyWindow>();

            window.titleContent =
                new GUIContent("HotUpdate DLL");

            window.minSize =
                new Vector2(650, 500);

            window.Show();
        }


        // ============================================================
        // 生命周期
        // ============================================================

        private void OnEnable()
        {
            // 第一次打开时，如果配置的平台不存在，
            // 默认使用 Unity 当前激活平台。
            if (!Enum.IsDefined(
                    typeof(BuildTarget),
                    Settings.BuildTarget))
            {
                Settings.BuildTarget =
                    EditorUserBuildSettings.activeBuildTarget;
            }

            RefreshDllList();
        }


        // ============================================================
        // GUI
        // ============================================================

        private void OnGUI()
        {
            DrawTitle();

            EditorGUILayout.Space(10);

            DrawBuildTarget();

            EditorGUILayout.Space(10);

            DrawSourceDirectory();

            EditorGUILayout.Space(10);

            DrawTargetDirectory();

            EditorGUILayout.Space(15);

            DrawDllList();

            EditorGUILayout.Space(15);

            DrawBottomButtons();
        }


        // ============================================================
        // 标题
        // ============================================================

        private static void DrawTitle()
        {
            EditorGUILayout.LabelField(
                "HybridCLR HotUpdate DLL Copy",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "从 HybridCLR 编译输出目录中选择热更新 DLL，" +
                "复制到 Unity Assets 目录，并自动改名为 .dll.bytes。",
                MessageType.Info);
        }


        // ============================================================
        // Platform
        // ============================================================

        private void DrawBuildTarget()
        {
            EditorGUILayout.LabelField(
                "构建平台",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            var newTarget =
                (BuildTarget)EditorGUILayout.EnumPopup(
                    "Build Target",
                    Settings.BuildTarget);

            if (EditorGUI.EndChangeCheck())
            {
                Settings.BuildTarget =
                    newTarget;

                Settings.SaveSettings();

                RefreshDllList();
            }

            EditorGUILayout.LabelField(
                "Unity Active Target",
                EditorUserBuildSettings
                    .activeBuildTarget
                    .ToString());
        }


        // ============================================================
        // 来源目录
        // ============================================================

        private void DrawSourceDirectory()
        {
            EditorGUILayout.LabelField(
                "DLL 来源目录",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            var auto =
                EditorGUILayout.Toggle(
                    "自动 HybridCLR 路径",
                    Settings.UseAutoSourceDirectory);

            if (EditorGUI.EndChangeCheck())
            {
                Settings.UseAutoSourceDirectory =
                    auto;

                Settings.SaveSettings();

                RefreshDllList();
            }


            var sourceDirectory =
                GetCurrentSourceDirectory();


            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.TextField(
                "Source",
                sourceDirectory);

            if (GUILayout.Button(
                    "选择",
                    GUILayout.Width(60)))
            {
                SelectSourceDirectory();
            }

            if (GUILayout.Button(
                    "打开",
                    GUILayout.Width(60)))
            {
                OpenDirectory(sourceDirectory);
            }

            EditorGUILayout.EndHorizontal();


            if (Settings.UseAutoSourceDirectory)
            {
                EditorGUILayout.HelpBox(
                    "当前使用 HybridCLR 默认 HotUpdateDlls 输出目录。" +
                    "如果你的 HybridCLR 版本目录结构不同，点击“选择”手动指定一次即可。",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "当前使用手动 DLL 来源目录，该路径会保存到 ProjectSettings。",
                    MessageType.Warning);

                if (GUILayout.Button("恢复自动路径"))
                {
                    Settings.UseAutoSourceDirectory =
                        true;

                    Settings.SaveSettings();

                    RefreshDllList();
                }
            }


            if (!Directory.Exists(sourceDirectory))
            {
                EditorGUILayout.HelpBox(
                    "DLL 来源目录不存在。\n\n" +
                    sourceDirectory +
                    "\n\n请先执行：\n" +
                    "HybridCLR → CompileDll → ActiveBuildTarget",
                    MessageType.Error);
            }
        }


        // ============================================================
        // 目标目录
        // ============================================================

        private void DrawTargetDirectory()
        {
            EditorGUILayout.LabelField(
                "Unity 目标目录",
                EditorStyles.boldLabel);


            EditorGUILayout.BeginHorizontal();

            var newTarget =
                EditorGUILayout.TextField(
                    "Target",
                    Settings.TargetDirectory);

            if (newTarget != Settings.TargetDirectory)
            {
                Settings.TargetDirectory =
                    NormalizePath(newTarget);

                Settings.SaveSettings();
            }


            if (GUILayout.Button(
                    "选择",
                    GUILayout.Width(60)))
            {
                SelectTargetDirectory();
            }


            if (GUILayout.Button(
                    "打开",
                    GUILayout.Width(60)))
            {
                OpenDirectory(
                    GetTargetAbsoluteDirectory());
            }

            EditorGUILayout.EndHorizontal();


            var targetAbsolute =
                GetTargetAbsoluteDirectory();


            if (!IsInsideAssets(targetAbsolute))
            {
                EditorGUILayout.HelpBox(
                    "目标目录必须位于当前 Unity 项目的 Assets 目录中，" +
                    "否则 Unity 和 YooAsset Collector 无法正常导入该 DLL。",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "推荐让 YooAsset Collector 单独收集此目录，" +
                    "这样 HotUpdate DLL 可以形成独立 Bundle，代码更新时只重新下载很小的 Bundle。",
                    MessageType.Info);
            }
        }


        // ============================================================
        // DLL List
        // ============================================================

        private void DrawDllList()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                $"发现 DLL：{availableDlls.Count}",
                EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                    "刷新",
                    GUILayout.Width(70)))
            {
                RefreshDllList();
            }

            if (GUILayout.Button(
                    "全选",
                    GUILayout.Width(70)))
            {
                SelectAllDlls();
            }

            if (GUILayout.Button(
                    "全不选",
                    GUILayout.Width(70)))
            {
                DeselectAllDlls();
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(5);


            if (availableDlls.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "当前目录下没有找到 .dll 文件。",
                    MessageType.Warning);

                return;
            }


            dllScrollPosition =
                EditorGUILayout.BeginScrollView(
                    dllScrollPosition,
                    GUI.skin.box,
                    GUILayout.MinHeight(180));


            foreach (var dllName in availableDlls)
            {
                DrawDllItem(dllName);
            }


            EditorGUILayout.EndScrollView();


            var selectedCount =
                availableDlls.Count(
                    Settings.IsSelected);

            EditorGUILayout.LabelField(
                $"已选择：{selectedCount} / {availableDlls.Count}");
        }


        private void DrawDllItem(string dllName)
        {
            var selected =
                Settings.IsSelected(dllName);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            var newSelected =
                EditorGUILayout.Toggle(
                    selected,
                    GUILayout.Width(20));

            if (EditorGUI.EndChangeCheck())
            {
                Settings.SetSelected(
                    dllName,
                    newSelected);

                Settings.SaveSettings();
            }


            EditorGUILayout.LabelField(
                dllName);


            var sourcePath =
                Path.Combine(
                    GetCurrentSourceDirectory(),
                    dllName);


            if (File.Exists(sourcePath))
            {
                var fileInfo =
                    new FileInfo(sourcePath);

                EditorGUILayout.LabelField(
                    FormatFileSize(fileInfo.Length),
                    GUILayout.Width(80));

                EditorGUILayout.LabelField(
                    fileInfo.LastWriteTime
                        .ToString("MM-dd HH:mm:ss"),
                    GUILayout.Width(110));
            }


            EditorGUILayout.EndHorizontal();
        }


        // ============================================================
        // Bottom
        // ============================================================

        private void DrawBottomButtons()
        {
            using (new EditorGUI.DisabledScope(
                       !CanCopy()))
            {
                var oldColor =
                    GUI.backgroundColor;

                GUI.backgroundColor =
                    new Color(
                        0.35f,
                        0.8f,
                        0.45f);

                if (GUILayout.Button(
                        "复制选中的 DLL → .dll.bytes",
                        GUILayout.Height(40)))
                {
                    CopySelectedDlls();
                }

                GUI.backgroundColor =
                    oldColor;
            }
        }


        // ============================================================
        // 刷新 DLL
        // ============================================================

        private void RefreshDllList()
        {
            availableDlls.Clear();

            var sourceDirectory =
                GetCurrentSourceDirectory();

            if (!Directory.Exists(sourceDirectory))
            {
                Repaint();
                return;
            }


            var dllFiles =
                Directory
                    .GetFiles(
                        sourceDirectory,
                        "*.dll",
                        SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(x =>
                        !string.IsNullOrEmpty(x))
                    .OrderBy(x => x)
                    .ToArray();


            availableDlls.AddRange(dllFiles);

            Repaint();
        }


        // ============================================================
        // Select
        // ============================================================

        private void SelectAllDlls()
        {
            foreach (var dll in availableDlls)
            {
                Settings.SetSelected(
                    dll,
                    true);
            }

            Settings.SaveSettings();
        }


        private void DeselectAllDlls()
        {
            foreach (var dll in availableDlls)
            {
                Settings.SetSelected(
                    dll,
                    false);
            }

            Settings.SaveSettings();
        }


        // ============================================================
        // Copy
        // ============================================================

        private void CopySelectedDlls()
        {
            var sourceDirectory =
                GetCurrentSourceDirectory();

            var targetDirectory =
                GetTargetAbsoluteDirectory();


            if (!Directory.Exists(sourceDirectory))
            {
                EditorUtility.DisplayDialog(
                    "错误",
                    "DLL 来源目录不存在：\n" +
                    sourceDirectory,
                    "确定");

                return;
            }


            if (!IsInsideAssets(targetDirectory))
            {
                EditorUtility.DisplayDialog(
                    "错误",
                    "目标目录必须位于 Unity Assets 目录内。",
                    "确定");

                return;
            }


            Directory.CreateDirectory(
                targetDirectory);


            var selectedDlls =
                availableDlls
                    .Where(Settings.IsSelected)
                    .ToArray();


            if (selectedDlls.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "提示",
                    "请至少选择一个 DLL。",
                    "确定");

                return;
            }


            try
            {
                var copiedCount = 0;


                foreach (var dllName in selectedDlls)
                {
                    var sourcePath =
                        Path.Combine(
                            sourceDirectory,
                            dllName);


                    if (!File.Exists(sourcePath))
                    {
                        Debug.LogWarning(
                            $"[Museum.HotUpdate] DLL不存在：{sourcePath}");

                        continue;
                    }


                    // ------------------------------------------------
                    // Museum.HotUpdate.dll
                    //
                    // ↓
                    //
                    // Museum.HotUpdate.dll.bytes
                    // ------------------------------------------------

                    var targetFileName =
                        dllName + ".bytes";


                    var targetPath =
                        Path.Combine(
                            targetDirectory,
                            targetFileName);


                    File.Copy(
                        sourcePath,
                        targetPath,
                        overwrite: true);


                    copiedCount++;


                    var sourceInfo =
                        new FileInfo(sourcePath);


                    Debug.Log(
                        "[Museum.HotUpdate] DLL复制完成" +
                        $"\nPlatform : {Settings.BuildTarget}" +
                        $"\nSource   : {sourcePath}" +
                        $"\nTarget   : {targetPath}" +
                        $"\nSize     : {FormatFileSize(sourceInfo.Length)}" +
                        $"\nModified : {sourceInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                }


                AssetDatabase.Refresh(
                    ImportAssetOptions.ForceUpdate);


                EditorUtility.DisplayDialog(
                    "复制完成",
                    $"成功复制 {copiedCount} 个 DLL。\n\n" +
                    $"目标目录：\n{Settings.TargetDirectory}",
                    "确定");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "复制失败",
                    exception.Message,
                    "确定");
            }
        }


        // ============================================================
        // Source Path
        // ============================================================

        private string GetCurrentSourceDirectory()
        {
            if (!Settings.UseAutoSourceDirectory &&
                !string.IsNullOrWhiteSpace(
                    Settings.CustomSourceDirectory))
            {
                return NormalizePath(
                    Settings.CustomSourceDirectory);
            }

            return GetDefaultHybridClrDirectory(
                Settings.BuildTarget);
        }


        /// <summary>
        /// 获取 HybridCLR 默认 HotUpdate DLL 输出目录。
        ///
        /// 默认结构：
        ///
        /// Project
        /// └── HybridCLRData
        ///     └── HotUpdateDlls
        ///         └── Android
        ///             └── xxx.dll
        /// </summary>
        private static string
            GetDefaultHybridClrDirectory(
                BuildTarget buildTarget)
        {
            var root =
                Path.Combine(
                    GetProjectRoot(),
                    "HybridCLRData",
                    "HotUpdateDlls");


            var candidates =
                GetTargetFolderCandidates(
                    buildTarget);


            foreach (var candidate in candidates)
            {
                var path =
                    Path.Combine(
                        root,
                        candidate);

                if (Directory.Exists(path))
                {
                    return NormalizePath(path);
                }
            }


            // 如果目录暂时不存在，
            // 返回最可能的默认目录。
            return NormalizePath(
                Path.Combine(
                    root,
                    buildTarget.ToString()));
        }


        private static IEnumerable<string>
            GetTargetFolderCandidates(
                BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:

                    yield return "Android";

                    break;


                case BuildTarget.iOS:

                    yield return "iOS";
                    yield return "IPhone";

                    break;


                case BuildTarget.StandaloneWindows64:

                    yield return "StandaloneWindows64";
                    yield return "Win64";

                    break;


                case BuildTarget.StandaloneWindows:

                    yield return "StandaloneWindows";
                    yield return "Win32";

                    break;


                case BuildTarget.StandaloneOSX:

                    yield return "StandaloneOSX";
                    yield return "OSX";

                    break;


                default:

                    yield return buildTarget.ToString();

                    break;
            }
        }


        // ============================================================
        // Folder Picker
        // ============================================================

        private void SelectSourceDirectory()
        {
            var current =
                GetCurrentSourceDirectory();


            var selected =
                EditorUtility.OpenFolderPanel(
                    "选择 HybridCLR DLL 来源目录",
                    current,
                    string.Empty);


            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }


            Settings.CustomSourceDirectory =
                NormalizePath(selected);

            Settings.UseAutoSourceDirectory =
                false;

            Settings.SaveSettings();


            RefreshDllList();
        }


        private void SelectTargetDirectory()
        {
            var current =
                GetTargetAbsoluteDirectory();


            var selected =
                EditorUtility.OpenFolderPanel(
                    "选择 DLL 目标目录",
                    current,
                    string.Empty);


            if (string.IsNullOrWhiteSpace(selected))
            {
                return;
            }


            selected =
                NormalizePath(selected);


            if (!IsInsideAssets(selected))
            {
                EditorUtility.DisplayDialog(
                    "目录无效",
                    "目标目录必须位于当前 Unity 项目的 Assets 目录内。",
                    "确定");

                return;
            }


            Settings.TargetDirectory =
                AbsoluteToProjectRelative(
                    selected);

            Settings.SaveSettings();
        }


        // ============================================================
        // Validation
        // ============================================================

        private bool CanCopy()
        {
            if (!Directory.Exists(
                    GetCurrentSourceDirectory()))
            {
                return false;
            }


            if (!IsInsideAssets(
                    GetTargetAbsoluteDirectory()))
            {
                return false;
            }


            return availableDlls.Any(
                Settings.IsSelected);
        }


        // ============================================================
        // Path Helper
        // ============================================================

        private static string GetProjectRoot()
        {
            return NormalizePath(
                Directory
                    .GetParent(
                        Application.dataPath)!
                    .FullName);
        }


        private string GetTargetAbsoluteDirectory()
        {
            var target =
                Settings.TargetDirectory;


            if (string.IsNullOrWhiteSpace(target))
            {
                return string.Empty;
            }


            if (Path.IsPathRooted(target))
            {
                return NormalizePath(target);
            }


            return NormalizePath(
                Path.Combine(
                    GetProjectRoot(),
                    target));
        }


        private static bool IsInsideAssets(
            string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(
                    absolutePath))
            {
                return false;
            }


            var assetsPath =
                NormalizePath(
                    Application.dataPath)
                .TrimEnd('/');


            absolutePath =
                NormalizePath(
                    absolutePath)
                .TrimEnd('/');


            return absolutePath.Equals(
                       assetsPath,
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   absolutePath.StartsWith(
                       assetsPath + "/",
                       StringComparison.OrdinalIgnoreCase);
        }


        private static string
            AbsoluteToProjectRelative(
                string absolutePath)
        {
            absolutePath =
                NormalizePath(absolutePath);


            var projectRoot =
                NormalizePath(
                    GetProjectRoot())
                .TrimEnd('/');


            if (absolutePath.StartsWith(
                    projectRoot + "/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return absolutePath[
                    (projectRoot.Length + 1)..];
            }


            return absolutePath;
        }


        private static string NormalizePath(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path
                .Replace('\\', '/')
                .TrimEnd('/');
        }


        private static void OpenDirectory(
            string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }


            if (!Directory.Exists(directory))
            {
                EditorUtility.DisplayDialog(
                    "目录不存在",
                    directory,
                    "确定");

                return;
            }


            EditorUtility.RevealInFinder(
                directory);
        }


        // ============================================================
        // Size
        // ============================================================

        private static string FormatFileSize(
            long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }


            if (bytes < 1024 * 1024)
            {
                return $"{bytes / 1024f:F1} KB";
            }


            return
                $"{bytes / 1024f / 1024f:F2} MB";
        }
    }
}

#endif