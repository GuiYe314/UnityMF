using UnityEngine;
using UnityEngine.Video;

namespace HotUpdate.Museum.N
{
    /// <summary>一个点位共用一个 VideoPlayer，只处理 VideoMessage。</summary>
    [DisallowMultipleComponent]
    public sealed class PointVideoModule : PointMessageModule<VideoMessage>
    {
        [SerializeField]
        private VideoPlayer videoPlayer;

        [Tooltip("包含 VideoPlayer/RawImage 的显示根节点。")]
        [SerializeField]
        private GameObject videoRoot;

        [SerializeField]
        private bool hideOnInitialize = true;

        public VideoClip CurrentClip { get; private set; }

        public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;

        public override void Initialize(MuseumPoint point)
        {
            base.Initialize(point);

            if (videoPlayer == null)
            {
                videoPlayer = GetComponentInChildren<VideoPlayer>(true);
            }

            if (hideOnInitialize)
            {
                StopAndHide();
            }
        }

        public void Play(VideoClip clip)
        {
            if (videoPlayer == null || clip == null)
            {
                StopAndHide();
                return;
            }

            if (videoRoot != null)
            {
                videoRoot.SetActive(true);
            }

            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            CurrentClip = clip;
            videoPlayer.clip = clip;
            videoPlayer.Play();
        }

        public void Show()
        {
            if (videoRoot != null)
            {
                videoRoot.SetActive(true);
            }
        }

        public void StopAndHide()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.clip = null;
            }

            CurrentClip = null;

            if (videoRoot != null)
            {
                videoRoot.SetActive(false);
            }
        }

        public override void Handle(in VideoMessage message)
        {
            if (message.Point != Point)
            {
                return;
            }

            switch (message.Command)
            {
                case VideoCommand.Play:
                    Play(message.Clip);
                    break;
                case VideoCommand.Stop:
                    StopAndHide();
                    break;
                case VideoCommand.Show:
                    Show();
                    break;
                case VideoCommand.Hide:
                    StopAndHide();
                    break;
            }
        }

        public override void ExitPoint(PointExitReason reason)
        {
            StopAndHide();
        }

        public override void Shutdown()
        {
            StopAndHide();
            videoPlayer = null;
            base.Shutdown();
        }
    }
}
