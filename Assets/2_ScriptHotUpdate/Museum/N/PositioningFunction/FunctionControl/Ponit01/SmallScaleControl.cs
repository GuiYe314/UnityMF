using HotUpdate.Museum.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Museum.Function
{
    public class SmallScaleControl : FunctionControlBase
    {
        public override void Open(ImportRoutingSignal importRoutingSignal, GameObject obj)
        {
            base.Open(importRoutingSignal, obj);

            switch (importRoutingSignal)
            {
                case ImportRoutingSignal.None:
                    break;
                case ImportRoutingSignal.Enter:
                    break;
                case ImportRoutingSignal.Exit:
                    break;
                case ImportRoutingSignal.Click:
                    break;
                case ImportRoutingSignal.Down:
                    break;
                case ImportRoutingSignal.Dragged:
                    break;
                case ImportRoutingSignal.Up:
                    break;
                default:
                    break;
            }
        }









    }

}
