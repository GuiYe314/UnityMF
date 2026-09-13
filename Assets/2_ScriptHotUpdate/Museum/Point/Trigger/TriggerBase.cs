using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Point
{

    public class TriggerBase : MonoBehaviour
    {



        /// <summary>
        /// 互动
        /// </summary>
        [Header("按钮互动")]
        [SerializeField]
        protected List<InputRouting> _Interaction;

        public int subsequence { get;protected set; }


        public Action<InputRoutingSignal, GameObject,int> OnTrigger;

  

        public virtual void Initialize(int subsequence)
        {
            this.subsequence = subsequence;
        }


        public virtual void Enter(object param = null)
        {
        }

        public virtual void Exit(object param = null)
        {
        }


        public virtual void Shutdown()
        {

        }

  
    }

}

