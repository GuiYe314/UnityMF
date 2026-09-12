using AOT.HotUpdate.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Point
{


    /// <summary>
    /// 距离检测点位
    /// </summary>
    public class DistancePoint : PointBase
    {
        [SerializeField]
        protected float _Distance = 3f;
        [SerializeField]
        protected float _DistanceExit = 5f;


        public override void Initialize(IServiceRegistry serviceRegistry)
        {
            base.Initialize(serviceRegistry);
            targetType = TargetType.Distance;
        }

        public override void Enter(PointBase pointBase)
        {
            base.Enter(pointBase);

        }


        public override void Exit(PointBase pointBase)
        {
            base.Exit(pointBase);


        }


        public override void UnpdataPoint()
        {
            base.UnpdataPoint();

            //点位进入检测，不能大于触发距离，当前点位不能是自己，当前点位不能是碰撞点位，当前点位距离摄像机的距离必须大于当前点位距离摄像机的距离
            if (
                (currentPoint == null && distanceToCamera < _Distance)||
                (distanceToCamera < _Distance
                && currentPoint != null 
                && currentPoint != this
                && currentPoint.targetType != TargetType.Collide
                && currentPoint.distanceToCamera > distanceToCamera)
                )
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

