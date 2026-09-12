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
    /// YooAsset 本地服务器发布配置。
    ///
    /// 配置保存到：
    ///
    /// ProjectSettings/
    /// MuseumYooAssetLocalPublishSettings.asset
    ///
    /// 因此关闭 Unity 后路径不会丢失。
    /// </summary>
    [FilePath(
        "ProjectSettings/MuseumYooAssetLocalPublishSettings.asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class YooAssetLocalPublishSettings
        : ScriptableSingleton<YooAssetLocalPublishSettings>
    {
        /// <summary>
        /// YooAsset 本次构建产物目录。
        ///
        /// 注意：
        /// 这里建议直接选择本次可发布文件所在目录，
        /// 目录中应该能看到：
        ///
        /// DefaultPackage.version
        /// Manifest
        /// Bundle...
        /// </summary>
        [SerializeField]
        private string sourceDirectory = string.Empty;

        /// <summary>
        /// 本地 Nginx / CDN 磁盘目录。
        ///
        /// 例如：
        ///
        /// D:/YooCDN/CDN/PC/v1.3
        /// </summary>
        [SerializeField]
        private string serverDirectory =
            "D:/YooCDN/CDN/PC/v1.3";

        /// <summary>
        /// Package 名称。
        /// 用于找到：
        ///
        /// DefaultPackage.version
        /// </summary>
        [SerializeField]
        private string packageName =
            "DefaultPackage";

        /// <summary>
        /// 发布时是否覆盖同名文件。
        /// </summary>
        [SerializeField]
        private bool overwriteExisting = true;

        /// <summary>
        /// 发布完成后是否打开服务器目录。
        /// </summary>
        [SerializeField]
        private bool revealAfterPublish = false;

        public string SourceDirectory
        {
            get => sourceDirectory;
            set => sourceDirectory = value;
        }

        public string ServerDirectory
        {
            get => serverDirectory;
            set => serverDirectory = value;
        }

        public string PackageName
        {
            get => packageName;
            set => packageName = value;
        }

        public bool OverwriteExisting
        {
            get => overwriteExisting;
            set => overwriteExisting = value;
        }

        public bool RevealAfterPublish
        {
            get => revealAfterPublish;
            set => revealAfterPublish = value;
        }

        public void SaveSettings()
        {
            Save(true);
        }
    }


    /// <summary>
    /// YooAsset 本地发布工具。
    ///
    /// 用于：
    ///
    /// YooAsset Build Output
    ///          ↓
    /// 本地 Nginx/CDN 目录
    ///
    /// 注意：
    /// Package.version 文件会最后复制，
    /// 防止客户端提前看到尚未完整发布的新版本。
    /// </summary>
    public sealed class YooAssetLocalPublishWindow
        : EditorWindow
    {
        private Vector2 scrollPosition;

        private YooAssetLocalPublishSettings Settings =>
            YooAssetLocalPublishSettings.instance;


        // ============================================================
        // Menu
        // ============================================================

        [MenuItem(
            "Tools/HotUpdate/本地推送测试")]
        private static void OpenWindow()
        {
            var window =
                GetWindow<YooAssetLocalPublishWindow>();

            window.titleContent =
                new GUIContent("YooAsset Publish");

            window.minSize =
                new Vector2(700, 500);

            window.Show();
        }


        // ============================================================
        // GUI
        // ============================================================

        private void OnGUI()
        {
            scrollPosition =
                EditorGUILayout.BeginScrollView(
                    scrollPosition);

            DrawTitle();

            EditorGUILayout.Space(10);

            DrawPackage();

            EditorGUILayout.Space(10);

            DrawSourceDirectory();

            EditorGUILayout.Space(10);

            DrawServerDirectory();

            EditorGUILayout.Space(10);

            DrawOptions();

            EditorGUILayout.Space(20);

            DrawSourceInfo();

            EditorGUILayout.Space(20);

            DrawPublishButton();

            EditorGUILayout.EndScrollView();
        }


        // ============================================================
        // 标题
        // ============================================================

        private static void DrawTitle()
        {
            EditorGUILayout.LabelField(
                "YooAsset Local Server Publisher",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "将 YooAsset 构建产物复制到本地 Nginx/CDN 资源目录。" +
                "\n\n为了避免客户端在资源尚未复制完成时读取到新版本，" +
                "Package.version 文件会最后发布。",
                MessageType.Info);
        }


        // ============================================================
        // Package
        // ============================================================

        private void DrawPackage()
        {
            EditorGUILayout.LabelField(
                "Package",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            var packageName =
                EditorGUILayout.TextField(
                    "Package Name",
                    Settings.PackageName);

            if (EditorGUI.EndChangeCheck())
            {
                Settings.PackageName =
                    packageName.Trim();

                Settings.SaveSettings();
            }
        }


        // ============================================================
        // Source
        // ============================================================

        private void DrawSourceDirectory()
        {
            EditorGUILayout.LabelField(
                "YooAsset 构建产物",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            var newPath =
                EditorGUILayout.TextField(
                    "Source",
                    Settings.SourceDirectory);

            if (EditorGUI.EndChangeCheck())
            {
                Settings.SourceDirectory =
                    NormalizePath(newPath);

                Settings.SaveSettings();
            }

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
                RevealDirectory(
                    Settings.SourceDirectory);
            }

            EditorGUILayout.EndHorizontal();


            if (!Directory.Exists(
                    Settings.SourceDirectory))
            {
                EditorGUILayout.HelpBox(
                    "YooAsset 构建目录不存在。",
                    MessageType.Warning);
            }
        }


        // ============================================================
        // Server
        // ============================================================

        private void DrawServerDirectory()
        {
            EditorGUILayout.LabelField(
                "本地服务器目录",
                EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            var newPath =
                EditorGUILayout.TextField(
                    "Server",
                    Settings.ServerDirectory);

            if (EditorGUI.EndChangeCheck())
            {
                Settings.ServerDirectory =
                    NormalizePath(newPath);

                Settings.SaveSettings();
            }

            if (GUILayout.Button(
                    "选择",
                    GUILayout.Width(60)))
            {
                SelectServerDirectory();
            }

            if (GUILayout.Button(
                    "打开",
                    GUILayout.Width(60)))
            {
                RevealDirectory(
                    Settings.ServerDirectory);
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.HelpBox(
                "这里填写服务器的磁盘目录，而不是 HTTP 地址。\n\n" +
                "例如：\n" +
                "D:/YooCDN/CDN/PC/v1.3\n\n" +
                "Nginx 再把这个目录映射为：\n" +
                "http://192.168.100.72:159/CDN/PC/v1.3",
                MessageType.None);
        }


        // ============================================================
        // Options
        // ============================================================

        private void DrawOptions()
        {
            EditorGUILayout.LabelField(
                "发布选项",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            var overwrite =
                EditorGUILayout.Toggle(
                    "覆盖已有文件",
                    Settings.OverwriteExisting);

            var reveal =
                EditorGUILayout.Toggle(
                    "发布后打开目录",
                    Settings.RevealAfterPublish);

            if (EditorGUI.EndChangeCheck())
            {
                Settings.OverwriteExisting =
                    overwrite;

                Settings.RevealAfterPublish =
                    reveal;

                Settings.SaveSettings();
            }


            EditorGUILayout.HelpBox(
                "建议不要在每次发布前清空服务器目录。" +
                "\nHashName 模式下，旧 Bundle 可能仍被历史 Manifest 使用。" +
                "\n后续应该通过专门的服务器资源清理工具删除废弃 Bundle。",
                MessageType.Warning);
        }


        // ============================================================
        // Source Info
        // ============================================================

        private void DrawSourceInfo()
        {
            EditorGUILayout.LabelField(
                "构建产物检查",
                EditorStyles.boldLabel);

            if (!Directory.Exists(
                    Settings.SourceDirectory))
            {
                return;
            }


            var files =
                Directory.GetFiles(
                    Settings.SourceDirectory,
                    "*",
                    SearchOption.AllDirectories);

            var totalBytes =
                files.Sum(x =>
                {
                    try
                    {
                        return new FileInfo(x).Length;
                    }
                    catch
                    {
                        return 0L;
                    }
                });


            EditorGUILayout.LabelField(
                "文件数量",
                files.Length.ToString());

            EditorGUILayout.LabelField(
                "总大小",
                FormatFileSize(totalBytes));


            var versionPath =
                FindVersionFile();

            if (string.IsNullOrEmpty(versionPath))
            {
                EditorGUILayout.HelpBox(
                    $"没有找到 {Settings.PackageName}.version",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField(
                    "Version File",
                    versionPath);

                try
                {
                    var packageVersion =
                        File.ReadAllText(versionPath)
                            .Trim();

                    EditorGUILayout.LabelField(
                        "Package Version",
                        packageVersion);
                }
                catch (Exception)
                {
                    // 不影响发布。
                }
            }
        }


        // ============================================================
        // Publish Button
        // ============================================================

        private void DrawPublishButton()
        {
            var canPublish =
                Directory.Exists(
                    Settings.SourceDirectory)
                &&
                !string.IsNullOrWhiteSpace(
                    Settings.ServerDirectory);


            using (new EditorGUI.DisabledScope(
                       !canPublish))
            {
                var oldColor =
                    GUI.backgroundColor;

                GUI.backgroundColor =
                    new Color(
                        0.35f,
                        0.8f,
                        0.45f);


                if (GUILayout.Button(
                        "发布到本地服务器",
                        GUILayout.Height(45)))
                {
                    Publish();
                }


                GUI.backgroundColor =
                    oldColor;
            }
        }


        // ============================================================
        // Publish
        // ============================================================

        private void Publish()
        {
            var source =
                NormalizePath(
                    Settings.SourceDirectory);

            var target =
                NormalizePath(
                    Settings.ServerDirectory);


            if (!Directory.Exists(source))
            {
                EditorUtility.DisplayDialog(
                    "发布失败",
                    "构建产物目录不存在：\n" +
                    source,
                    "确定");

                return;
            }


            Directory.CreateDirectory(target);


            // --------------------------------------------------------
            // 获取所有需要发布的文件。
            // --------------------------------------------------------

            var allFiles =
                Directory.GetFiles(
                    source,
                    "*",
                    SearchOption.AllDirectories)
                .Where(x =>
                    !x.EndsWith(
                        ".meta",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();


            // --------------------------------------------------------
            // 找 Package.version。
            //
            // 这个文件必须最后复制。
            // --------------------------------------------------------

            var versionFileName =
                Settings.PackageName + ".version";


            var versionFiles =
                allFiles
                    .Where(x =>
                        string.Equals(
                            Path.GetFileName(x),
                            versionFileName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();


            // 普通资源
            var normalFiles =
                allFiles
                    .Except(versionFiles)
                    .ToList();


            var copiedFiles = 0;
            long copiedBytes = 0;


            try
            {
                // ====================================================
                // ① 先发布普通文件
                // ====================================================

                for (var i = 0;
                     i < normalFiles.Count;
                     i++)
                {
                    var file =
                        normalFiles[i];


                    var progress =
                        normalFiles.Count == 0
                            ? 1f
                            : (float)i /
                              normalFiles.Count;


                    EditorUtility.DisplayProgressBar(
                        "YooAsset 发布",
                        "复制：" +
                        Path.GetFileName(file),
                        progress * 0.9f);


                    CopyFile(
                        source,
                        target,
                        file,
                        ref copiedFiles,
                        ref copiedBytes);
                }


                // ====================================================
                // ② 最后发布 Package.version
                // ====================================================

                foreach (var versionFile in versionFiles)
                {
                    EditorUtility.DisplayProgressBar(
                        "YooAsset 发布",
                        "最后发布：" +
                        Path.GetFileName(versionFile),
                        0.95f);


                    CopyFile(
                        source,
                        target,
                        versionFile,
                        ref copiedFiles,
                        ref copiedBytes);
                }


                EditorUtility.ClearProgressBar();


                var packageVersion =
                    GetPackageVersion(
                        versionFiles);


                Debug.Log(
                    "[Museum.YooAsset] 本地服务器发布完成。" +
                    $"\nPackage       : {Settings.PackageName}" +
                    $"\nVersion       : {packageVersion}" +
                    $"\nSource        : {source}" +
                    $"\nServer        : {target}" +
                    $"\nCopied Files  : {copiedFiles}" +
                    $"\nCopied Size   : {FormatFileSize(copiedBytes)}");


                EditorUtility.DisplayDialog(
                    "发布完成",
                    $"Package：{Settings.PackageName}\n" +
                    $"Version：{packageVersion}\n\n" +
                    $"文件数量：{copiedFiles}\n" +
                    $"总大小：{FormatFileSize(copiedBytes)}\n\n" +
                    $"服务器目录：\n{target}",
                    "确定");


                if (Settings.RevealAfterPublish)
                {
                    RevealDirectory(target);
                }
            }
            catch (Exception exception)
            {
                EditorUtility.ClearProgressBar();

                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "发布失败",
                    exception.Message,
                    "确定");
            }
        }


        // ============================================================
        // Copy File
        // ============================================================

        private void CopyFile(
            string sourceRoot,
            string targetRoot,
            string sourceFile,
            ref int copiedFiles,
            ref long copiedBytes)
        {
            // 获得文件相对于 Source 的路径。
            //
            // 例如：
            //
            // Source:
            // D:/Bundles/Publish
            //
            // File:
            // D:/Bundles/Publish/abc.bundle
            //
            // Relative:
            // abc.bundle

            var relativePath =
                GetRelativePath(
                    sourceRoot,
                    sourceFile);


            var targetFile =
                NormalizePath(
                    Path.Combine(
                        targetRoot,
                        relativePath));


            var targetDirectory =
                Path.GetDirectoryName(
                    targetFile);


            if (!string.IsNullOrEmpty(
                    targetDirectory))
            {
                Directory.CreateDirectory(
                    targetDirectory);
            }


            // 不允许覆盖时，
            // 如果目标文件已经存在则跳过。
            if (!Settings.OverwriteExisting &&
                File.Exists(targetFile))
            {
                return;
            }


            File.Copy(
                sourceFile,
                targetFile,
                overwrite:
                    Settings.OverwriteExisting);


            copiedFiles++;

            copiedBytes +=
                new FileInfo(sourceFile).Length;
        }


        // ============================================================
        // Find Version
        // ============================================================

        private string FindVersionFile()
        {
            if (!Directory.Exists(
                    Settings.SourceDirectory))
            {
                return string.Empty;
            }


            var versionName =
                Settings.PackageName +
                ".version";


            return Directory
                       .GetFiles(
                           Settings.SourceDirectory,
                           versionName,
                           SearchOption.AllDirectories)
                       .FirstOrDefault()
                   ?? string.Empty;
        }


        private static string GetPackageVersion(
            IReadOnlyList<string> versionFiles)
        {
            if (versionFiles == null ||
                versionFiles.Count == 0)
            {
                return "Unknown";
            }


            try
            {
                return File
                    .ReadAllText(
                        versionFiles[0])
                    .Trim();
            }
            catch
            {
                return "Unknown";
            }
        }


        // ============================================================
        // Folder Select
        // ============================================================

        private void SelectSourceDirectory()
        {
            var path =
                EditorUtility.OpenFolderPanel(
                    "选择 YooAsset 构建产物目录",
                    Settings.SourceDirectory,
                    string.Empty);


            if (string.IsNullOrEmpty(path))
            {
                return;
            }


            Settings.SourceDirectory =
                NormalizePath(path);

            Settings.SaveSettings();
        }


        private void SelectServerDirectory()
        {
            var path =
                EditorUtility.OpenFolderPanel(
                    "选择本地服务器目录",
                    Settings.ServerDirectory,
                    string.Empty);


            if (string.IsNullOrEmpty(path))
            {
                return;
            }


            Settings.ServerDirectory =
                NormalizePath(path);

            Settings.SaveSettings();
        }


        // ============================================================
        // Path
        // ============================================================

        private static string GetRelativePath(
            string root,
            string file)
        {
            root =
                NormalizePath(root)
                    .TrimEnd('/')
                + "/";


            file =
                NormalizePath(file);


            var rootUri =
                new Uri(root);

            var fileUri =
                new Uri(file);


            var relativeUri =
                rootUri.MakeRelativeUri(
                    fileUri);


            return Uri
                .UnescapeDataString(
                    relativeUri.ToString())
                .Replace('\\', '/');
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


        private static void RevealDirectory(
            string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }


            if (!Directory.Exists(directory))
            {
                return;
            }


            EditorUtility.RevealInFinder(
                directory);
        }


        // ============================================================
        // File Size
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
                return
                    $"{bytes / 1024f:F1} KB";
            }


            if (bytes <
                1024L * 1024L * 1024L)
            {
                return
                    $"{bytes / 1024f / 1024f:F2} MB";
            }


            return
                $"{bytes / 1024f / 1024f / 1024f:F2} GB";
        }
    }
}

#endif