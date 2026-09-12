using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AOT.HotUpdate.Framework
{
    /// <summary>
    /// APP 级模块宿主。
    /// 它按注册顺序初始化模块，并按相反顺序关闭模块，
    /// 例如先初始化资源服务再初始化 MR，退出时则先关闭 MR 再释放资源。
    /// </summary>
    public sealed class AppModuleHost
    {
        private readonly List<IAppModule> modules =
            new List<IAppModule>();

        private readonly List<IAppModule> initializedModules =
            new List<IAppModule>();

        public bool IsInitialized { get; private set; }

        public IReadOnlyList<IAppModule> Modules => modules;

        /// <summary>
        /// 在宿主启动前注册模块。ModuleId 必须唯一。
        /// </summary>
        public void Register(IAppModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));
            if (IsInitialized || initializedModules.Count > 0)
            {
                throw new InvalidOperationException(
                    "Cannot register a module after initialization started.");
            }
            if (string.IsNullOrWhiteSpace(module.ModuleId))
                throw new ArgumentException("ModuleId cannot be empty.", nameof(module));
            if (modules.Any(x =>
                    string.Equals(x.ModuleId, module.ModuleId, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Duplicate module id: " + module.ModuleId);
            }

            modules.Add(module);
        }

        /// <summary>
        /// 顺序初始化所有模块。
        /// 某个模块失败时，已经成功初始化的模块会按逆序回滚关闭。
        /// </summary>
        public async Task InitializeAllAsync(
            AppModuleContext context,
            CancellationToken cancellationToken)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (IsInitialized || initializedModules.Count > 0)
                throw new InvalidOperationException("Module host already initialized.");

            try
            {
                foreach (var module in modules)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await module.InitializeAsync(context, cancellationToken);
                    initializedModules.Add(module);
                }

                IsInitialized = true;
            }
            catch
            {
                await ShutdownInitializedModulesAsync(CancellationToken.None);
                throw;
            }
        }

        /// <summary>
        /// 按初始化的相反顺序关闭模块，保证上层依赖先退出。
        /// </summary>
        public async Task ShutdownAllAsync(CancellationToken cancellationToken)
        {
            await ShutdownInitializedModulesAsync(cancellationToken);
            IsInitialized = false;
        }

        private async Task ShutdownInitializedModulesAsync(
            CancellationToken cancellationToken)
        {
            List<Exception> errors = null;

            for (var index = initializedModules.Count - 1; index >= 0; index--)
            {
                try
                {
                    await initializedModules[index]
                        .ShutdownAsync(cancellationToken);
                }
                catch (Exception exception)
                {
                    if (errors == null)
                        errors = new List<Exception>();

                    errors.Add(exception);
                }
            }

            initializedModules.Clear();

            if (errors != null)
                throw new AggregateException("One or more modules failed to shut down.", errors);
        }
    }
}
