using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 只负责交互目标的单选状态。视频、文物和按钮展示由其他消息模块处理。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MuseumPointSelectionModule :
        PointMessagingModuleBehaviour,
        IPointMessageHandler<InteractionInputMessage>
    {
        [Tooltip("自动收集当前点位下的 MuseumInteractionTarget。")]
        [SerializeField]
        private bool autoCollectTargets = true;

        [Tooltip("补充不在点位层级下的交互目标。")]
        [SerializeField]
        private List<MuseumInteractionTarget> configuredTargets = new();

        [SerializeField]
        private bool selectFirstOnEnter;

        private readonly List<MuseumInteractionTarget> targets = new();
        private MuseumInteractionTarget selectedTarget;
        private bool pointActive;

        public IReadOnlyList<MuseumInteractionTarget> Targets => targets;
        public MuseumInteractionTarget SelectedTarget => selectedTarget;
        public int SelectedIndex => targets.IndexOf(selectedTarget);

        public override void Initialize(MuseumPoint point)
        {
            base.Initialize(point);
            BuildTargetList();
            Messages?.Subscribe<InteractionInputMessage>(this);
            pointActive = false;
            selectedTarget = null;
        }

        public override void EnterPoint()
        {
            pointActive = true;
            ClearSelection();

            if (selectFirstOnEnter && targets.Count > 0)
            {
                Select(targets[0]);
            }
        }

        public void Handle(in InteractionInputMessage message)
        {
            if (!pointActive ||
                message.Point != Point ||
                message.Phase != InteractionInputPhase.Click ||
                !targets.Contains(message.Target))
            {
                return;
            }

            Select(message.Target);
        }

        public void Select(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= targets.Count)
            {
                return;
            }

            Select(targets[targetIndex]);
        }

        public void Select(MuseumInteractionTarget target)
        {
            if (!pointActive ||
                Point == null ||
                !Point.IsEntered ||
                target == null ||
                !targets.Contains(target) ||
                selectedTarget == target)
            {
                return;
            }

            MuseumInteractionTarget previous = selectedTarget;
            selectedTarget = target;
            OptionSelectionChangedMessage message =
                new(Point, previous, selectedTarget);
            Messages?.Publish(message);
        }

        public void ClearSelection()
        {
            if (selectedTarget == null)
            {
                return;
            }

            MuseumInteractionTarget previous = selectedTarget;
            selectedTarget = null;
            OptionSelectionChangedMessage message =
                new(Point, previous, null);
            Messages?.Publish(message);
        }

        public override void ExitPoint(PointExitReason reason)
        {
            pointActive = false;
            ClearSelection();
        }

        public override void Shutdown()
        {
            pointActive = false;
            ClearSelection();
            Messages?.Unsubscribe<InteractionInputMessage>(this);
            targets.Clear();
            base.Shutdown();
        }

        private void BuildTargetList()
        {
            targets.Clear();
            HashSet<MuseumInteractionTarget> uniqueTargets = new();

            if (autoCollectTargets && Point != null)
            {
                MuseumInteractionTarget[] discovered =
                    Point.GetComponentsInChildren<MuseumInteractionTarget>(true);
                foreach (MuseumInteractionTarget target in discovered)
                {
                    AddTarget(target, uniqueTargets);
                }
            }

            foreach (MuseumInteractionTarget target in configuredTargets)
            {
                AddTarget(target, uniqueTargets);
            }
        }

        private void AddTarget(
            MuseumInteractionTarget target,
            HashSet<MuseumInteractionTarget> uniqueTargets)
        {
            if (target != null &&
                target.Selectable &&
                uniqueTargets.Add(target))
            {
                targets.Add(target);
            }
        }
    }
}
