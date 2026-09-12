using HotUpdate.Point;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.FunctionsSpecial
{
    [Serializable]
    public class GameObjectTriggerInfoActive : FunctionBase
    {
        [SerializeField]
        protected List<GameObjectTriggerInfoActiveData> datas;


        public override void Initialize(GameObject obj)
        {
            base.Initialize(obj);
        }

        public override void Enter(InteractionData interactionData = null)
        {

            foreach (var data in datas)
            {
                data.activeObj.SetActive(data.triggerObj == interactionData.targetObj);
            }


        }

        public override void Exit(InteractionData interactionData = null)
        {


        }


        [Serializable]
        protected class GameObjectTriggerInfoActiveData
        {
            public GameObject triggerObj;
            public GameObject activeObj;



        }
    }




}

