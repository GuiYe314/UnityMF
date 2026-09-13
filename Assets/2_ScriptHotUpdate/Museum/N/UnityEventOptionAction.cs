using UnityEngine;
using UnityEngine.Events;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 无需再写脚本的通用扩展技能，可在 Inspector 中组合动画、特效和业务入口。
    /// </summary>
    public sealed class UnityEventOptionAction : MuseumOptionAction
    {
        [SerializeField]
        private UnityEvent onSelected;

        [SerializeField]
        private UnityEvent onDeselected;

        [SerializeField]
        private UnityEvent onPointExit;

        [SerializeField]
        private UnityEvent onShutdown;

        public override void OnSelected(MuseumOptionContext context)
        {
            onSelected?.Invoke();
        }

        public override void OnDeselected(MuseumOptionContext context)
        {
            onDeselected?.Invoke();
        }

        public override void OnPointExit(
            MuseumOptionContext context,
            PointExitReason reason)
        {
            onPointExit?.Invoke();
        }

        public override void Shutdown(MuseumOptionContext context)
        {
            onShutdown?.Invoke();
        }
    }
}
