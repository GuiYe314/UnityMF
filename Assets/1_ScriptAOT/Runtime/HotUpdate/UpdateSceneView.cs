using AOT.HotUpdate.Framework;
using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace AOT.HotUpdate
{
    /// <summary>
    /// 更新场景的纯视图适配器。
    /// 它只读取 ResourceUpdateSnapshot，并把按钮操作转发给
    /// UpdateSceneBootstrap，不直接接触 YooAsset 或 HybridCLR API。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UpdateSceneView : MonoBehaviour
    {
        [Header("流程")]
        [SerializeField] private UpdateSceneBootstrap bootstrap;
        [FormerlySerializedAs("autoEnterMainScene")]
        [SerializeField] private bool autoEnterMrFeature;

        [Header("显示")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text versionText;
        [SerializeField] private Slider progressSlider;

        [Header("按钮")]
        [SerializeField] private Button downloadButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button enterButton;

        private bool subscribed;

        /// <summary>
        /// 供 UpdateSceneBuilder 自动完成场景引用。
        /// </summary>
        public void Configure(
            UpdateSceneBootstrap flow,
            Text status,
            Text progress,
            Text version,
            Slider slider,
            Button download,
            Button retry,
            Button enter)
        {
            bootstrap = flow;
            statusText = status;
            progressText = progress;
            versionText = version;
            progressSlider = slider;
            downloadButton = download;
            retryButton = retry;
            enterButton = enter;
        }

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            bootstrap.SnapshotChanged += OnSnapshotChanged;
            bootstrap.HotUpdateStarted +=
                OnHotUpdateStarted;
            bootstrap.FlowFailed += OnFlowFailed;
            subscribed = true;

            downloadButton.onClick.AddListener(
                bootstrap.ConfirmDownloadAndEnter);
            retryButton.onClick.AddListener(
                bootstrap.Retry);
            enterButton.onClick.AddListener(
                bootstrap.EnterMrFeature);

            Render(
                bootstrap.Snapshot ??
                new ResourceUpdateSnapshot(
                    ResourceUpdateStage.Idle,
                    0f,
                    0L,
                    0L,
                    string.Empty,
                    "等待初始化资源系统。",
                    string.Empty));

            SetButtons(
                ResourceUpdateStage.Idle,
                bootstrap.IsHotUpdateStarted);
        }

        private void OnDestroy()
        {
            if (bootstrap == null)
                return;

            if (subscribed)
            {
                bootstrap.SnapshotChanged -=
                    OnSnapshotChanged;
                bootstrap.HotUpdateStarted -=
                    OnHotUpdateStarted;
                bootstrap.FlowFailed -=
                    OnFlowFailed;
            }

            downloadButton?.onClick.RemoveListener(
                bootstrap.ConfirmDownloadAndEnter);
            retryButton?.onClick.RemoveListener(
                bootstrap.Retry);
            enterButton?.onClick.RemoveListener(
                bootstrap.EnterMrFeature);
        }

        private void OnSnapshotChanged(
            ResourceUpdateSnapshot snapshot)
        {
            Render(snapshot);
            SetButtons(
                snapshot.Stage,
                bootstrap.IsHotUpdateStarted);
        }

        private void OnHotUpdateStarted()
        {
            if (statusText != null)
            {
                statusText.text =
                    "资源与热更新入口准备完成。";
            }

            SetButtons(
                ResourceUpdateStage.Ready,
                true);

            if (autoEnterMrFeature)
                bootstrap.EnterMrFeature();
        }

        private void OnFlowFailed(string message)
        {
            if (statusText != null)
            {
                statusText.text =
                    "启动失败：" + message;
            }

            SetButtons(
                ResourceUpdateStage.Failed,
                false);
        }

        private void Render(
            ResourceUpdateSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            statusText.text =
                snapshot.Stage + Environment.NewLine +
                snapshot.Message;

            progressSlider.value =
                snapshot.Progress;

            progressText.text =
                (snapshot.Progress * 100f).ToString("0") +
                "%    " +
                FormatBytes(snapshot.DownloadedBytes) +
                " / " +
                FormatBytes(snapshot.TotalBytes);

            versionText.text =
                string.IsNullOrWhiteSpace(
                    bootstrap.CurrentPackageVersion)
                    ? "Package: checking..."
                    : "Package: " +
                      bootstrap.CurrentPackageVersion;

            if (!string.IsNullOrWhiteSpace(
                    snapshot.CurrentFile))
            {
                progressText.text +=
                    Environment.NewLine + snapshot.CurrentFile;
            }
        }

        private void SetButtons(
            ResourceUpdateStage stage,
            bool canEnter)
        {
            downloadButton.gameObject.SetActive(
                stage ==
                ResourceUpdateStage.AwaitingDownload);

            retryButton.gameObject.SetActive(
                stage == ResourceUpdateStage.Failed ||
                stage == ResourceUpdateStage.Cancelled);

            enterButton.gameObject.SetActive(
                canEnter);
        }

        private bool ValidateReferences()
        {
            var valid =
                bootstrap != null &&
                statusText != null &&
                progressText != null &&
                versionText != null &&
                progressSlider != null &&
                downloadButton != null &&
                retryButton != null &&
                enterButton != null;

            if (!valid)
            {
                Debug.LogError(
                    "[Museum.Update] UpdateSceneView 引用不完整。",
                    this);
            }

            return valid;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0L)
                return "0 B";

            var value = (double)bytes;
            var units = new[]
            {
                "B", "KB", "MB", "GB"
            };
            var index = 0;

            while (value >= 1024d &&
                   index < units.Length - 1)
            {
                value /= 1024d;
                index++;
            }

            return value.ToString(
                       index == 0 ? "0" : "0.0") +
                   " " + units[index];
        }
    }
}
