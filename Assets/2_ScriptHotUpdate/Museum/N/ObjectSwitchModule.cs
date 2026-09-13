using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>可单元测试的双物体显示状态解析器。</summary>
    public sealed class ObjectDisplayStateResolver
    {
        private readonly struct OverrideEntry
        {
            public OverrideEntry(
                ObjectDisplayState state,
                int priority,
                long sequence)
            {
                State = state;
                Priority = priority;
                Sequence = sequence;
            }

            public ObjectDisplayState State { get; }
            public int Priority { get; }
            public long Sequence { get; }
        }

        private readonly Dictionary<InteractionSourceKey, OverrideEntry> overrides =
            new();
        private long nextSequence;

        public ObjectDisplayState BaseState { get; private set; }

        public ObjectDisplayState ResolvedState
        {
            get
            {
                bool found = false;
                OverrideEntry best = default;

                foreach (OverrideEntry candidate in overrides.Values)
                {
                    if (!found ||
                        candidate.Priority > best.Priority ||
                        (candidate.Priority == best.Priority &&
                         candidate.Sequence > best.Sequence))
                    {
                        best = candidate;
                        found = true;
                    }
                }

                return found ? best.State : BaseState;
            }
        }

        public int OverrideCount => overrides.Count;

        public void Reset(ObjectDisplayState state)
        {
            BaseState = state;
            overrides.Clear();
            nextSequence = 0;
        }

        public void Toggle()
        {
            BaseState = BaseState == ObjectDisplayState.First
                ? ObjectDisplayState.Second
                : ObjectDisplayState.First;
        }

        public void SetBase(ObjectDisplayState state)
        {
            BaseState = state;
        }

        public void SetOverride(
            InteractionSourceKey source,
            ObjectDisplayState state,
            int priority)
        {
            overrides[source] =
                new OverrideEntry(state, priority, ++nextSequence);
        }

        public bool Release(InteractionSourceKey source)
        {
            return overrides.Remove(source);
        }
    }

    /// <summary>
    /// 两个互斥物体的唯一状态所有者。支持按钮基础状态与多来源临时覆盖。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectSwitchModule :
        PointMessagingModuleBehaviour,
        IPointMessageHandler<ToggleObjectDisplayMessage>,
        IPointMessageHandler<SetObjectDisplayMessage>,
        IPointMessageHandler<ReleaseObjectDisplayMessage>
    {
        [Tooltip("消息使用的组名；不填时使用物体名称。")]
        [SerializeField]
        private string groupId;

        [SerializeField]
        private GameObject firstObject;

        [SerializeField]
        private GameObject secondObject;

        [SerializeField]
        private ObjectDisplayState initialState = ObjectDisplayState.First;

        private readonly ObjectDisplayStateResolver resolver = new();

        public string GroupId =>
            string.IsNullOrWhiteSpace(groupId) ? gameObject.name : groupId;

        public ObjectDisplayState BaseState => resolver.BaseState;
        public ObjectDisplayState CurrentState => resolver.ResolvedState;

        public override void Initialize(MuseumPoint point)
        {
            base.Initialize(point);
            Messages?.Subscribe<ToggleObjectDisplayMessage>(this);
            Messages?.Subscribe<SetObjectDisplayMessage>(this);
            Messages?.Subscribe<ReleaseObjectDisplayMessage>(this);
            resolver.Reset(ObjectDisplayState.Hidden);
            ApplyState();
        }

        public override void EnterPoint()
        {
            resolver.Reset(initialState);
            ApplyState();
        }

        public void Handle(in ToggleObjectDisplayMessage message)
        {
            if (!Matches(message.Point, message.GroupId))
            {
                return;
            }

            resolver.Toggle();
            ApplyState();
        }

        public void Handle(in SetObjectDisplayMessage message)
        {
            if (!Matches(message.Point, message.GroupId))
            {
                return;
            }

            if (message.IsTemporary)
            {
                resolver.SetOverride(
                    message.Source,
                    message.State,
                    message.Priority);
            }
            else
            {
                resolver.SetBase(message.State);
            }

            ApplyState();
        }

        public void Handle(in ReleaseObjectDisplayMessage message)
        {
            if (!Matches(message.Point, message.GroupId) ||
                !resolver.Release(message.Source))
            {
                return;
            }

            ApplyState();
        }

        public override void ExitPoint(PointExitReason reason)
        {
            resolver.Reset(ObjectDisplayState.Hidden);
            ApplyState();
        }

        public override void Shutdown()
        {
            resolver.Reset(ObjectDisplayState.Hidden);
            ApplyState();
            Messages?.Unsubscribe<ToggleObjectDisplayMessage>(this);
            Messages?.Unsubscribe<SetObjectDisplayMessage>(this);
            Messages?.Unsubscribe<ReleaseObjectDisplayMessage>(this);
            base.Shutdown();
        }

        private bool Matches(MuseumPoint messagePoint, string messageGroupId)
        {
            return messagePoint == Point &&
                   string.Equals(
                       messageGroupId,
                       GroupId,
                       StringComparison.Ordinal);
        }

        private void ApplyState()
        {
            ObjectDisplayState state = resolver.ResolvedState;
            firstObject?.SetActive(state == ObjectDisplayState.First);
            secondObject?.SetActive(state == ObjectDisplayState.Second);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                groupId = gameObject.name;
            }
        }
    }
}
