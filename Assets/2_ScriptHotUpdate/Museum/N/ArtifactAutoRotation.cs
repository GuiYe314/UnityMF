using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 文物旋转组件。移入只暂停当前文物，退出点位时由文物组模块统一停止。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArtifactAutoRotation :
        PointMessagingModuleBehaviour,
        IPointMessageHandler<ArtifactRotationMessage>,
        IPointMessageHandler<InteractionInputMessage>
    {
        [SerializeField]
        private Vector3 degreesPerSecond = new(0f, 30f, 0f);

        [SerializeField]
        private Space rotationSpace = Space.Self;

        [Tooltip("用于接收移入/移出消息；为空时从父级自动查找。")]
        [SerializeField]
        private MuseumInteractionTarget interactionTarget;

        private bool running;
        private bool manuallyPaused;
        private readonly HashSet<InteractionSourceKey> hoverSources = new();

        public bool IsRunning => running;
        public bool IsPaused => manuallyPaused || hoverSources.Count > 0;

        public override void Initialize(MuseumPoint point)
        {
            base.Initialize(point);
            if (interactionTarget == null)
            {
                interactionTarget =
                    GetComponentInParent<MuseumInteractionTarget>();
            }

            Messages?.Subscribe<ArtifactRotationMessage>(this);
            Messages?.Subscribe<InteractionInputMessage>(this);
        }

        private void Update()
        {
            if (!running || IsPaused)
            {
                return;
            }

            transform.Rotate(
                degreesPerSecond * Time.deltaTime,
                rotationSpace);
        }

        public void StartRotation()
        {
            running = true;
        }

        public void StopRotation()
        {
            running = false;
            manuallyPaused = false;
            hoverSources.Clear();
        }

        /// <summary>供 MRTK/其他输入适配器在移入时调用。</summary>
        public void NotifyHoverEnter()
        {
            manuallyPaused = true;
        }

        /// <summary>供 MRTK/其他输入适配器在移出时调用。</summary>
        public void NotifyHoverExit()
        {
            manuallyPaused = false;
        }

        public void Handle(in ArtifactRotationMessage message)
        {
            if (message.Point != Point ||
                message.Scope == null ||
                (transform != message.Scope &&
                 !transform.IsChildOf(message.Scope)))
            {
                return;
            }

            if (message.Command == ArtifactRotationCommand.Start)
            {
                StartRotation();
            }
            else
            {
                StopRotation();
            }
        }

        public void Handle(in InteractionInputMessage message)
        {
            if (interactionTarget == null ||
                message.Target != interactionTarget)
            {
                return;
            }

            if (message.Phase == InteractionInputPhase.HoverEnter)
            {
                hoverSources.Add(message.Source);
            }
            else if (message.Phase == InteractionInputPhase.HoverExit)
            {
                hoverSources.Remove(message.Source);
            }
        }

        public override void ExitPoint(PointExitReason reason)
        {
            StopRotation();
        }

        public override void Shutdown()
        {
            StopRotation();
            Messages?.Unsubscribe<ArtifactRotationMessage>(this);
            Messages?.Unsubscribe<InteractionInputMessage>(this);
            interactionTarget = null;
            base.Shutdown();
        }

        private void OnDisable()
        {
            StopRotation();
        }
    }
}
