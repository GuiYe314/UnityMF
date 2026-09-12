using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace HotUpdate.Functions
{

    [Serializable]
    public class GameObjectDotweenScale : FunctionBase
    {



        [Header("缩放")]
        [Tooltip("是否执行移动。关闭后会暂停移动，但不会影响旋转。")]
        [SerializeField]
        public bool zoomEnabled;

        [Tooltip("选择需要移动的轴，可以同时选择 X、Y、Z。")]
        [SerializeField]
        private float zoomSize = 1.5f;





        public override void Initialize(GameObject obj)
        {
            base.Initialize(obj);
            functionDataBase = Get_FunctionData();
        }



        protected override FunctionDataBase Get_FunctionData()
        {

            GameObjectDotweenScaleData gameObjectDotweenScaleData = new GameObjectDotweenScaleData(zoomEnabled, zoomSize);
            gameObjectDotweenScaleData.gameObjectComponent = obj._GetComponent<GameObjectDotweenScaleDataComponent>();
            return gameObjectDotweenScaleData;
        }


        protected class GameObjectDotweenScaleData : FunctionDataBase
        {
            public bool zoomEnabled;
            public float zoomSize;


            public GameObjectDotweenScaleDataComponent gameObjectComponent;


            public GameObjectDotweenScaleData(bool zoomEnabled, float zoomSize)
            {
                this.zoomEnabled = zoomEnabled;
                this.zoomSize = zoomSize;
            }

            public override void Enter()
            {
                base.Enter();
                gameObjectComponent.Execution(zoomEnabled, zoomSize);
            }

            public override void Exit()
            {
                base.Exit();
                gameObjectComponent.Execution(!zoomEnabled, zoomSize);
            }


        }
    }

}
