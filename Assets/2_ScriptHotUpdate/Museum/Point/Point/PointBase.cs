using AOT.HotUpdate.Experience;
using AOT.HotUpdate.Framework;
using Slate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace HotUpdate.Point
{

    public class PointBase : MonoBehaviour
    {
        protected bool IsInitiated = false;

        /// <summary>
        /// 点位模型父类
        /// </summary>
        [SerializeField]
        protected GameObject _pointModel;

        /// <summary>
        /// 点位执行模型父类
        /// </summary>
        [SerializeField]
        protected GameObject _pointExecution;


        /// <summary>
        /// 点位剧情
        /// </summary>
        [SerializeField]
        protected Cutscene _pointCutscene;


        /// <summary>
        /// 模型路径
        /// </summary>
        [SerializeField]
        protected string ModelPath;


        public TargetType targetType { get;protected set; }

        /// <summary>
        /// 当前点位距离摄像机的距离,用于点位资源缓存
        /// </summary>
        public float distanceToCamera { get; set; }

        [HideInInspector]
        public float exitInstantiationTime = 5f;
        protected GameObject pointObj = null;

        public Camera targetCamera { get; set; }

        public Action<PointBase> OnEnter { get; set; }
        public Action<PointBase> OnExit { get; set; }
        protected IServiceRegistry serviceRegistry = null;

        protected IContentAssetProvider contentAssetProvider = null;

        protected PointBase currentPoint = null;

        protected CancellationToken cancellationToken = new();



        public virtual void Initialize(IServiceRegistry serviceRegistry)
        {
            IsInitiated = true;
            this.serviceRegistry = serviceRegistry;
            this.contentAssetProvider = serviceRegistry.Resolve<IContentAssetProvider>();

            _pointModel = _pointModel ?? transform.Find("PointModel")?.gameObject;
            _pointExecution = _pointExecution ?? transform.Find("PointExecution")?.gameObject;
            _pointCutscene = _pointCutscene ?? transform.Find("PointExecution")?.GetComponentInChildren<Cutscene>();
        }


        public virtual void Enter(PointBase pointBase)
        {
            currentPoint = pointBase;
            pointObj.SetActive(currentPoint == this);
        }

        public virtual void Exit(PointBase pointBase)
        {
            
        }


        /// <summary>
        /// 加载模型
        /// </summary>
        public virtual void LoadModel()
        {

            if(pointObj != null)
            {
                exitInstantiationTime = -1;
                return;
            }

            PreloadAsync();
        }

        public async Task PreloadAsync()
        {
            GameObject obj = await contentAssetProvider.LoadAsync<GameObject>(ModelPath, cancellationToken);
            pointObj = obj._Instantiate( _pointExecution.transform, false);
            pointObj.SetActive(false);

            //查找管理器
            LoadingModelManage loadingModelController = pointObj._GetComponent<LoadingModelManage>();
            loadingModelController.Initialize(_pointCutscene);

        }


        /// <summary>
        /// 卸载模型
        /// </summary>
        public virtual void UnloadModel()
        {
            if(pointObj != null)
            {
                exitInstantiationTime = -1;
                GameObject.Destroy(pointObj);
                pointObj = null;
            }

        }


        public virtual void UnpdataPoint()
        {

            //检测点位控制器与相机的距离
            distanceToCamera = Vector3.Distance(targetCamera.transform.position, transform.position);
        }

        public virtual void Shutdown()
        {

        }


    }


    public enum TargetType
    {
        Distance = 0,
        Collide = 1
    }

}

