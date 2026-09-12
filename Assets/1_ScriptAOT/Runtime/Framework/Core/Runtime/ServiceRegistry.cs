using System;
using System.Collections.Generic;

namespace AOT.HotUpdate.Framework
{
    /// <summary>
    /// IServiceRegistry 的轻量实现。
    /// 它只负责“接口类型 -> 实例”的映射，不做自动反射和自动构造，
    /// 依赖关系保持显式，便于在 Unity 与 HybridCLR 环境中排查问题。
    /// </summary>
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> services =  new Dictionary<Type, object>();

        private readonly object syncRoot = new object();

        /// <summary>
        /// 以 TService 作为键注册实例。
        /// 同一种接口重复注册通常意味着组合入口配置错误，因此直接抛出异常。
        /// </summary>
        public void Register<TService>(TService service) where TService : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var serviceType = typeof(TService);
            lock (syncRoot)
            {
                if (services.ContainsKey(serviceType))
                {
                    throw new InvalidOperationException(
                        "Service already registered: " + serviceType.FullName);
                }

                services.Add(serviceType, service);
            }
        }

        /// <summary>
        /// 取得必需服务；未注册时快速失败，避免空引用延迟到业务流程中才出现。
        /// </summary>
        public TService Resolve<TService>()
            where TService : class
        {
            if (TryResolve<TService>(out var service))
                return service;

            throw new InvalidOperationException(
                "Service is not registered: " + typeof(TService).FullName);
        }

        /// <summary>
        /// 尝试取得可选服务，不存在时返回 false。
        /// </summary>
        public bool TryResolve<TService>(out TService service)
            where TService : class
        {
            lock (syncRoot)
            {
                if (services.TryGetValue(typeof(TService), out var value))
                {
                    service = (TService)value;
                    return true;
                }
            }

            service = null;
            return false;
        }
    }
}
