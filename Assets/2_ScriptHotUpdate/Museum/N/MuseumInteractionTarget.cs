using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 一个可交互对象的数据入口。输入、视觉和业务模块都通过消息识别它。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MuseumInteractionTarget :
        MonoBehaviour,
        IPointMessageHandler<InteractionInputMessage>,
        IPointMessageHandler<OptionSelectionChangedMessage>,
        IPointMessageHandler<PointLifecycleMessage>
    {
        [Tooltip("不填时使用物体名称。")]
        [SerializeField]
        private string targetId;

        [Tooltip("关闭后仍可接收 Hover 等消息，但不会进入展品单选合集。")]
        [SerializeField]
        private bool selectable = true;

        [SerializeField]
        private MuseumOptionView view;

        [Tooltip("该目标被选中时对应的视频，可为空。")]
        [SerializeField]
        private VideoClip videoClip;

        [Tooltip("该目标被选中时对应的文物组，可为空。")]
        [SerializeField]
        private GameObject artifactGroup;

        [Tooltip("该目标独有的扩展技能，可为空。")]
        [SerializeField]
        private List<MuseumOptionAction> actions = new();

        private MuseumPoint point;
        private PointMessageBus messages;
        private bool bound;
        private bool selected;
        private readonly HashSet<InteractionSourceKey> hoverSources = new();

        public string TargetId =>
            string.IsNullOrWhiteSpace(targetId) ? gameObject.name : targetId;

        public MuseumOptionView View => view;
        public bool Selectable => selectable;
        public VideoClip VideoClip => videoClip;
        public GameObject ArtifactGroup => artifactGroup;
        public IReadOnlyList<MuseumOptionAction> Actions => actions;
        public MuseumPoint Point => point;
        public bool IsBound => bound;

        internal void Bind(MuseumPoint owner, PointMessageBus messageBus)
        {
            if (bound && point == owner && messages == messageBus)
            {
                return;
            }

            Unbind();

            point = owner;
            messages = messageBus;
            bound = point != null && messages != null;
            if (!bound)
            {
                return;
            }

            if (view == null)
            {
                view = GetComponent<MuseumOptionView>();
            }

            messages.Subscribe<InteractionInputMessage>(this);
            messages.Subscribe<OptionSelectionChangedMessage>(this);
            messages.Subscribe<PointLifecycleMessage>(this);
            if (view != null)
            {
                view.Bind(this, messages);
            }

            MuseumOptionContext context = CreateContext();
            foreach (MuseumOptionAction action in actions)
            {
                if (action != null)
                {
                    action.Initialize(context);
                }
            }
        }

        internal void Unbind()
        {
            if (messages != null)
            {
                messages.Unsubscribe<InteractionInputMessage>(this);
                messages.Unsubscribe<OptionSelectionChangedMessage>(this);
                messages.Unsubscribe<PointLifecycleMessage>(this);
            }

            if (bound)
            {
                MuseumOptionContext context = CreateContext();
                foreach (MuseumOptionAction action in actions)
                {
                    if (action != null)
                    {
                        action.Shutdown(context);
                    }
                }
            }

            if (view != null)
            {
                view.Unbind();
            }
            bound = false;
            hoverSources.Clear();
            selected = false;
            messages = null;
            point = null;
        }

        public bool PublishInput(
            InteractionInputPhase phase,
            InteractionSourceKey source)
        {
            if (!bound || point == null || !point.IsEntered)
            {
                return false;
            }

            InteractionInputMessage message =
                new(point, this, phase, source);
            messages.Publish(message);
            return true;
        }

        public void Handle(in InteractionInputMessage message)
        {
            if (message.Target != this)
            {
                return;
            }

            if (message.Phase == InteractionInputPhase.HoverEnter)
            {
                if (hoverSources.Add(message.Source))
                {
                    PublishVisualState();
                }
            }
            else if (message.Phase == InteractionInputPhase.HoverExit)
            {
                if (hoverSources.Remove(message.Source))
                {
                    PublishVisualState();
                }
            }
        }

        public void Handle(in OptionSelectionChangedMessage message)
        {
            if (message.Point != point)
            {
                return;
            }

            MuseumOptionContext context = CreateContext();
            if (message.Previous == this)
            {
                selected = false;
                foreach (MuseumOptionAction action in actions)
                {
                    if (action != null)
                    {
                        action.OnDeselected(context);
                    }
                }

                PublishVisualState();
            }

            if (message.Current == this)
            {
                selected = true;
                foreach (MuseumOptionAction action in actions)
                {
                    if (action != null)
                    {
                        action.OnSelected(context);
                    }
                }

                PublishVisualState();
            }
        }

        public void Handle(in PointLifecycleMessage message)
        {
            if (message.Point != point ||
                message.Phase != PointLifecyclePhase.Exited)
            {
                return;
            }

            MuseumOptionContext context = CreateContext();
            hoverSources.Clear();
            selected = false;
            PublishVisualState();
            foreach (MuseumOptionAction action in actions)
            {
                if (action != null)
                {
                    action.OnPointExit(context, message.ExitReason);
                }
            }
        }

        private MuseumOptionContext CreateContext()
        {
            return new MuseumOptionContext(point, this, messages);
        }

        private void PublishVisualState()
        {
            if (messages == null || point == null)
            {
                return;
            }

            MuseumOptionVisualState state = selected
                ? MuseumOptionVisualState.Selected
                : hoverSources.Count > 0
                    ? MuseumOptionVisualState.Hovered
                    : MuseumOptionVisualState.Normal;
            ModelVisualMessage message = new(point, this, state);
            messages.Publish(message);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                targetId = gameObject.name;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
