using System.Collections.Generic;

namespace HotUpdate.Museum.N
{
    /// <summary>保存“点位 -> 有效触发来源集合”，不依赖 Unity 生命周期。</summary>
    public sealed class PointActivationRegistry<TPoint> where TPoint : class
    {
        private readonly Dictionary<TPoint, HashSet<PointActivationKey>> sourcesByPoint =
            new();

        public bool Add(TPoint point, PointActivationKey source)
        {
            if (point == null)
            {
                return false;
            }

            if (!sourcesByPoint.TryGetValue(point, out HashSet<PointActivationKey> sources))
            {
                sources = new HashSet<PointActivationKey>();
                sourcesByPoint.Add(point, sources);
            }

            return sources.Add(source);
        }

        public bool Remove(TPoint point, PointActivationKey source)
        {
            if (point == null ||
                !sourcesByPoint.TryGetValue(point, out HashSet<PointActivationKey> sources))
            {
                return false;
            }

            bool removed = sources.Remove(source);
            if (sources.Count == 0)
            {
                sourcesByPoint.Remove(point);
            }

            return removed;
        }

        public bool HasAny(TPoint point)
        {
            return point != null &&
                   sourcesByPoint.TryGetValue(point, out HashSet<PointActivationKey> sources) &&
                   sources.Count > 0;
        }

        public int GetSourceCount(TPoint point)
        {
            return point != null &&
                   sourcesByPoint.TryGetValue(point, out HashSet<PointActivationKey> sources)
                ? sources.Count
                : 0;
        }

        public IEnumerable<PointActivationKey> GetSources(TPoint point)
        {
            if (point != null &&
                sourcesByPoint.TryGetValue(point, out HashSet<PointActivationKey> sources))
            {
                return sources;
            }

            return System.Array.Empty<PointActivationKey>();
        }

        public void RemovePoint(TPoint point)
        {
            if (point != null)
            {
                sourcesByPoint.Remove(point);
            }
        }

        public void Clear()
        {
            sourcesByPoint.Clear();
        }
    }
}
