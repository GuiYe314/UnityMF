using AOT.HotUpdate.Framework.Pooling;
using HotUpdate.Museum.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace HotUpdate.Museum.Function
{
    public class FunctionControlBase : MonoBehaviour
    {
        [SerializeField]
        protected ImportBase importBase;

        private void Start()
        {
            importBase.OnTrigger += Open;
            importBase = importBase == null ? GetComponent<ImportBase>() : importBase;
        }

        public virtual void Open(ImportRoutingSignal importRoutingSignal, GameObject obj)
        {

        }


        public virtual void Clos()
        {

        }

    }



    public class FunctionBasesData : IPoolable
    {

        public ImportRoutingSignal importRoutingSignal;

        public ObjState objState;
        public bool objStateReversal;

        public GameObject obj;


        public static FunctionBasesData Acquire(GameObject obj, bool objStateReversal, ImportRoutingSignal importRoutingSignal, ObjState objState)
        {
            FunctionBasesData functionBasesData = ReferencePool.Acquire<FunctionBasesData>();
            functionBasesData.obj = obj;
            functionBasesData.objState = objState;
            functionBasesData.objStateReversal = objStateReversal;
            functionBasesData.importRoutingSignal = importRoutingSignal;


            return functionBasesData;
        }



        public static FunctionBasesData Acquire(FunctionBasesData functionBasesDatas)
        {
            FunctionBasesData functionBasesData = ReferencePool.Acquire<FunctionBasesData>();
            functionBasesData.obj = functionBasesDatas.obj;
            functionBasesData.objState = functionBasesDatas.objState;
            functionBasesData.objStateReversal = functionBasesDatas.objStateReversal;
            functionBasesData.importRoutingSignal = functionBasesDatas.importRoutingSignal;


            return functionBasesData;
        }

        public void Release()
        {

            ReferencePool.Release(this);
        }




        public void OnAcquire()
        {
            importRoutingSignal = ImportRoutingSignal.None;
            objStateReversal = false;
            objState = ObjState.None;
            obj = null;
        }


        public void OnRelease()
        {
            importRoutingSignal = ImportRoutingSignal.None;
            objStateReversal = false;
            objState = ObjState.None;
            obj = null;

        }


    }

}


