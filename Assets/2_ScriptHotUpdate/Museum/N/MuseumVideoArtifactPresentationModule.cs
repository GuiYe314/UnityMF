using UnityEngine;

namespace HotUpdate.Museum.N
{
    public enum MuseumPointDisplayMode
    {
        Video = 0,
        Artifact = 1
    }

    /// <summary>
    /// 可选业务模块：把“选中展品”转换为视频、文物及按钮展示命令。
    /// 不需要这种流程的点位不要挂此组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MuseumVideoArtifactPresentationModule :
        PointMessagingModuleBehaviour,
        IPointMessageHandler<OptionSelectionChangedMessage>,
        IPointMessageHandler<MuseumDisplayToggleMessage>
    {
        [Tooltip("点击展品后显示的视频/文物切换按钮区域。")]
        [SerializeField]
        private GameObject buttonRoot;

        private MuseumInteractionTarget selectedTarget;

        public MuseumPointDisplayMode DisplayMode { get; private set; }
        public MuseumInteractionTarget SelectedTarget => selectedTarget;

        public override void Initialize(MuseumPoint point)
        {
            base.Initialize(point);
            Messages?.Subscribe<OptionSelectionChangedMessage>(this);
            Messages?.Subscribe<MuseumDisplayToggleMessage>(this);
            ResetPresentation();
        }

        public void Handle(in OptionSelectionChangedMessage message)
        {
            if (message.Point != Point)
            {
                return;
            }

            selectedTarget = message.Current;
            if (selectedTarget == null)
            {
                ResetPresentation();
                return;
            }

            DisplayMode = MuseumPointDisplayMode.Video;
            buttonRoot?.SetActive(true);

            ArtifactMessage artifactMessage = new(
                Point,
                ArtifactCommand.Prepare,
                selectedTarget.ArtifactGroup);
            Messages?.Publish(artifactMessage);

            VideoMessage videoMessage = new(
                Point,
                VideoCommand.Play,
                selectedTarget.VideoClip);
            Messages?.Publish(videoMessage);
        }

        public void Handle(in MuseumDisplayToggleMessage message)
        {
            if (message.Point != Point || selectedTarget == null)
            {
                return;
            }

            if (DisplayMode == MuseumPointDisplayMode.Video)
            {
                VideoMessage stopVideo =
                    new(Point, VideoCommand.Stop);
                ArtifactMessage showArtifact =
                    new(Point, ArtifactCommand.Show, selectedTarget.ArtifactGroup);
                Messages?.Publish(stopVideo);
                Messages?.Publish(showArtifact);
                DisplayMode = MuseumPointDisplayMode.Artifact;
            }
            else
            {
                ArtifactMessage hideArtifact =
                    new(Point, ArtifactCommand.Hide, selectedTarget.ArtifactGroup);
                VideoMessage playVideo =
                    new(Point, VideoCommand.Play, selectedTarget.VideoClip);
                Messages?.Publish(hideArtifact);
                Messages?.Publish(playVideo);
                DisplayMode = MuseumPointDisplayMode.Video;
            }
        }

        public void ToggleDisplayMode()
        {
            if (Point == null || !Point.IsEntered)
            {
                return;
            }

            MuseumDisplayToggleMessage message = new(
                Point,
                new InteractionSourceKey(
                    InteractionInputSourceType.Script,
                    GetInstanceID()));
            Messages?.Publish(message);
        }

        public override void ExitPoint(PointExitReason reason)
        {
            ResetPresentation();
        }

        public override void Shutdown()
        {
            ResetPresentation();
            Messages?.Unsubscribe<OptionSelectionChangedMessage>(this);
            Messages?.Unsubscribe<MuseumDisplayToggleMessage>(this);
            base.Shutdown();
        }

        private void ResetPresentation()
        {
            selectedTarget = null;
            DisplayMode = MuseumPointDisplayMode.Video;
            buttonRoot?.SetActive(false);

            if (Messages == null || Point == null)
            {
                return;
            }

            VideoMessage stopVideo = new(Point, VideoCommand.Stop);
            ArtifactMessage clearArtifact =
                new(Point, ArtifactCommand.Clear);
            Messages.Publish(stopVideo);
            Messages.Publish(clearArtifact);
        }
    }
}
