using AOT.HotUpdate.Framework;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



namespace HotUpdate.Point
{

    /// <summary>
    /// 博物馆点位控制器
    /// 只做点位的显示和隐藏，不做点位的逻辑处理
    /// </summary>
    public class MuseumPointsController : MonoBehaviour
    {
        [SerializeField]
        protected Camera targetCamera;

        /// <summary>
        /// 点位检测频率
        /// </summary>
        [SerializeField]
        protected float inspectionFrequency = 0.1f;

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
        protected List<PointBase> loadPointBases = new();
        protected List<PointBase> loadPointBasesRemove = new();

        /// <summary>
        /// 所有点位控制列表
        /// </summary>
        protected PointBase[] pointBases;
        #endregion


        /// <summary>
        /// 检测到复合的点位控制列表
        /// </summary>
        List<PointBase> detectedPoint = new();



        private IServiceRegistry serviceRegistry;

        public void Initialize(IServiceRegistry serviceRegistry)
        {
            this.serviceRegistry = serviceRegistry;

            pointBases = GetComponentsInChildren<PointBase>();
            foreach (var point in pointBases)
            {
                point.Initialize(serviceRegistry);
                point.targetCamera = targetCamera;
                point.OnEnter += OnEnter;
                point.OnExit += OnExit;
            }
            UnpdataPoint().Forget();
        }



        private void OnEnter(PointBase pointBase)
        {
            if (!detectedPoint.Contains(pointBase))
            {
                detectedPoint.Add(pointBase);
            }

            DetermineExecutionPoint();
        }

        private void OnExit(PointBase pointBase)
        {
 
            if (detectedPoint.Contains(pointBase))
            {
                pointBase.Exit(pointBase);
                detectedPoint.Remove(pointBase);
            }
    
        }

        protected async UniTask UnpdataPoint()
        {

            while (true)
            {
                foreach (var point in pointBases)
                {
                    point.UnpdataPoint();

                    //查看加载模型
                   if(point.distanceToCamera < maxInstantiationDistance)
                    {
                        loadPointBases.Add(point);
                    }

                }

          
                //查看需要实力化点位信息
                foreach (var item in loadPointBases)
                {

                    if (item.distanceToCamera < instantiationDistance)
                    {
                        item.LoadModel();
                    }
                    else
                    {
                        if(item.exitInstantiationTime  == -1)
                        {
                            item.exitInstantiationTime = Time.time;
                        }
                        else if(Time.time - item.exitInstantiationTime > exitInstantiationTime || item.distanceToCamera > maxInstantiationDistance)
                        {
                            item.UnloadModel();
                            loadPointBasesRemove.Add(item);
                        }
                    }
                }

                if(loadPointBasesRemove.Count > 0)
                {
                    foreach (var item in loadPointBasesRemove)
                    {
                        loadPointBases.Remove(item);
                    }

                    loadPointBasesRemove.Clear();
                }
                await UniTask.Delay((int)(inspectionFrequency * 1000));
            }

        
        }


        /// <summary>
        /// 排序点位--碰撞体优先级高于距离优先级
        /// 默认碰撞体点位没有交叉
        /// </summary>
        protected void DetermineExecutionPoint()
        {

            //当前点位
            PointBase pointBase = null;

            //只有一个直接选中
            if (detectedPoint.Count < 2) {

                pointBase = pointBases[0];
            }

            if(pointBase == null)
                 pointBase = detectedPoint.Find(x => x.targetType == TargetType.Collide);
            if (pointBase == null)
            {
                //进行排序
                detectedPoint.Sort((x, y) => x.distanceToCamera.CompareTo(y.distanceToCamera));
                pointBase = detectedPoint[0];
            }

            foreach (var point in detectedPoint)
            {
                point?.Enter(pointBase);
            }
        }


        public void Shutdown()
        {
            foreach (var point in pointBases)
            {
                point.Shutdown();
            }


            pointBases = null;
            targetCamera = null;
        }


    }

}
