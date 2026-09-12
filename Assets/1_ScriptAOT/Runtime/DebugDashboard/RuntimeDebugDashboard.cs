using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using YooAsset;

namespace AOT.Debugging
{
    /// <summary>
    /// 可直接放进场景的发布版运行时调试看板。
    /// 使用 IMGUI 绘制，不依赖 Canvas、TMP 或热更新程序集。
    /// 默认收起，PC 可按 F8，移动端可点击右上角 DBG 按钮。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Museum/Debug/Runtime Debug Dashboard")]
    public sealed class RuntimeDebugDashboard : MonoBehaviour
    {
        private sealed class LogEntry
        {
            public string Time;
            public string Message;
            public string StackTrace;
            public LogType Type;
        }

        [Header("显示")]
        [Tooltip("启动时是否直接展开。关闭时仍会显示右上角 DBG 小按钮。")]
        [SerializeField] private bool showOnStart;
        [Tooltip("PC 发布包内打开/关闭看板的快捷键。")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F8;
        [Tooltip("切换场景时保留看板。多个场景重复放置时会自动销毁后创建的副本。")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("YooAsset")]
        [Tooltip("需要显示运行状态的 YooAsset 资源包名称。")]
        [SerializeField] private string packageName = "DefaultPackage";

        [Header("日志")]
        [Tooltip("内存中最多保留多少条运行日志。")]
        [Range(20, 1000)]
        [SerializeField] private int maxLogEntries = 200;
        [Tooltip("关闭后只记录 Warning、Error、Exception 和 Assert。")]
        [SerializeField] private bool captureNormalLogs = true;

        private static RuntimeDebugDashboard instance;
        private readonly object logLock = new object();
        private readonly List<LogEntry> logs = new List<LogEntry>();

        private Rect windowRect;
        private Vector2 overviewScroll;
        private Vector2 logScroll;
        private bool isVisible;
        private int selectedTab;
        private int selectedLogFilter;
        private float smoothedDeltaTime;
        private float nextReportRefreshTime;
        private string cachedReport = string.Empty;

        private GUIStyle reportStyle;
        private GUIStyle logStyle;
        private GUIStyle warningStyle;
        private GUIStyle errorStyle;

        public bool IsVisible
        {
            get { return isVisible; }
        }

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                if (instance != null && instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                instance = this;
                DontDestroyOnLoad(gameObject);
            }

            isVisible = showOnStart;
            ResetWindowRect();
            RefreshReport();
        }

        private void OnEnable()
        {
            Application.logMessageReceivedThreaded += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleLog;
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            if (deltaTime > 0f)
            {
                smoothedDeltaTime = smoothedDeltaTime <= 0f
                    ? deltaTime
                    : Mathf.Lerp(smoothedDeltaTime, deltaTime, 0.1f);
            }

            if (Time.unscaledTime >= nextReportRefreshTime)
            {
                RefreshReport();
                nextReportRefreshTime = Time.unscaledTime + 1f;
            }
        }

        private void OnGUI()
        {
            HandleKeyboardShortcut(Event.current);
            EnsureStyles();

            var buttonWidth = Mathf.Clamp(Screen.width * 0.09f, 64f, 96f);
            var buttonHeight = Mathf.Clamp(Screen.height * 0.055f, 36f, 54f);
            var toggleRect = new Rect(
                Screen.width - buttonWidth - 10f,
                10f,
                buttonWidth,
                buttonHeight);

            if (!isVisible)
            {
                if (GUI.Button(toggleRect, "DBG"))
                {
                    isVisible = true;
                    KeepWindowOnScreen();
                }
                return;
            }

            windowRect = GUI.Window(
                GetInstanceID(),
                windowRect,
                DrawWindow,
                "Runtime Debug Dashboard");

            if (GUI.Button(toggleRect, "关闭"))
                isVisible = false;
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();
            selectedTab = GUILayout.Toolbar(
                selectedTab,
                new[] { "运行信息", "运行日志" });

            if (selectedTab == 0)
                DrawOverview();
            else
                DrawLogs();

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("复制全部信息", GUILayout.Height(34f)))
                GUIUtility.systemCopyBuffer = BuildFullReport();

            if (GUILayout.Button("重置位置", GUILayout.Height(34f)))
                ResetWindowRect();

            if (GUILayout.Button("收起", GUILayout.Height(34f)))
                isVisible = false;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 90f, 28f));
        }

        private void DrawOverview()
        {
            overviewScroll = GUILayout.BeginScrollView(overviewScroll);
            GUILayout.TextArea(cachedReport, reportStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
        }

        private void DrawLogs()
        {
            selectedLogFilter = GUILayout.Toolbar(
                selectedLogFilter,
                new[] { "全部", "警告", "错误" });

            GUILayout.BeginHorizontal();
            GUILayout.Label("已缓存：" + GetLogCount());

            if (GUILayout.Button("复制日志", GUILayout.Width(100f)))
                GUIUtility.systemCopyBuffer = BuildLogReport();

            if (GUILayout.Button("清空", GUILayout.Width(80f)))
                ClearLogs();

            GUILayout.EndHorizontal();

            var snapshot = GetLogSnapshot();
            logScroll = GUILayout.BeginScrollView(logScroll);

            for (var i = 0; i < snapshot.Length; i++)
            {
                var entry = snapshot[i];
                if (!PassesFilter(entry.Type))
                    continue;

                var style = GetLogStyle(entry.Type);
                var message = "[" + entry.Time + "] [" + entry.Type + "] " + entry.Message;
                if (!string.IsNullOrWhiteSpace(entry.StackTrace) &&
                    entry.Type != LogType.Log)
                {
                    message += "\n" + entry.StackTrace;
                }

                GUILayout.TextArea(message, style);
            }

            GUILayout.EndScrollView();
        }

        private void HandleKeyboardShortcut(Event currentEvent)
        {
            // 使用 IMGUI 键盘事件，兼容旧输入系统和仅启用新输入系统的项目。
            if (currentEvent == null ||
                currentEvent.type != EventType.KeyDown ||
                currentEvent.keyCode != toggleKey)
            {
                return;
            }

            isVisible = !isVisible;
            if (isVisible)
                KeepWindowOnScreen();

            currentEvent.Use();
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (!captureNormalLogs && type == LogType.Log)
                return;

            var entry = new LogEntry
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Message = condition ?? string.Empty,
                StackTrace = stackTrace ?? string.Empty,
                Type = type
            };

            lock (logLock)
            {
                logs.Add(entry);
                var limit = Mathf.Max(20, maxLogEntries);
                var excess = logs.Count - limit;
                if (excess > 0)
                    logs.RemoveRange(0, excess);
            }
        }

        private void RefreshReport()
        {
            var builder = new StringBuilder(2048);
            var fps = smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;

            AppendSection(builder, "运行");
            AppendLine(builder, "FPS", fps.ToString("F1"));
            AppendLine(builder, "运行时长", FormatDuration(Time.realtimeSinceStartup));
            AppendLine(builder, "当前场景", SceneManager.GetActiveScene().name);
            AppendLine(builder, "Time Scale", Time.timeScale.ToString("F2"));
            AppendLine(builder, "本地时间", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            AppendSection(builder, "应用");
            AppendLine(builder, "产品名称", Application.productName);
            AppendLine(builder, "应用版本", Application.version);
            AppendLine(builder, "Unity", Application.unityVersion);
            AppendLine(builder, "平台", Application.platform.ToString());
            AppendLine(builder, "语言", Application.systemLanguage.ToString());
            AppendLine(builder, "Development Build", Debug.isDebugBuild.ToString());

            AppendSection(builder, "资源与网络");
            AppendLine(builder, "网络状态", Application.internetReachability.ToString());
            AppendLine(builder, "YooAsset 包", packageName);
            AppendLine(builder, "YooAsset 状态", GetYooAssetStatus());
            AppendLine(
                builder,
                "上次完整版本",
                PlayerPrefs.GetString(
                    "Museum.YooAsset.LastVerifiedVersion." + packageName,
                    "无记录"));

            AppendSection(builder, "设备");
            AppendLine(builder, "设备型号", SystemInfo.deviceModel);
            AppendLine(builder, "操作系统", SystemInfo.operatingSystem);
            AppendLine(builder, "CPU", SystemInfo.processorType);
            AppendLine(builder, "CPU 核心", SystemInfo.processorCount.ToString());
            AppendLine(builder, "系统内存", SystemInfo.systemMemorySize + " MB");
            AppendLine(builder, "GPU", SystemInfo.graphicsDeviceName);
            AppendLine(builder, "显存", SystemInfo.graphicsMemorySize + " MB");
            AppendLine(builder, "图形 API", SystemInfo.graphicsDeviceType.ToString());

            AppendSection(builder, "显示与内存");
            AppendLine(
                builder,
                "分辨率",
                Screen.width + " x " + Screen.height +
                " @" + Screen.currentResolution.refreshRateRatio.value.ToString("F1") + "Hz");
            AppendLine(builder, "DPI", Screen.dpi.ToString("F1"));
            AppendLine(builder, "全屏", Screen.fullScreen.ToString());
            AppendLine(builder, "Unity 已分配", FormatBytes(Profiler.GetTotalAllocatedMemoryLong()));
            AppendLine(builder, "Unity 保留", FormatBytes(Profiler.GetTotalReservedMemoryLong()));
            AppendLine(builder, "GC 托管内存", FormatBytes(GC.GetTotalMemory(false)));
            AppendLine(builder, "电量", GetBatteryText());

            cachedReport = builder.ToString();
        }

        private string GetYooAssetStatus()
        {
            try
            {
                if (!YooAssets.IsInitialized)
                    return "未初始化";

                ResourcePackage package;
                if (!YooAssets.TryGetPackage(packageName, out package) || package == null)
                    return "未找到资源包";

                var version = package.GetPackageVersion();
                if (string.IsNullOrWhiteSpace(version))
                    version = "Manifest 未加载";

                return package.InitializeStatus + " / " + version;
            }
            catch (Exception exception)
            {
                return "读取失败：" + exception.Message;
            }
        }

        private string BuildFullReport()
        {
            return cachedReport + "\n========== 运行日志 ==========\n" + BuildLogReport();
        }

        private string BuildLogReport()
        {
            var builder = new StringBuilder();
            var snapshot = GetLogSnapshot();

            for (var i = 0; i < snapshot.Length; i++)
            {
                var entry = snapshot[i];
                if (!PassesFilter(entry.Type))
                    continue;

                builder.Append('[')
                    .Append(entry.Time)
                    .Append("] [")
                    .Append(entry.Type)
                    .Append("] ")
                    .AppendLine(entry.Message);

                if (!string.IsNullOrWhiteSpace(entry.StackTrace) &&
                    entry.Type != LogType.Log)
                {
                    builder.AppendLine(entry.StackTrace);
                }
            }

            return builder.ToString();
        }

        private LogEntry[] GetLogSnapshot()
        {
            lock (logLock)
                return logs.ToArray();
        }

        private int GetLogCount()
        {
            lock (logLock)
                return logs.Count;
        }

        private void ClearLogs()
        {
            lock (logLock)
                logs.Clear();
        }

        private bool PassesFilter(LogType type)
        {
            if (selectedLogFilter == 0)
                return true;
            if (selectedLogFilter == 1)
                return type == LogType.Warning;

            return type == LogType.Error ||
                   type == LogType.Exception ||
                   type == LogType.Assert;
        }

        private GUIStyle GetLogStyle(LogType type)
        {
            if (type == LogType.Warning)
                return warningStyle;
            if (type == LogType.Error ||
                type == LogType.Exception ||
                type == LogType.Assert)
            {
                return errorStyle;
            }
            return logStyle;
        }

        private void EnsureStyles()
        {
            if (reportStyle != null)
                return;

            reportStyle = new GUIStyle(GUI.skin.textArea)
            {
                wordWrap = true,
                fontSize = ResolveFontSize(),
                richText = false
            };

            logStyle = new GUIStyle(reportStyle);
            warningStyle = new GUIStyle(reportStyle);
            errorStyle = new GUIStyle(reportStyle);
            warningStyle.normal.textColor = new Color(1f, 0.75f, 0.2f);
            errorStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
        }

        private int ResolveFontSize()
        {
            return Mathf.Clamp(
                Mathf.RoundToInt(Screen.dpi > 0f ? Screen.dpi / 12f : 16f),
                14,
                24);
        }

        private void ResetWindowRect()
        {
            var width = Mathf.Clamp(Screen.width - 40f, 360f, 760f);
            var height = Mathf.Clamp(Screen.height - 60f, 360f, 900f);
            windowRect = new Rect(20f, 20f, width, height);
            KeepWindowOnScreen();
        }

        private void KeepWindowOnScreen()
        {
            var maxX = Mathf.Max(0f, Screen.width - windowRect.width);
            var maxY = Mathf.Max(0f, Screen.height - windowRect.height);
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, maxX);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, maxY);
        }

        private static void AppendSection(StringBuilder builder, string title)
        {
            if (builder.Length > 0)
                builder.AppendLine();

            builder.Append("========== ")
                .Append(title)
                .AppendLine(" ==========");
        }

        private static void AppendLine(
            StringBuilder builder,
            string name,
            string value)
        {
            builder.Append(name)
                .Append("：")
                .AppendLine(string.IsNullOrWhiteSpace(value) ? "-" : value);
        }

        private static string FormatDuration(float seconds)
        {
            var duration = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
            return string.Format(
                "{0:00}:{1:00}:{2:00}",
                (int)duration.TotalHours,
                duration.Minutes,
                duration.Seconds);
        }

        private static string FormatBytes(long bytes)
        {
            const float megabyte = 1024f * 1024f;
            return (bytes / megabyte).ToString("F1") + " MB";
        }

        private static string GetBatteryText()
        {
            if (SystemInfo.batteryLevel < 0f)
                return "不可用";

            return (SystemInfo.batteryLevel * 100f).ToString("F0") +
                   "% / " +
                   SystemInfo.batteryStatus;
        }

        [ContextMenu("打开/关闭看板")]
        private void ToggleDashboard()
        {
            isVisible = !isVisible;
        }
    }
}
