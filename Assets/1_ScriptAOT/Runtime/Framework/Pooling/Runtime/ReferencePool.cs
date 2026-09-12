using System;
using System.Collections.Generic;

namespace AOT.HotUpdate.Framework.Pooling
{
    /// <summary>
    /// 纯 C# 引用池的全局入口。
    /// 每个具体类型拥有独立的 <see cref="ReferencePool{T}"/>，
    /// 用于减少短生命周期数据对象带来的 GC 分配。
    /// </summary>
    /// <remarks>
    /// 不要用它管理 GameObject、Component、ScriptableObject 等 UnityEngine.Object。
    /// 池只负责实例复用，不负责业务对象的自动释放；Acquire 与 Release 必须成对出现。
    /// </remarks>
    public static class ReferencePool
    {
        // 类型到具体泛型池的映射，仅在首次使用某类型时创建。
        private static readonly Dictionary<Type, IReferencePool> Pools =
            new Dictionary<Type, IReferencePool>();

        private static readonly object SyncRoot = new object();

        /// <summary>当前已经创建的类型池数量。</summary>
        public static int PoolCount
        {
            get
            {
                lock (SyncRoot)
                    return Pools.Count;
            }
        }

        /// <summary>
        /// 获取一个 T 实例。池中没有空闲对象时会通过公开无参构造函数创建。
        /// </summary>
        public static T Acquire<T>()
            where T : class, IPoolable, new()
        {
            return GetOrCreatePool<T>().Acquire();
        }

        /// <summary>
        /// 以编译期类型归还实例，推荐在已知具体类型时使用。
        /// </summary>
        public static void Release<T>(T instance)
            where T : class, IPoolable, new()
        {
            GetOrCreatePool<T>().Release(instance);
        }

        /// <summary>
        /// 以运行时类型归还实例。
        /// 该对象必须先由 <see cref="Acquire{T}"/> 取得，因此对应类型池一定已经存在。
        /// 此重载不会通过反射动态创建泛型池。
        /// </summary>
        public static void Release(IPoolable instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            IReferencePool pool;
            Type type = instance.GetType();
            lock (SyncRoot)
            {
                if (!Pools.TryGetValue(type, out pool))
                {
                    throw new InvalidOperationException(
                        $"No reference pool exists for type '{type.FullName}'. " +
                        "Acquire the instance from ReferencePool before releasing it.");
                }
            }

            pool.ReleaseObject(instance);
        }

        /// <summary>
        /// 为 T 类型新增指定数量的预热实例。
        /// </summary>
        public static void Prewarm<T>(int count)
            where T : class, IPoolable, new()
        {
            GetOrCreatePool<T>().Prewarm(count);
        }

        /// <summary>
        /// 将 T 类型池的空闲对象数量裁剪到指定值。
        /// </summary>
        /// <returns>实际移除的数量。</returns>
        public static int Trim<T>(int targetUnusedCount)
            where T : class, IPoolable, new()
        {
            return GetOrCreatePool<T>().Trim(targetUnusedCount);
        }

        /// <summary>
        /// 清除 T 类型池内的所有空闲对象，不影响已经借出的对象。
        /// </summary>
        public static void Clear<T>()
            where T : class, IPoolable, new()
        {
            GetOrCreatePool<T>().Clear();
        }

        /// <summary>
        /// 清除所有类型池内的空闲对象。
        /// 类型池注册和仍在使用中的对象会保留，之后仍可正常归还。
        /// </summary>
        public static void ClearAll()
        {
            IReferencePool[] snapshot;
            lock (SyncRoot)
            {
                snapshot = new IReferencePool[Pools.Count];
                Pools.Values.CopyTo(snapshot, 0);
            }

            foreach (IReferencePool pool in snapshot)
                pool.Clear();
        }

        /// <summary>获取 T 类型对象池的统计快照。</summary>
        public static ReferencePoolInfo GetInfo<T>()
            where T : class, IPoolable, new()
        {
            return GetOrCreatePool<T>().GetInfo();
        }

        /// <summary>获取所有已创建类型池的统计快照。</summary>
        public static ReferencePoolInfo[] GetAllInfos()
        {
            IReferencePool[] snapshot;
            lock (SyncRoot)
            {
                snapshot = new IReferencePool[Pools.Count];
                Pools.Values.CopyTo(snapshot, 0);
            }

            var infos = new ReferencePoolInfo[snapshot.Length];
            for (int i = 0; i < snapshot.Length; i++)
                infos[i] = snapshot[i].GetInfo();

            return infos;
        }

        /// <summary>
        /// 获取 T 对应的类型池；第一次调用时创建并注册。
        /// 仅使用泛型构造，不依赖 Activator 或运行时泛型反射。
        /// </summary>
        private static ReferencePool<T> GetOrCreatePool<T>()
            where T : class, IPoolable, new()
        {
            Type type = typeof(T);
            lock (SyncRoot)
            {
                if (Pools.TryGetValue(type, out IReferencePool existing))
                    return (ReferencePool<T>)existing;

                var created = new ReferencePool<T>();
                Pools.Add(type, created);
                return created;
            }
        }
    }
}
