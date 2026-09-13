using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>点位运行时模型的统一生命周期入口。</summary>
    [DisallowMultipleComponent]
    public sealed class PointRuntimeController : MonoBehaviour
    {
        [Tooltip("自动收集当前节点及子节点上的 PointModuleBehaviour。")]
        [SerializeField]
        private bool autoCollectModules = true;

        [Tooltip("需要额外指定顺序时可手动加入；重复项会自动忽略。")]
        [SerializeField]
        private List<PointModuleBehaviour> configuredModules = new();

        private readonly List<IPointModule> runtimeModules = new();
        private MuseumPoint point;
        private bool initialized;
        private bool entered;

        public bool IsInitialized => initialized;
        public bool IsEntered => entered;

        public void Initialize(MuseumPoint owner)
        {
            if (initialized)
            {
                return;
            }

            point = owner;
            BuildModuleList();

            foreach (IPointModule module in runtimeModules)
            {
                module.Initialize(point);
            }

            initialized = true;
        }

        public void EnterPoint()
        {
            if (entered)
            {
                return;
            }

            if (!initialized)
            {
                Debug.LogError(
                    $"{nameof(PointRuntimeController)} 必须先 Initialize。",
                    this);
                return;
            }

            entered = true;
            foreach (IPointModule module in runtimeModules)
            {
                module.EnterPoint();
            }
        }

        public void ExitPoint(PointExitReason reason)
        {
            if (!initialized || !entered)
            {
                return;
            }

            entered = false;
            for (int i = runtimeModules.Count - 1; i >= 0; i--)
            {
                runtimeModules[i].ExitPoint(reason);
            }
        }

        public void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            ExitPoint(PointExitReason.Shutdown);

            for (int i = runtimeModules.Count - 1; i >= 0; i--)
            {
                runtimeModules[i].Shutdown();
            }

            runtimeModules.Clear();
            point = null;
            initialized = false;
        }

        private void BuildModuleList()
        {
            runtimeModules.Clear();
            HashSet<IPointModule> uniqueModules = new();

            if (autoCollectModules)
            {
                PointModuleBehaviour[] discovered =
                    GetComponentsInChildren<PointModuleBehaviour>(true);

                foreach (PointModuleBehaviour module in discovered)
                {
                    AddModule(module, uniqueModules);
                }
            }

            foreach (PointModuleBehaviour module in configuredModules)
            {
                AddModule(module, uniqueModules);
            }
        }

        private void AddModule(
            PointModuleBehaviour module,
            HashSet<IPointModule> uniqueModules)
        {
            if (module == null || !uniqueModules.Add(module))
            {
                return;
            }

            runtimeModules.Add(module);
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}
