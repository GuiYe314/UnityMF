using System;

namespace AOT.HotUpdate.Framework.Pooling
{
    /// <summary>
    /// 某一种引用类型对象池在查询时刻的只读统计快照。
    /// 快照创建后不会随对象池继续变化。
    /// </summary>
    public readonly struct ReferencePoolInfo
    {
        /// <summary>对象池所管理的实际类型。</summary>
        public Type ReferenceType { get; }

        /// <summary>当前位于池中、可以复用的实例数量。</summary>
        public int UnusedCount { get; }

        /// <summary>当前已经借出、尚未归还的实例数量。</summary>
        public int UsingCount { get; }

        /// <summary>历史获取次数，包括新建与复用。</summary>
        public long AcquireCount { get; }

        /// <summary>历史成功归还次数。</summary>
        public long ReleaseCount { get; }

        /// <summary>历史实际创建的实例数量。</summary>
        public long CreateCount { get; }

        /// <summary>因清理失败、裁剪或清空而丢弃的实例数量。</summary>
        public long DiscardCount { get; }

        internal ReferencePoolInfo(
            Type referenceType,
            int unusedCount,
            int usingCount,
            long acquireCount,
            long releaseCount,
            long createCount,
            long discardCount)
        {
            ReferenceType = referenceType;
            UnusedCount = unusedCount;
            UsingCount = usingCount;
            AcquireCount = acquireCount;
            ReleaseCount = releaseCount;
            CreateCount = createCount;
            DiscardCount = discardCount;
        }

        /// <summary>
        /// 返回适合日志输出的统计摘要。
        /// </summary>
        public override string ToString()
        {
            return $"{ReferenceType.FullName}: Using={UsingCount}, Unused={UnusedCount}, " +
                   $"Acquire={AcquireCount}, Release={ReleaseCount}, " +
                   $"Create={CreateCount}, Discard={DiscardCount}";
        }
    }
}
