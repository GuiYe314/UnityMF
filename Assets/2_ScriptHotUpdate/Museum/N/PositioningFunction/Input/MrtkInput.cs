
using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Museum.Input
{

    public class MrtkInput : ImportBase, IMixedRealityFocusHandler, IMixedRealityPointerHandler
    {


        public void OnFocusEnter(FocusEventData eventData)
        {
            OnTrigger?.Invoke(ImportRoutingSignal.Enter, gameObject);
        }

        public void OnFocusExit(FocusEventData eventData)
        {
            OnTrigger?.Invoke(ImportRoutingSignal.Exit, gameObject);
        }

        public void OnPointerClicked(
            MixedRealityPointerEventData eventData)
        {
            OnTrigger?.Invoke(ImportRoutingSignal.Click, gameObject);
        }

        public void OnPointerDown(
            MixedRealityPointerEventData eventData)
        {
            OnTrigger?.Invoke(ImportRoutingSignal.Down, gameObject);

        }

        public void OnPointerDragged(
            MixedRealityPointerEventData eventData)
        {
            OnTrigger?.Invoke(ImportRoutingSignal.Dragged, gameObject);
        }

        public void OnPointerUp(
            MixedRealityPointerEventData eventData)
        {
            OnTrigger?.Invoke(ImportRoutingSignal.Up, gameObject);
        }
    }




}
