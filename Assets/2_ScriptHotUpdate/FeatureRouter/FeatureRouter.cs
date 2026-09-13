using AOT.HotUpdate.Experience;
using AOT.HotUpdate.Framework;
using HotUpdate.Museum.Manager;
using HotUpdate.Point;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace HotUpdate.Experience
{
    /// <summary>
    /// 功能路由
    /// </summary>
    public class FeatureRouter : IFeatureRouter
    {

        private readonly IAppSceneLoader sceneLoader;

        private readonly IAppLogger logger;
        private readonly IServiceRegistry serviceRegistry;

        // 这是 YooAsset 地址，不是 Build Settings 场景名称。
        // 地址来自场景收集器的 MuseumRelativeAddressRule。
        public const string ExperienceSceneAddress = "museum";

        private PointsManager pointsManager = null;

        public FeatureRouter(
       IServiceRegistry serviceRegistry,
       IAppLogger logger)
        {
            this.serviceRegistry = serviceRegistry
                ?? throw new ArgumentNullException(nameof(serviceRegistry));
            this.sceneLoader = serviceRegistry.Resolve<IAppSceneLoader>()
                ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task EnterFeatureAsync(CancellationToken cancellationToken)
        {
            await sceneLoader.LoadSceneAsync(
            ExperienceSceneAddress,
            AppSceneLoadMode.Single,
            cancellationToken);

            // 进入功能后，业务层可以通过服务注册表获取 IAppSceneLoader、IAppLogger 等服务。
            try
            {
                //获取点位控制器。所有点位控制
                pointsManager = GameObject.FindObjectOfType<PointsManager>();
                pointsManager.Initialize(serviceRegistry);
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to initialize MuseumPointsController: {e.Message}");
                throw;
            }
      
    

        }

        public async Task ExitCurrentFeatureAsync(CancellationToken cancellationToken)
        {

            pointsManager.Shutdown();

            await sceneLoader.UnloadSceneAsync(
                ExperienceSceneAddress,
                cancellationToken);
        }
    }

}

