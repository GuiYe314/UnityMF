using AOT.HotUpdate.Framework;
using Cysharp.Threading.Tasks;
using HotUpdate.Museum.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Museum.Manager
{

    /// <summary>
    /// 点位管理
    /// </summary>
    public class PointsManager : MonoBehaviour
    {
        PositionControllerBsae[] positionControllers;

        /// <summary>
        /// 服务注册
        /// </summary>
        private IServiceRegistry serviceRegistry;



        /// <summary>
        /// 点位检测频率
        /// </summary>
        [SerializeField]
        protected float inspectionFrequency = 0.1f;

        protected Transform targetCamera = null;


        #region 点位缓存信息

        [Header("点位缓存信息")]

        /// <summary>
        /// 点位实例化最大距离,用于点位资源缓存
        /// </summary>
        [SerializeField]
        protected float instantiationDistance = 8f;
        /// <summary>
        /// 点位实例化最小距离,用于点位资源缓存
        /// </summary>
        [SerializeField]
        protected float maxInstantiationDistance = 12f;
        /// <summary>
        /// 点位实例化时间,用于点位资源缓存
        /// </summary>
        [SerializeField]
        protected float exitInstantiationTime = 5f;


    

        /// <summary>
        /// 所有点位控制列表
        /// </summary>
        protected PositionControllerBsae[] pointBases;
        /// <summary>
        /// 当前加载资源点位
        /// </summary>
        protected List<PositionControllerBsae> loadPoint = new();
        /// <summary>
        /// 当前符合点位
        /// </summary>
        protected PositionControllerBsae triggerPoint;
        #endregion



        public void Initialize(IServiceRegistry serviceRegistry)
        {
            this.serviceRegistry = serviceRegistry;
            targetCamera = Camera.main.transform;


            pointBases = GetComponentsInChildren<PositionControllerBsae>();
            positionControllers = GetComponentsInChildren<PositionControllerBsae>();
            foreach (var point in positionControllers)
            {
                point.Initialize(serviceRegistry);
                point.OnEnter += OnEnter;
                point.OnExit += OnExit;
            }

            //开启点位检测
            UpdatePoint().Forget();
        }


        #region 点位控制


        /// <summary>
        /// 点位移入
        /// </summary>
        /// <param name="position"></param>
        private void OnEnter(PositionControllerBsae position)
        {

            if (triggerPoint != null && position == triggerPoint) return;

            PositionControllerBsae positionControllerBsae = null;
            switch (position.pointType)
            {
                case pointType.Collide:
                    positionControllerBsae = position;
                    break;
                case pointType.Distance:
                    if (triggerPoint&& triggerPoint.distanceToCamera < position.distanceToCamera)
                    {
                        positionControllerBsae = position;
                        break;
                    }
                    positionControllerBsae = position;
                    break;
            
                default:
                    break;
            }


            if(positionControllerBsae != null)
            {

                if(triggerPoint != null)
                {
                    triggerPoint.Close();
                }

                triggerPoint = positionControllerBsae;
                triggerPoint.Open();

            }

        }

        /// <summary>
        /// 移出
        /// </summary>
        /// <param name="position"></param>
        private void OnExit(PositionControllerBsae position)
        {
            if (triggerPoint != null)
            {
                triggerPoint.Close();
                triggerPoint=null;
            }
        }


        #endregion

        #region 点位检测

        protected async UniTask UpdatePoint()
        {

            while (true)
            {



                LoadMode();
                await UniTask.Delay((int)(inspectionFrequency * 1000));
            }


        }

        /// <summary>
        /// 加载模型
        /// </summary>
        protected void LoadMode()
        {
            foreach (var point in pointBases)
            {

                //检测点位控制器与相机的距离
                point.distanceToCamera = Vector3.Distance(targetCamera.transform.position, transform.position);

                //更新点位
                point.UpdatePoint();

                //添加加载模型点位控制器
                if (!loadPoint.Contains(point) && point.distanceToCamera < maxInstantiationDistance)
                {
                    loadPoint.Add(point);

                    //加载模型
                    point.Load();
                }
                else if (loadPoint.Contains(point) && point.distanceToCamera > maxInstantiationDistance)
                {

                    //卸载模型
                    point.Destroy();
                    loadPoint.Remove(point);
                }

            }
        }


        #endregion


        public void Shutdown()
        {

        }
    }

}


