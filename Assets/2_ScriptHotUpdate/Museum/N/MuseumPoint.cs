using System;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>新点位的稳定入口，只负责身份和生命周期。</summary>
    [DisallowMultipleComponent]
    public sealed class MuseumPoint : MonoBehaviour
    {
        [Tooltip("不填就使用物体名称。")]
        [SerializeField]
        private string pointId;

        [SerializeField]
        private string interactionGroup = "Default";

        [Tooltip("同来源类型下数值越大优先级越高。")]
        [SerializeField]
        private int priority;

        [SerializeField]
        private PointRuntimeController runtimeController;

        private bool initialized;

        public event Action<MuseumPoint> Entered;
        public event Action<MuseumPoint, PointExitReason> Exited;

        public string PointId =>
            string.IsNullOrWhiteSpace(pointId) ? gameObject.name : pointId;

        public string InteractionGroup =>
            string.IsNullOrWhiteSpace(interactionGroup) ? "Default" : interactionGroup;

        public int Priority => priority;
        public bool IsEntered { get; private set; }
        public float DistanceToObserver { get; private set; } = float.PositiveInfinity;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (runtimeController == null)
            {
                runtimeController = GetComponentInChildren<PointRuntimeController>(true);
            }

            runtimeController?.Initialize(this);
            initialized = true;
        }

        public void EnterPoint()
        {
            if (IsEntered)
            {
                return;
            }

            Initialize();
            IsEntered = true;
            runtimeController?.EnterPoint();
            Entered?.Invoke(this);
        }

        public void ExitPoint(PointExitReason reason)
        {
            if (!IsEntered)
            {
                return;
            }

            IsEntered = false;
            runtimeController?.ExitPoint(reason);
            Exited?.Invoke(this, reason);
        }

        public void Shutdown()
        {
            ExitPoint(PointExitReason.Shutdown);
            runtimeController?.Shutdown();
            initialized = false;
            DistanceToObserver = float.PositiveInfinity;
        }

        internal void UpdateDistance(Vector3 observerPosition)
        {
            DistanceToObserver = Vector3.Distance(observerPosition, transform.position);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(pointId))
            {
                pointId = gameObject.name;
            }

            if (string.IsNullOrWhiteSpace(interactionGroup))
            {
                interactionGroup = "Default";
            }
        }
    }
}
