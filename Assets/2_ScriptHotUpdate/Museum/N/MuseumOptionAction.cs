using UnityEngine;

namespace HotUpdate.Museum.N
{
    public readonly struct MuseumOptionContext
    {
        public MuseumOptionContext(
            MuseumPoint point,
            MuseumInteractionTarget target,
            PointMessageBus messages)
        {
            Point = point;
            Target = target;
            Messages = messages;
        }

        public MuseumPoint Point { get; }
        public MuseumInteractionTarget Target { get; }
        public PointMessageBus Messages { get; }
    }

    /// <summary>
    /// 单个展品的可选扩展技能。不同展品可以挂不同组合，不需要修改选择控制器。
    /// </summary>
    public abstract class MuseumOptionAction : MonoBehaviour
    {
        public virtual void Initialize(MuseumOptionContext context)
        {
        }

        public virtual void OnSelected(MuseumOptionContext context)
        {
        }

        public virtual void OnDeselected(MuseumOptionContext context)
        {
        }

        public virtual void OnPointExit(
            MuseumOptionContext context,
            PointExitReason reason)
        {
        }

        public virtual void Shutdown(MuseumOptionContext context)
        {
        }
    }
}
