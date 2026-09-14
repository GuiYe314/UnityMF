using AOT.HotUpdate.Framework.Pooling;
using HotUpdate.Museum.Input;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace HotUpdate.Museum.Function
{
    public class FunctionBases : MonoBehaviour
    {

 

        [SerializeField]
        FunctionState[] functionStates = new FunctionState[3];


        public virtual void Initialize() { }

        public virtual void Open(FunctionDataContext functionBasesData)
        {
            int index = (int)functionBasesData.objState;
            //判断objStateReversal
            if (functionBasesData.objStateReversal)
            {
                functionStates[index].isActive = false ;
                foreach (var item in functionStates)
                {
                    if (!item.isActive) continue;
                    item.Open(functionBasesData);
                }

            }
            else
            {
                functionStates[index].isActive = true;
                functionStates[index].Open(functionBasesData);
            }

        }

        /// <summary>
        /// 事件状态
        /// </summary>
        public class FunctionState
        {

            public bool isActive = false;

            public virtual void Open(FunctionDataContext functionBasesData)
            {

            }
        }

    }



}
