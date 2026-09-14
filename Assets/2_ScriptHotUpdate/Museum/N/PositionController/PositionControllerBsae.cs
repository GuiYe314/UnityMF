using AOT.HotUpdate.Experience;
using AOT.HotUpdate.Framework;
using HotUpdate.Museum.Function;
using Slate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HotUpdate.Museum.Controller
{
    /// <summary>
    /// 点位控制器基类
    /// </summary>
    public class PositionControllerBsae : MonoBehaviour
    {

        /// <summary>
        /// 服务注册
        /// </summary>
        private IServiceRegistry serviceRegistry;


        /// <summary>
        /// 进入
        /// </summary>
        public Action<PositionControllerBsae> OnEnter { get; set; }

        /// <summary>
        /// 移出
        /// </summary>
        public Action<PositionControllerBsae> OnExit { get; set; }

        /// <summary>
        /// 资源路径
        /// </summary>
        [SerializeField]
        public string assetPath;
        protected GameObject pointObj = null;

        /// <summary>
        /// 当前点位距离摄像机的距离,用于点位资源缓存
        /// </summary>
        public float distanceToCamera { get; set; }

        #region 点位资源
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
        #endregion

        #region 点位信息

        public pointType pointType { get; protected set; }
        #endregion

        protected IContentAssetProvider contentAssetProvider = null;
        protected CancellationToken cancellationToken = new();


        protected FunctionControlBase[] functionControlBases;

        public virtual void Initialize(IServiceRegistry serviceRegistry)
        {
            distanceToCamera = 999999;
            this.serviceRegistry = serviceRegistry;
            this.contentAssetProvider = serviceRegistry.Resolve<IContentAssetProvider>();


        }

        public virtual void UpdatePoint()
        {

        }


        /// <summary>
        /// 点位开启
        /// </summary>
        /// <param name="position"></param>
        public  virtual void Open()
        {

            pointObj.SetActive(true);
        }





        /// <summary>
        /// 点位关闭
        /// </summary>
        /// <param name="position"></param>
        public virtual void Close()
        {

            pointObj.SetActive(false);

        }

        /// <summary>
        /// 点位加载
        /// </summary>
        /// <param name="level"></param>
        public virtual void Load()
        {
            try
            {
                if (pointObj == null)
                    PreloadAsync();
            }
            catch (Exception e)
            {
                Debug.LogError("加载模型错误：" + e.Message);
                throw;
            }
      

        }
        public async Task PreloadAsync()
        {
            GameObject obj = await contentAssetProvider.LoadAsync<GameObject>(assetPath, cancellationToken);
            pointObj = obj._Instantiate(_pointExecution.transform, false);
            pointObj.SetActive(false);

            _pointCutscene.SetGroupActorOfName(pointObj.name, pointObj);

            _pointCutscene?.PlaySection("Intro");

            functionControlBases = pointObj.GetComponentsInChildren<FunctionControlBase>(true);

            foreach (var item in functionControlBases)
            {
                item.Initialize();
            }

        }


        /// <summary>
        /// 点位销毁
        /// </summary>

        public virtual void Destroy()
        {
            if (pointObj != null)
            {

                foreach (var item in functionControlBases)
                {
                    item.Clos();
                }

                GameObject.Destroy(pointObj);
                pointObj = null;
            }
        }
    }

    public enum pointType
    {
        Distance = 0,
        Collide = 1
    }
}