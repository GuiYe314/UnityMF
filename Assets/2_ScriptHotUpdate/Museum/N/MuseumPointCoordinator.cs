using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 聚合同一点位的多个触发来源，并仲裁同组互斥点位。
    /// 当前只自动发现 DistancePointTrigger；公开上报接口供后续触发器复用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MuseumPointCoordinator : MonoBehaviour
    {
        [Tooltip("通常设置为主相机；为空时尝试使用 Camera.main。")]
        [SerializeField]
        private Transform observer;

        [Tooltip("自动发现点位的根节点；为空时使用当前节点。")]
        [SerializeField]
        private Transform pointSearchRoot;

        [Tooltip("可显式补充不在搜索根节点下的点位。")]
        [SerializeField]
        private List<MuseumPoint> configuredPoints = new();

        [Min(0.02f)]
        [Tooltip("评估间隔。")]
        [SerializeField]
        private float evaluationInterval = 0.1f;

        [Tooltip("同优先级点位至少近这么多米才切换，避免边界抖动。")]
        [Min(0f)]
        [SerializeField]
        private float switchDistanceAdvantage = 0.25f;

        [SerializeField]
        private bool initializeOnStart = true;

        private readonly List<MuseumPoint> points = new();
        private readonly List<DistancePointTrigger> distanceTriggers = new();
        private readonly PointActivationRegistry<MuseumPoint> activations = new();
        private readonly Dictionary<string, MuseumPoint> activeExclusivePoints =
            new(StringComparer.Ordinal);

        private bool initialized;
        private bool evaluating;
        private bool shuttingDown;
        private float nextEvaluationTime;

        public bool IsInitialized => initialized;

        private void Start()
        {
            if (initializeOnStart)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!initialized || Time.unscaledTime < nextEvaluationTime)
            {
                return;
            }

            EvaluateNow();
            nextEvaluationTime = Time.unscaledTime + evaluationInterval;
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            ResolveObserver();
            DiscoverPoints();

            foreach (MuseumPoint point in points)
            {
                point.Initialize();

                DistancePointTrigger[] triggers =
                    point.GetComponentsInChildren<DistancePointTrigger>(true);

                foreach (DistancePointTrigger trigger in triggers)
                {
                    if (trigger == null || distanceTriggers.Contains(trigger))
                    {
                        continue;
                    }

                    trigger.Initialize(this, point);
                    distanceTriggers.Add(trigger);
                }
            }

            initialized = true;
            nextEvaluationTime = 0f;
            EvaluateNow();
        }

        public void EvaluateNow()
        {
            if (!initialized || shuttingDown)
            {
                return;
            }

            ResolveObserver();
            if (observer == null)
            {
                return;
            }

            Vector3 observerPosition = observer.position;
            foreach (MuseumPoint point in points)
            {
                point.UpdateDistance(observerPosition);
            }

            evaluating = true;
            try
            {
                foreach (DistancePointTrigger trigger in distanceTriggers)
                {
                    trigger?.Evaluate(observerPosition);
                }
            }
            finally
            {
                evaluating = false;
            }

            Reconcile();
        }

        /// <summary>后续碰撞、手势、手动触发器统一调用的入口。</summary>
        public void ReportEnter(MuseumPoint point, PointActivationKey source)
        {
            if (!initialized || shuttingDown || point == null)
            {
                return;
            }

            if (!points.Contains(point))
            {
                point.Initialize();
                points.Add(point);
            }

            if (activations.Add(point, source) && !evaluating)
            {
                Reconcile();
            }
        }

        /// <summary>只移除当前来源；同一点位仍有其他来源时不会退出。</summary>
        public void ReportExit(MuseumPoint point, PointActivationKey source)
        {
            if (!initialized || shuttingDown || point == null)
            {
                return;
            }

            if (activations.Remove(point, source) && !evaluating)
            {
                Reconcile();
            }
        }

        public int GetActiveSourceCount(MuseumPoint point)
        {
            return activations.GetSourceCount(point);
        }

        public void Shutdown()
        {
            if (!initialized || shuttingDown)
            {
                return;
            }

            shuttingDown = true;

            foreach (DistancePointTrigger trigger in distanceTriggers)
            {
                trigger?.Shutdown(false);
            }

            activations.Clear();
            activeExclusivePoints.Clear();

            foreach (MuseumPoint point in points)
            {
                point?.Shutdown();
            }

            distanceTriggers.Clear();
            points.Clear();
            initialized = false;
            evaluating = false;
            shuttingDown = false;
        }

        private void DiscoverPoints()
        {
            points.Clear();
            HashSet<MuseumPoint> uniquePoints = new();

            Transform root = pointSearchRoot != null ? pointSearchRoot : transform;
            MuseumPoint[] discovered = root.GetComponentsInChildren<MuseumPoint>(true);

            foreach (MuseumPoint point in discovered)
            {
                AddPoint(point, uniquePoints);
            }

            foreach (MuseumPoint point in configuredPoints)
            {
                AddPoint(point, uniquePoints);
            }
        }

        private void AddPoint(MuseumPoint point, HashSet<MuseumPoint> uniquePoints)
        {
            if (point != null && uniquePoints.Add(point))
            {
                points.Add(point);
            }
        }

        private void ResolveObserver()
        {
            if (observer == null && Camera.main != null)
            {
                observer = Camera.main.transform;
            }
        }

        private void Reconcile()
        {
            HashSet<string> groups = new(StringComparer.Ordinal);
            foreach (MuseumPoint point in points)
            {
                if (point != null)
                {
                    groups.Add(point.InteractionGroup);
                }
            }

            List<string> activeGroups =
                new(activeExclusivePoints.Keys);
            foreach (string activeGroup in activeGroups)
            {
                groups.Add(activeGroup);
            }

            foreach (string group in groups)
            {
                activeExclusivePoints.TryGetValue(group, out MuseumPoint previous);
                MuseumPoint winner = FindExclusiveWinner(group, previous);

                if (winner == previous)
                {
                    continue;
                }

                if (previous != null)
                {
                    previous.ExitPoint(
                        winner == null
                            ? PointExitReason.NoTrigger
                            : PointExitReason.SwitchPoint);
                }

                if (winner == null)
                {
                    activeExclusivePoints.Remove(group);
                }
                else
                {
                    activeExclusivePoints[group] = winner;
                    winner.EnterPoint();
                }
            }
        }

        private MuseumPoint FindExclusiveWinner(
            string group,
            MuseumPoint previous)
        {
            MuseumPoint best = null;

            foreach (MuseumPoint candidate in points)
            {
                if (candidate == null ||
                    !string.Equals(
                        candidate.InteractionGroup,
                        group,
                        StringComparison.Ordinal) ||
                    !activations.HasAny(candidate))
                {
                    continue;
                }

                if (best == null || IsBetterCandidate(candidate, best))
                {
                    best = candidate;
                }
            }

            if (best == null ||
                previous == null ||
                best == previous ||
                !activations.HasAny(previous))
            {
                return best;
            }

            int bestSourcePriority = GetHighestSourcePriority(best);
            int previousSourcePriority = GetHighestSourcePriority(previous);

            if (bestSourcePriority == previousSourcePriority &&
                best.Priority == previous.Priority &&
                best.DistanceToObserver + switchDistanceAdvantage >=
                previous.DistanceToObserver)
            {
                return previous;
            }

            return best;
        }

        private bool IsBetterCandidate(MuseumPoint candidate, MuseumPoint current)
        {
            int candidateSourcePriority = GetHighestSourcePriority(candidate);
            int currentSourcePriority = GetHighestSourcePriority(current);

            if (candidateSourcePriority != currentSourcePriority)
            {
                return candidateSourcePriority > currentSourcePriority;
            }

            if (candidate.Priority != current.Priority)
            {
                return candidate.Priority > current.Priority;
            }

            return candidate.DistanceToObserver < current.DistanceToObserver;
        }

        private int GetHighestSourcePriority(MuseumPoint point)
        {
            int priority = int.MinValue;

            foreach (PointActivationKey source in activations.GetSources(point))
            {
                priority = Mathf.Max(priority, (int)source.SourceType);
            }

            return priority;
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void OnValidate()
        {
            evaluationInterval = Mathf.Max(0.02f, evaluationInterval);
            switchDistanceAdvantage = Mathf.Max(0f, switchDistanceAdvantage);
        }
    }
}
