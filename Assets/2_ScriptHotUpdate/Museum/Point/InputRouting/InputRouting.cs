using AOT.HotUpdate.Framework.Pooling;

using System;

using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Point
{

    /// <summary>
    /// 输入路由。根据输入去执行不同信息
    /// </summary>
    [Serializable]
    public class InputRouting : LoadingModeControllerBase
    {

        /// <summary>
        /// 执行流
        /// </summary>
        [SerializeField]
        protected List<InputRoutingWorkflow> executionInteraction = new();





        public override void Initialize(LoadingModelManage loadingModelManage)
        {
            base.Initialize(loadingModelManage);

            foreach (var item in executionInteraction)
            {
                item.Initialize(this);
            }
        }


 

        public virtual void Enter(object param = null)
        {
        }

        public virtual void Exit(object param = null)
        {
        }


    }


    /// <summary>
    /// 输入路由执行流
    /// </summary>
    [Serializable]
    public class InputRoutingWorkflow
    {

        [SerializeField]
        protected string name;

        [SerializeField]
        protected InputRoutingSignal interactionSignal;

        [SerializeField]
        protected ObjectState objectState;

       [SerializeField]
        protected FunctionRoute route;

        [SerializeField]
        protected bool objectStateNOT;
        /// <summary>
        /// 触发器
        /// </summary>
        [SerializeField]
        protected List<TriggerBase> triggerBases;
        /// <summary>
        /// 执行方法控制器
        /// </summary>
        [SerializeField]
        protected List<FunctionControl> functionControl;

        protected InputRouting interaction = null;

        public void Initialize(InputRouting interaction)
        {

            ReferencePool.Prewarm<InteractionData>(30);
            this.interaction = interaction;



            foreach (var item in functionControl)
            {
                item.Initialize(interaction);
            }

            for (int i = 0; i < triggerBases.Count; i++)
            {
                triggerBases[i].Initialize(i);
                triggerBases[i].OnTrigger += Handle;

            }

        }

        public void Handle(InputRoutingSignal signal, GameObject source,int subsequence)
        {

            try
            {

        

                if (signal != interactionSignal) return;

                InteractionData interactionData = InteractionData.Acquire(interaction,
                    source, interactionSignal, objectState, objectStateNOT
                    );

                switch (route)
                {
                    case FunctionRoute.oneToMany:

                        foreach (var item in functionControl)
                        {
                            item.Handle(interactionData);
                        }

                        break;
                    case FunctionRoute.oneToOne:

                        if(functionControl.Count > subsequence)
                            functionControl[subsequence].Handle(interactionData);

                        break;
                    case FunctionRoute.manyToOne:

                        functionControl[0].Handle(interactionData);

                        break;
                    case FunctionRoute.manyToMany:


                        foreach (var item in functionControl)
                        {
                            item.Handle(interactionData);
                        }

                        break;
                    default:
                        break;
                }


                interactionData.Release();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
           

        }


    }


    public class InteractionData : IPoolable
    {

        public InputRouting interaction;

        /// <summary>
        /// 触发物体
        /// </summary>
        public GameObject targetObj { get; set; }

        public InputRoutingSignal interactionSignal;

        public ObjectState objectStart;
        public bool objectStateNOT;


        public static InteractionData Acquire(InputRouting interaction,
            GameObject targetObj,
            InputRoutingSignal interactionSignal,
            ObjectState objectStart,
             bool objectStateNOT

            )
        {
            InteractionData interactionData = ReferencePool.Acquire<InteractionData>();

            interactionData.interactionSignal = interactionSignal;
            interactionData.objectStart = objectStart;
            interactionData.interaction = interaction;
            interactionData.objectStateNOT = objectStateNOT;

            interactionData.targetObj = targetObj;
            return interactionData;
        }



        public static InteractionData Acquire(InteractionData interactionData)
        {
            InteractionData interactionDataNew = ReferencePool.Acquire<InteractionData>();

            interactionDataNew.interactionSignal = interactionData.interactionSignal;
            interactionDataNew.objectStart = interactionData.objectStart;
            interactionDataNew.interaction = interactionData.interaction;
            interactionDataNew.objectStateNOT = interactionData.objectStateNOT;

            interactionDataNew.targetObj = interactionData.targetObj;

            return interactionDataNew;
        }

        public void Release()
        {

            ReferencePool.Release(this);
        }




        public void OnAcquire()
        {
            
        }


        public void OnRelease()
        {
            interaction = null;
            targetObj = null;
            interactionSignal = InputRoutingSignal.None;
            objectStart = ObjectState.None;
            objectStateNOT = false;
        }
    }



}
