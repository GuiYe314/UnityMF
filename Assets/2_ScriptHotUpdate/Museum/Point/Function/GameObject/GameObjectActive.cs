using HotUpdate.Attributes;
using HotUpdate.Point;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Functions
{
    [Serializable]
    public class GameObjectActive : FunctionBase
    {
        [SerializeField]
        protected List<GameObject> objs;

        [SerializeField]
        protected bool initActive = false;

        public override void Initialize(GameObject obj)
        {
            base.Initialize(obj);
        }

        public override void Enter(InteractionData interactionData = null)
        {
       
            foreach (var item in objs)
            {
                item.SetActive(initActive);
            }

        }

        public override void Exit(InteractionData interactionData = null)
        { 
     
            foreach (var item in objs)
            {
                item.SetActive(!initActive);
            }
        }
        
    }
}


