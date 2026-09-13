using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace HotUpdate.Museum.N
{
    /// <summary>
    /// 展品内部的 MRTK 移入/移出/点击适配器。
    /// 它不参与点位进入仲裁；当前点位激活来源仍然只有距离触发。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MuseumOptionView))]
    public sealed class MuseumOptionMrtkInput :
        MonoBehaviour,
        IMixedRealityFocusHandler,
        IMixedRealityPointerHandler
    {
        [SerializeField]
        private MuseumOptionView optionView;

        private void Awake()
        {
            if (optionView == null)
            {
                optionView = GetComponent<MuseumOptionView>();
            }
        }

        public void OnFocusEnter(FocusEventData eventData)
        {
            optionView?.NotifyHoverEnter();
        }

        public void OnFocusExit(FocusEventData eventData)
        {
            optionView?.NotifyHoverExit();
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            optionView?.NotifyClick();
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
