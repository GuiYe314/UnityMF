using System;
using UnityEngine;
using UnityEngine.Video;

namespace HotUpdate.Museum.N
{
    public enum InteractionInputPhase
    {
        HoverEnter = 0,
        HoverExit = 1,
        Click = 2
    }

    public enum InteractionInputSourceType
    {
        Unknown = 0,
        Mrtk = 100,
        UnityUi = 200,
        Collider = 300,
        Gesture = 400,
        Script = 500
    }

    public readonly struct InteractionSourceKey :
        IEquatable<InteractionSourceKey>
    {
        public InteractionSourceKey(
            InteractionInputSourceType sourceType,
            int sourceId)
        {
            SourceType = sourceType;
            SourceId = sourceId;
        }

        public InteractionInputSourceType SourceType { get; }
        public int SourceId { get; }

        public bool Equals(InteractionSourceKey other)
        {
            return SourceType == other.SourceType && SourceId == other.SourceId;
        }

        public override bool Equals(object obj)
        {
            return obj is InteractionSourceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)SourceType * 397) ^ SourceId;
            }
        }
    }

    public readonly struct InteractionInputMessage : IPointMessage
    {
        public InteractionInputMessage(
            MuseumPoint point,
            MuseumInteractionTarget target,
            InteractionInputPhase phase,
            InteractionSourceKey source)
        {
            Point = point;
            Target = target;
            Phase = phase;
            Source = source;
        }

        public MuseumPoint Point { get; }
        public MuseumInteractionTarget Target { get; }
        public InteractionInputPhase Phase { get; }
        public InteractionSourceKey Source { get; }
    }

    public readonly struct OptionSelectionChangedMessage : IPointMessage
    {
        public OptionSelectionChangedMessage(
            MuseumPoint point,
            MuseumInteractionTarget previous,
            MuseumInteractionTarget current)
        {
            Point = point;
            Previous = previous;
            Current = current;
        }

        public MuseumPoint Point { get; }
        public MuseumInteractionTarget Previous { get; }
        public MuseumInteractionTarget Current { get; }
    }

    public enum MuseumOptionVisualState
    {
        Normal = 0,
        Hovered = 1,
        Selected = 2
    }

    public readonly struct ModelVisualMessage : IPointMessage
    {
        public ModelVisualMessage(
            MuseumPoint point,
            MuseumInteractionTarget target,
            MuseumOptionVisualState state)
        {
            Point = point;
            Target = target;
            State = state;
        }

        public MuseumPoint Point { get; }
        public MuseumInteractionTarget Target { get; }
        public MuseumOptionVisualState State { get; }
    }

    public enum PointLifecyclePhase
    {
        Entered = 0,
        Exited = 1
    }

    public readonly struct PointLifecycleMessage : IPointMessage
    {
        public PointLifecycleMessage(
            MuseumPoint point,
            PointLifecyclePhase phase,
            PointExitReason exitReason)
        {
            Point = point;
            Phase = phase;
            ExitReason = exitReason;
        }

        public MuseumPoint Point { get; }
        public PointLifecyclePhase Phase { get; }
        public PointExitReason ExitReason { get; }
    }

    public enum VideoCommand
    {
        Play = 0,
        Stop = 1,
        Show = 2,
        Hide = 3
    }

    public readonly struct VideoMessage : IPointMessage
    {
        public VideoMessage(
            MuseumPoint point,
            VideoCommand command,
            VideoClip clip = null)
        {
            Point = point;
            Command = command;
            Clip = clip;
        }

        public MuseumPoint Point { get; }
        public VideoCommand Command { get; }
        public VideoClip Clip { get; }
    }

    public enum ArtifactCommand
    {
        Prepare = 0,
        Show = 1,
        Hide = 2,
        Clear = 3
    }

    public readonly struct ArtifactMessage : IPointMessage
    {
        public ArtifactMessage(
            MuseumPoint point,
            ArtifactCommand command,
            GameObject artifactGroup = null)
        {
            Point = point;
            Command = command;
            ArtifactGroup = artifactGroup;
        }

        public MuseumPoint Point { get; }
        public ArtifactCommand Command { get; }
        public GameObject ArtifactGroup { get; }
    }

    public enum ArtifactRotationCommand
    {
        Start = 0,
        Stop = 1
    }

    public readonly struct ArtifactRotationMessage : IPointMessage
    {
        public ArtifactRotationMessage(
            MuseumPoint point,
            ArtifactRotationCommand command,
            Transform scope)
        {
            Point = point;
            Command = command;
            Scope = scope;
        }

        public MuseumPoint Point { get; }
        public ArtifactRotationCommand Command { get; }
        public Transform Scope { get; }
    }

    public readonly struct MuseumDisplayToggleMessage : IPointMessage
    {
        public MuseumDisplayToggleMessage(
            MuseumPoint point,
            InteractionSourceKey source)
        {
            Point = point;
            Source = source;
        }

        public MuseumPoint Point { get; }
        public InteractionSourceKey Source { get; }
    }

    public enum ObjectDisplayState
    {
        Hidden = 0,
        First = 1,
        Second = 2
    }

    public readonly struct ToggleObjectDisplayMessage : IPointMessage
    {
        public ToggleObjectDisplayMessage(
            MuseumPoint point,
            string groupId)
        {
            Point = point;
            GroupId = groupId;
        }

        public MuseumPoint Point { get; }
        public string GroupId { get; }
    }

    public readonly struct SetObjectDisplayMessage : IPointMessage
    {
        public SetObjectDisplayMessage(
            MuseumPoint point,
            string groupId,
            ObjectDisplayState state,
            InteractionSourceKey source,
            int priority = 0,
            bool isTemporary = true)
        {
            Point = point;
            GroupId = groupId;
            State = state;
            Source = source;
            Priority = priority;
            IsTemporary = isTemporary;
        }

        public MuseumPoint Point { get; }
        public string GroupId { get; }
        public ObjectDisplayState State { get; }
        public InteractionSourceKey Source { get; }
        public int Priority { get; }
        public bool IsTemporary { get; }
    }

    public readonly struct ReleaseObjectDisplayMessage : IPointMessage
    {
        public ReleaseObjectDisplayMessage(
            MuseumPoint point,
            string groupId,
            InteractionSourceKey source)
        {
            Point = point;
            GroupId = groupId;
            Source = source;
        }

        public MuseumPoint Point { get; }
        public string GroupId { get; }
        public InteractionSourceKey Source { get; }
    }
}
