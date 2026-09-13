using System;
using UnityEngine;

namespace HotUpdate.Museum.N
{

    /// <summary>当前只实现 Distance，其他值为后续触发适配器保留。</summary>
    public enum PointActivationSourceType
    {
        Distance = 0,
        Collider = 100,
        Gesture = 200,
        Manual = 300
    }

    public enum PointExitReason
    {
        NoTrigger = 0,
        SwitchPoint = 1,
        Unload = 2,
        Shutdown = 3
    }

    public enum DistanceActivationChange
    {
        None = 0,
        Entered = 1,
        Exited = 2
    }

    /// <summary>唯一标识一个触发来源；同一点位可以同时持有多个来源。</summary>
    public readonly struct PointActivationKey : IEquatable<PointActivationKey>
    {
        public PointActivationKey(PointActivationSourceType sourceType, int sourceId)
        {
            SourceType = sourceType;
            SourceId = sourceId;
        }

        public PointActivationSourceType SourceType { get; }
        public int SourceId { get; }

        public bool Equals(PointActivationKey other)
        {
            return SourceType == other.SourceType && SourceId == other.SourceId;
        }

        public override bool Equals(object obj)
        {
            return obj is PointActivationKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)SourceType * 397) ^ SourceId;
            }
        }
    }

    public interface IPointModule
    {
        void Initialize(MuseumPoint point);
        void EnterPoint();
        void ExitPoint(PointExitReason reason);
        void Shutdown();
    }

    /// <summary>挂到点位模型子节点后，由 PointRuntimeController 自动收集。</summary>
    public abstract class PointModuleBehaviour : MonoBehaviour, IPointModule
    {
        protected MuseumPoint Point { get; private set; }

        public virtual void Initialize(MuseumPoint point)
        {
            Point = point;
        }

        public virtual void EnterPoint()
        {
        }

        public virtual void ExitPoint(PointExitReason reason)
        {
        }

        public virtual void Shutdown()
        {
            Point = null;
        }
    }

    /// <summary>带进入/退出滞回区间，避免玩家站在边界时反复触发。</summary>
    public sealed class DistanceActivationGate
    {
        public bool IsInside { get; private set; }

        public DistanceActivationChange Evaluate(
            float distance,
            float enterDistance,
            float exitDistance)
        {
            ValidateDistances(enterDistance, exitDistance);

            if (!IsInside && distance <= enterDistance)
            {
                IsInside = true;
                return DistanceActivationChange.Entered;
            }

            if (IsInside && distance >= exitDistance)
            {
                IsInside = false;
                return DistanceActivationChange.Exited;
            }

            return DistanceActivationChange.None;
        }

        public bool Reset()
        {
            bool wasInside = IsInside;
            IsInside = false;
            return wasInside;
        }

        private static void ValidateDistances(float enterDistance, float exitDistance)
        {
            if (enterDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enterDistance),
                    "进入距离不能小于 0。");
            }

            if (exitDistance < enterDistance)
            {
                throw new ArgumentException("退出距离必须大于或等于进入距离。");
            }
        }
    }
}
