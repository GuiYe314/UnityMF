
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace HotUpdate.Point
{

    /// <summary>
    /// ∏∫‘MRTK ‰»Î
    /// </summary>
    [DisallowMultipleComponent]
    public class MrtkTrigger : TriggerBase, IMixedRealityFocusHandler, IMixedRealityPointerHandler
    {

        public override void Initialize(int subsequence)
        {
            base.Initialize(subsequence);

        }

        public override void Enter(object param = null)
        {
            base.Enter(param);
            foreach (var interaction in _Interaction)
            {
                interaction.Enter();
        

            }

        }

        public override void Exit(object param = null)
        {
            base.Exit(param);

            foreach (var interaction in _Interaction)
            {
                interaction.Exit();
            }
        }



        public void OnFocusEnter(FocusEventData eventData)
        {
            OnTrigger?.Invoke(InputRoutingSignal.Enter, gameObject, subsequence);
        }

        public void OnFocusExit(FocusEventData eventData)
        {
            OnTrigger?.Invoke(InputRoutingSignal.Exit, gameObject, subsequence);
        }

        public void OnPointerClicked(
            MixedRealityPointerEventData eventData)
        {
            OnTrigger?.Invoke(InputRoutingSignal.Click, gameObject, subsequence);
        }

        public void OnPointerDown(
            MixedRealityPointerEventData eventData)
        {
            OnTrigger?.Invoke(InputRoutingSignal.Down, gameObject, subsequence);

        }

        public void OnPointerDragged(
            MixedRealityPointerEventData eventData)
        {
            OnTrigger?.Invoke(InputRoutingSignal.Dragged, gameObject, subsequence);
        }

        public void OnPointerUp(
            MixedRealityPointerEventData eventData)
        {
            OnTrigger?.Invoke(InputRoutingSignal.Up, gameObject, subsequence);
        }
    }

}
