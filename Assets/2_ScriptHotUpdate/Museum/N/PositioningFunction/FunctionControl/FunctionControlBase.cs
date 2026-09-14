using AOT.HotUpdate.Framework.Pooling;
using HotUpdate.Museum.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace HotUpdate.Museum.Function
{
    public class FunctionControlBase : MonoBehaviour
    {



        public virtual void Initialize()
        {

        }



        public virtual void Open(FunctionDataContext functionBasesData)
        {




        }


        public virtual void Clos()
        {

        }

    }



    public class FunctionDataContext : IPoolable
    {

        public ImportRoutingSignal inputRoutingSignal { get; protected set; }

        public ObjState objState { get; protected set; }
        public bool objStateReversal { get; set; }

        public GameObject obj { get; protected set; }

        public FunctionEventName eventName { get; protected set; }

        public static FunctionDataContext Acquire(GameObject obj, bool objStateReversal, ImportRoutingSignal importRoutingSignal, ObjState objState, FunctionEventName eventName)
        {
            FunctionDataContext functionBasesData = ReferencePool.Acquire<FunctionDataContext>();
            functionBasesData.obj = obj;
            functionBasesData.objState = objState;
            functionBasesData.objStateReversal = objStateReversal;
            functionBasesData.inputRoutingSignal = importRoutingSignal;
            functionBasesData.eventName = eventName;


            return functionBasesData;
        }



        public static FunctionDataContext Acquire(FunctionDataContext functionBasesDatas)
        {
            FunctionDataContext functionBasesData = ReferencePool.Acquire<FunctionDataContext>();
            functionBasesData.obj = functionBasesDatas.obj;
            functionBasesData.objState = functionBasesDatas.objState;
            functionBasesData.objStateReversal = functionBasesDatas.objStateReversal;
            functionBasesData.inputRoutingSignal = functionBasesDatas.inputRoutingSignal;
            functionBasesData.eventName = functionBasesDatas.eventName;
            return functionBasesData;
        }

        public void Release()
        {

            ReferencePool.Release(this);
        }




        public void OnAcquire()
        {
            inputRoutingSignal = ImportRoutingSignal.None;
            objStateReversal = false;
            objState = ObjState.None;
            eventName = FunctionEventName.None;
            obj = null;
        }


        public void OnRelease()
        {
            inputRoutingSignal = ImportRoutingSignal.None;
            objStateReversal = false;
            objState = ObjState.None;
            eventName = FunctionEventName.None;
            obj = null;

        }


    }

}


