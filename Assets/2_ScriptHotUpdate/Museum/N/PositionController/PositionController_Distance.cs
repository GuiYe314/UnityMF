using AOT.HotUpdate.Framework;
using HotUpdate.Museum.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace HotUpdate.Museum.Controller
{
    public class PositionController_Distance : PositionControllerBsae
    {

        [SerializeField]
        protected float _Distance = 3f;
        [SerializeField]
        protected float _DistanceExit = 5f;

        public override void Initialize(IServiceRegistry serviceRegistry)
        {
            base.Initialize(serviceRegistry);
            pointType = pointType.Distance;
        }

        public override void UpdatePoint()
        {
            base.UpdatePoint();

            if (distanceToCamera < _Distance)
            {
                OnEnter?.Invoke(this);
            }

            if (distanceToCamera > _DistanceExit)
            {
                OnExit?.Invoke(this);
            }

        }


    }

}

