using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 展品内部的 MRTK 移入/移出/点击适配器。
    /// 它不参与点位进入仲裁；当前点位激活来源仍然只有距离触发。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MuseumInteractionTarget))]
    public sealed class MuseumOptionMrtkInput :
        TargetMessageInputBehaviour,
        IMixedRealityFocusHandler,
        IMixedRealityPointerHandler
    {
        public void OnFocusEnter(FocusEventData eventData)
        {
            PublishInput(
                InteractionInputPhase.HoverEnter,
                InteractionInputSourceType.Mrtk);
        }

        public void OnFocusExit(FocusEventData eventData)
        {
            PublishInput(
                InteractionInputPhase.HoverExit,
                InteractionInputSourceType.Mrtk);
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            PublishInput(
                InteractionInputPhase.Click,
                InteractionInputSourceType.Mrtk);
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
        }

        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
        }
    }
}
