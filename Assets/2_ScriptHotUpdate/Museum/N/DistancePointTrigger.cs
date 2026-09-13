using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 第一阶段唯一实现的点位触发源，由 MuseumPointCoordinator 集中轮询。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DistancePointTrigger : MonoBehaviour
    {
        [SerializeField]
        private MuseumPoint point;

        [Min(0f)]
        [SerializeField]
        private float enterDistance = 3f;

        [Min(0f)]
        [SerializeField]
        private float exitDistance = 5f;

        private readonly DistanceActivationGate gate = new();
        private MuseumPointCoordinator coordinator;
        private PointActivationKey activationKey;
        private bool initialized;

        public bool IsInside => gate.IsInside;
        public float EnterDistance => enterDistance;
        public float ExitDistance => exitDistance;

        internal void Initialize(
            MuseumPointCoordinator ownerCoordinator,
            MuseumPoint ownerPoint)
        {
            coordinator = ownerCoordinator;
            point = point != null ? point : ownerPoint;
            activationKey = new PointActivationKey(
                PointActivationSourceType.Distance,
                GetInstanceID());
            gate.Reset();
            initialized = coordinator != null && point != null;

            if (!initialized)
            {
                Debug.LogError("距离触发器缺少 Coordinator 或 MuseumPoint。", this);
            }
        }

        internal void Evaluate(Vector3 observerPosition)
        {
            if (!initialized)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                Release();
                return;
            }

            float distance = Vector3.Distance(observerPosition, transform.position);
            DistanceActivationChange change =
                gate.Evaluate(distance, enterDistance, exitDistance);

            if (change == DistanceActivationChange.Entered)
            {
                coordinator.ReportEnter(point, activationKey);
            }
            else if (change == DistanceActivationChange.Exited)
            {
                coordinator.ReportExit(point, activationKey);
            }
        }

        internal void Shutdown(bool reportExit)
        {
            if (reportExit)
            {
                Release();
            }
            else
            {
                gate.Reset();
            }

            coordinator = null;
            point = null;
            initialized = false;
        }

        private void Release()
        {
            if (gate.Reset() && coordinator != null && point != null)
            {
                coordinator.ReportExit(point, activationKey);
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                Release();
            }
        }

        private void OnValidate()
        {
            enterDistance = Mathf.Max(0f, enterDistance);
            exitDistance = Mathf.Max(enterDistance, exitDistance);
        }

        private void OnDrawSelected()
        {
            Transform anchor = transform;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(anchor.position, enterDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(anchor.position, exitDistance);
        }
    }
}
