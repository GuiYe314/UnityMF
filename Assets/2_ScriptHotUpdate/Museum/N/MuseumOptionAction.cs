using UnityEngine;

namespace HotUpdate.Museum.N
{
    public readonly struct MuseumOptionContext
    {
        public MuseumOptionContext(
            MuseumPoint point,
            MuseumPointSelectionModule selection,
            MuseumOption option,
            int optionIndex)
        {
            Point = point;
            Selection = selection;
            Option = option;
            OptionIndex = optionIndex;
        }

        public MuseumPoint Point { get; }
        public MuseumPointSelectionModule Selection { get; }
        public MuseumOption Option { get; }
        public int OptionIndex { get; }
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
