using AOT.HotUpdate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YooAsset.Editor;

namespace Museum.Infrastructure.EditorTools
{
    /// <summary>
    /// 生成可直接运行的更新场景和 YooAsset EditorSimulate 配置。
    ///
    /// 该工具只维护名为 DefaultPackage/MuseumStartup 的配置，
    /// 不清空团队已经添加的其他 YooAsset Package 或分组。
    /// 可以重复执行，用于场景被误改后的快速恢复。
    /// </summary>
    public static class UpdateSceneBuilder
    {
        private const string CollectorGroupName =
            "MuseumStartup";

        private const string ContentRoot =
            "Assets/3_ResourceFile";
        private const string ProbeAssetPath =
            ContentRoot + "/UpdateProbe.txt";

        private const string DemoRoot =
            "Assets/Scenes";
        private const string RuntimeConfigPath =
            DemoRoot + "/YooAssetRuntimeConfig.asset";
        private const string HotUpdateSettingsPath =
            DemoRoot + "/HotUpdateLoadSettings.asset";
        private const string UpdateScenePath =
            DemoRoot + "/UpdateScene.unity";
        private const string MainScenePath =
            "Assets/3_ResourceFile/Scenes/museum.unity";

        [MenuItem("Museum/Update/Build Editor Demo")]
        public static void BuildEditorDemo()
        {
            EnsureFolder(ContentRoot);
            EnsureFolder(DemoRoot);

            CreateProbeAsset();

            var runtimeConfig =
                GetOrCreateAsset<YooAssetRuntimeConfig>(
                    RuntimeConfigPath);

            var hotUpdateSettings =
                GetOrCreateAsset<HotUpdateLoadSettings>(
                    HotUpdateSettingsPath);

            ConfigureYooAssetCollector(runtimeConfig.PackageName);
            CreateUpdateScene(
                runtimeConfig,
                hotUpdateSettings);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var sceneAsset =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    UpdateScenePath);
            Selection.activeObject = sceneAsset;
            EditorGUIUtility.PingObject(sceneAsset);

            Debug.Log(
                "[Museum.Update] Editor demo generated. " +
                "Open UpdateScene and press Play.");
        }

        private static void CreateProbeAsset()
        {
            File.WriteAllText(
                ProbeAssetPath,
                "Museum YooAsset EditorSimulate probe v1");

            AssetDatabase.ImportAsset(
                ProbeAssetPath,
                ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// 只添加或修正本项目负责的收集器，保留其他团队配置。
        /// </summary>
        internal static void ConfigureYooAssetCollector(
            string packageName)
        {
            var setting =
                BundleCollectorSettingData.Setting;

            var package = setting.Packages
                .FirstOrDefault(item =>
                    string.Equals(
                        item.PackageName,
                        packageName,
                        StringComparison.Ordinal));

            if (package == null)
            {
                package =
                    BundleCollectorSettingData
                        .CreatePackage(packageName);
            }

            package.PackageDesc =
                "Museum runtime content and hot update files.";
            package.EnableAddressable = true;
            package.SupportExtensionless = true;
            package.AutoCollectShaders = true;
            package.IgnoreRuleName =
                nameof(NormalIgnoreRule);
            BundleCollectorSettingData.ModifyPackage(
                package);

            var group = package.Groups
                .FirstOrDefault(item =>
                    string.Equals(
                        item.GroupName,
                        CollectorGroupName,
                        StringComparison.Ordinal));

            // 当前项目已经按 AOT/HotUpdate、Points、Common 分组收集
            // Assets/3_ResourceFile。旧的 MuseumStartup 根目录收集器会与这些
            // 团队配置重复收集相同资源，因此迁移时只移除本工具拥有的旧组，
            // 不修改任何其他组或收集器。
            if (group != null)
            {
                BundleCollectorSettingData.RemoveGroup(package, group);
            }
            BundleCollectorSettingData.FixFile();
            BundleCollectorSettingData.SaveFile();
        }

        private static void CreateUpdateScene(
            YooAssetRuntimeConfig runtimeConfig,
            HotUpdateLoadSettings hotUpdateSettings)
        {
            var scene =
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

            var root = new GameObject(
                "Update App");

            var bootstrap =
                root.AddComponent<UpdateSceneBootstrap>();
            bootstrap.Configure(
                runtimeConfig,
                hotUpdateSettings,
                "UpdateProbe");

            CreateEventSystem();
            var canvas = CreateCanvas();

            var background =
                CreateUiObject(
                    "Background",
                    canvas.transform);
            Stretch(background);
            background.gameObject
                .AddComponent<Image>().color =
                new Color(
                    0.025f,
                    0.04f,
                    0.07f,
                    1f);

            var panel =
                CreateUiObject(
                    "Update Panel",
                    background);
            panel.anchorMin =
                new Vector2(0.5f, 0.5f);
            panel.anchorMax =
                new Vector2(0.5f, 0.5f);
            panel.pivot =
                new Vector2(0.5f, 0.5f);
            panel.sizeDelta =
                new Vector2(900f, 570f);
            panel.gameObject
                .AddComponent<Image>().color =
                new Color(
                    0.07f,
                    0.10f,
                    0.16f,
                    0.98f);

            var title = CreateText(
                "Title",
                panel,
                "Museum Resource Update",
                42,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            SetRect(
                title.rectTransform,
                new Vector2(0.08f, 0.80f),
                new Vector2(0.92f, 0.96f));

            var status = CreateText(
                "Status",
                panel,
                "等待初始化资源系统。",
                28,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
            status.color =
                new Color(0.88f, 0.92f, 1f, 1f);
            SetRect(
                status.rectTransform,
                new Vector2(0.08f, 0.54f),
                new Vector2(0.92f, 0.80f));

            var version = CreateText(
                "Version",
                panel,
                "Package: checking...",
                20,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
            version.color =
                new Color(0.55f, 0.68f, 0.82f, 1f);
            SetRect(
                version.rectTransform,
                new Vector2(0.08f, 0.46f),
                new Vector2(0.92f, 0.54f));

            var slider =
                CreateProgressSlider(
                    panel);
            SetRect(
                slider.GetComponent<RectTransform>(),
                new Vector2(0.12f, 0.36f),
                new Vector2(0.88f, 0.42f));

            var progress = CreateText(
                "Progress",
                panel,
                "0%    0 B / 0 B",
                20,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
            progress.color =
                new Color(0.65f, 0.78f, 0.88f, 1f);
            SetRect(
                progress.rectTransform,
                new Vector2(0.08f, 0.24f),
                new Vector2(0.92f, 0.35f));

            var download = CreateButton(
                "Download Button",
                panel,
                "确认下载",
                new Color(
                    0.08f, 0.58f, 0.76f, 1f));
            SetRect(
                download.GetComponent<RectTransform>(),
                new Vector2(0.09f, 0.07f),
                new Vector2(0.36f, 0.20f));

            var retry = CreateButton(
                "Retry Button",
                panel,
                "重试",
                new Color(
                    0.80f, 0.36f, 0.18f, 1f));
            SetRect(
                retry.GetComponent<RectTransform>(),
                new Vector2(0.365f, 0.07f),
                new Vector2(0.635f, 0.20f));

            var enter = CreateButton(
                "Enter MR Button",
                panel,
                "进入 MR",
                new Color(
                    0.16f, 0.68f, 0.42f, 1f));
            SetRect(
                enter.GetComponent<RectTransform>(),
                new Vector2(0.64f, 0.07f),
                new Vector2(0.91f, 0.20f));

            var view =
                panel.gameObject
                    .AddComponent<UpdateSceneView>();
            view.Configure(
                bootstrap,
                status,
                progress,
                version,
                slider,
                download,
                retry,
                enter);

            EditorSceneManager.SaveScene(
                scene,
                UpdateScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            var result =
                new List<EditorBuildSettingsScene>
                {
                    new EditorBuildSettingsScene(
                        UpdateScenePath,
                        true)
                };

            foreach (var existing in
                     EditorBuildSettings.scenes)
            {
                if (string.Equals(
                        existing.path,
                        UpdateScenePath,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        existing.path,
                        MainScenePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(existing);
            }

            EditorBuildSettings.scenes =
                result.ToArray();
        }

        private static T GetOrCreateAsset<T>(
            string path)
            where T : ScriptableObject
        {
            var asset =
                AssetDatabase.LoadAssetAtPath<T>(
                    path);

            if (asset != null)
                return asset;

            asset =
                ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(
                asset,
                path);
            return asset;
        }

        private static void CreateEventSystem()
        {
            var eventSystem =
                new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<
                StandaloneInputModule>();
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject =
                new GameObject("Canvas");
            var canvas =
                canvasObject.AddComponent<Canvas>();
            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            var scaler =
                canvasObject
                    .AddComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode
                    .ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<
                GraphicRaycaster>();
            return canvas;
        }

        private static RectTransform CreateUiObject(
            string name,
            Transform parent)
        {
            var gameObject =
                new GameObject(
                    name,
                    typeof(RectTransform));
            var rect =
                gameObject.GetComponent<
                    RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            var rect =
                CreateUiObject(name, parent);
            var text =
                rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            text.verticalOverflow =
                VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Slider CreateProgressSlider(
            Transform parent)
        {
            var rect =
                CreateUiObject(
                    "Progress Slider",
                    parent);

            var background =
                rect.gameObject.AddComponent<Image>();
            background.color =
                new Color(
                    0.13f, 0.18f, 0.25f, 1f);

            var fillArea =
                CreateUiObject(
                    "Fill Area",
                    rect);
            Stretch(fillArea);
            fillArea.offsetMin =
                new Vector2(6f, 6f);
            fillArea.offsetMax =
                new Vector2(-6f, -6f);

            var fill =
                CreateUiObject(
                    "Fill",
                    fillArea);
            Stretch(fill);
            var fillImage =
                fill.gameObject.AddComponent<Image>();
            fillImage.color =
                new Color(
                    0.10f, 0.68f, 0.88f, 1f);

            var slider =
                rect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.direction =
                Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            return slider;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Color color)
        {
            var rect =
                CreateUiObject(name, parent);
            var image =
                rect.gameObject.AddComponent<Image>();
            image.color = color;

            var button =
                rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(
                "Label",
                rect,
                label,
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);

            return button;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(
            RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(
            string folderPath)
        {
            var segments =
                folderPath.Split('/');
            var current = segments[0];

            for (var index = 1;
                 index < segments.Length;
                 index++)
            {
                var next =
                    current + "/" + segments[index];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        segments[index]);
                }

                current = next;
            }
        }
    }
}
