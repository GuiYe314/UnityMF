using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AOT.HotUpdate.Framework.Pooling
{
    /// <summary>
    /// 不暴露泛型参数的内部适配接口，供全局池统一管理和统计。
    /// </summary>
    internal interface IReferencePool
    {
        Type ReferenceType { get; }
        int UsingCount { get; }
        object AcquireObject();
        void ReleaseObject(object instance);
        void Prewarm(int count);
        int Trim(int targetUnusedCount);
        void Clear();
        ReferencePoolInfo GetInfo();
    }

    /// <summary>
    /// 单个具体类型的引用池。
    /// 空闲对象使用栈保存，使最近归还的实例优先被复用。
    /// </summary>
    /// <typeparam name="T">
    /// 需要池化的纯 C# 类型，必须实现 <see cref="IPoolable"/> 并具有公开无参构造函数。
    /// </typeparam>
    internal sealed class ReferencePool<T> : IReferencePool
        where T : class, IPoolable, new()
    {
        // 空闲实例栈：负责实际保存可复用对象。
        private readonly Stack<T> unused = new Stack<T>();

        // 空闲集合：O(1) 检测重复归还。
        private readonly HashSet<T> unusedSet =
            new HashSet<T>(ReferenceIdentityComparer<T>.Instance);

        // 使用中集合：保证只有从当前池借出的对象才能被归还。
        private readonly HashSet<T> usingSet =
            new HashSet<T>(ReferenceIdentityComparer<T>.Instance);

        // 只保护池内部状态；生命周期回调尽量放在锁外执行。
        private readonly object syncRoot = new object();

        private long acquireCount;
        private long releaseCount;
        private long createCount;
        private long discardCount;

        /// <summary>当前池所管理的实际类型。</summary>
        public Type ReferenceType => typeof(T);

        /// <summary>当前已经借出但尚未归还的实例数量。</summary>
        public int UsingCount
        {
            get
            {
                lock (syncRoot)
                    return usingSet.Count;
            }
        }

        /// <summary>
        /// 获取一个可用实例。优先复用空闲对象，池为空时才会 new。
        /// </summary>
        /// <exception cref="InvalidOperationException">池内部状态不一致。</exception>
        public T Acquire()
        {
            T instance;
            lock (syncRoot)
            {
                if (unused.Count > 0)
                {
                    instance = unused.Pop();
                    unusedSet.Remove(instance);
                }
                else
                {
                    instance = new T();
                    createCount++;
                }

                if (!usingSet.Add(instance))
                {
                    throw new InvalidOperationException(
                        $"Pool state is invalid for type '{typeof(T).FullName}'.");
                }

                acquireCount++;
            }

            try
            {
                instance.OnAcquire();
                return instance;
            }
            catch
            {
                // 初始化失败的对象状态不可信，直接丢弃。
                lock (syncRoot)
                {
                    usingSet.Remove(instance);
                    discardCount++;
                }

                throw;
            }
        }

        /// <summary>
        /// 将实例归还当前池。回收成功后对象所有权重新归池，调用方不得继续使用。
        /// </summary>
        /// <param name="instance">必须由当前池获取且尚未归还的实例。</param>
        /// <exception cref="ArgumentNullException">实例为空。</exception>
        /// <exception cref="InvalidOperationException">重复归还或实例不属于当前池。</exception>
        public void Release(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            // 先从使用中集合移除，避免生命周期回调间发生重复归还。
            lock (syncRoot)
            {
                if (!usingSet.Remove(instance))
                {
                    string reason = unusedSet.Contains(instance)
                        ? "The instance has already been released."
                        : "The instance was not acquired from this pool.";
                    throw new InvalidOperationException(
                        $"{reason} Type: '{typeof(T).FullName}'.");
                }
            }

            try
            {
                instance.OnRelease();
            }
            catch
            {
                // 清理失败的对象不可安全复用，不放回空闲栈。
                lock (syncRoot)
                    discardCount++;
                throw;
            }

            lock (syncRoot)
            {
                unused.Push(instance);
                unusedSet.Add(instance);
                releaseCount++;
            }
        }

        /// <summary>
        /// 预创建指定数量的空闲实例，用于减少首次业务高峰中的分配。
        /// 该参数表示“新增数量”，不是最终目标容量。
        /// </summary>
        public void Prewarm(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i++)
            {
                T instance = new T();

                // 新对象先执行清理，使预热对象与正常回收对象状态一致。
                instance.OnRelease();

                lock (syncRoot)
                {
                    unused.Push(instance);
                    unusedSet.Add(instance);
                    createCount++;
                }
            }
        }

        /// <summary>
        /// 将空闲实例数量裁剪至指定值。使用中的实例不受影响。
        /// </summary>
        /// <param name="targetUnusedCount">裁剪后允许保留的空闲数量。</param>
        /// <returns>本次实际移除的实例数量。</returns>
        public int Trim(int targetUnusedCount)
        {
            if (targetUnusedCount < 0)
                throw new ArgumentOutOfRangeException(nameof(targetUnusedCount));

            int removed = 0;
            lock (syncRoot)
            {
                while (unused.Count > targetUnusedCount)
                {
                    T instance = unused.Pop();
                    unusedSet.Remove(instance);
                    removed++;
                    discardCount++;
                }
            }

            return removed;
        }

        /// <summary>
        /// 清除全部空闲实例。已经借出的实例仍可在之后正常归还。
        /// </summary>
        public void Clear()
        {
            lock (syncRoot)
            {
                discardCount += unused.Count;
                unused.Clear();
                unusedSet.Clear();
            }
        }

        /// <summary>获取当前池的统计快照。</summary>
        public ReferencePoolInfo GetInfo()
        {
            lock (syncRoot)
            {
                return new ReferencePoolInfo(
                    typeof(T),
                    unused.Count,
                    usingSet.Count,
                    acquireCount,
                    releaseCount,
                    createCount,
                    discardCount);
            }
        }

        object IReferencePool.AcquireObject() => Acquire();

        void IReferencePool.ReleaseObject(object instance)
        {
            if (!(instance is T typedInstance))
            {
                throw new ArgumentException(
                    $"Instance must be assignable to '{typeof(T).FullName}'.",
                    nameof(instance));
            }

            Release(typedInstance);
        }

        /// <summary>
        /// 强制使用引用身份进行比较，避免业务类重写 Equals/GetHashCode 后影响池状态。
        /// </summary>
        private sealed class ReferenceIdentityComparer<TValue> : IEqualityComparer<TValue>
            where TValue : class
        {
            public static readonly ReferenceIdentityComparer<TValue> Instance =
                new ReferenceIdentityComparer<TValue>();

            public bool Equals(TValue x, TValue y) => ReferenceEquals(x, y);

            public int GetHashCode(TValue obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
