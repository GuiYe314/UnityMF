using HotUpdate.Point;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FunctionEnum;

namespace HotUpdate.Functions
{
    [Serializable]
    public class GameObjectAutoRot : FunctionBase
    {
        #region 参数
        [Header("旋转设置")]
        [Tooltip("是否执行旋转。关闭后会暂停旋转，但不会影响移动。")]
        [SerializeField]
        public bool rotationEnabled = true;

        [Tooltip("选择需要旋转的轴，可以同时选择 X、Y、Z。")]
        [SerializeField]
        private MotionAxis rotationAxes = MotionAxis.Y;

        [Tooltip("各轴每秒旋转的角度。例如 Y=30 表示每秒绕 Y 轴旋转 30 度。")]
        [SerializeField]
        private Vector3 rotationSpeed = new Vector3(0f, 30f, 0f);

        [Tooltip("Self 表示围绕物体自身轴旋转；World 表示围绕世界坐标轴旋转。")]
        [SerializeField]
        private Space rotationSpace = Space.Self;
        #endregion

   

        public override void Initialize(GameObject obj)
        {
            base.Initialize(obj);
            functionDataBase = Get_FunctionData();
        }



        protected override FunctionDataBase Get_FunctionData()
        {

            GameObjectAutoRotData gameObjectAutoRotData = new GameObjectAutoRotData(rotationEnabled, rotationAxes, rotationSpeed, rotationSpace);
            gameObjectAutoRotData.rotComponent = obj._GetComponent<GameObjectAutoRotComponer>();
            return gameObjectAutoRotData;
        }


        protected class GameObjectAutoRotData : FunctionDataBase
        {
            public bool rotationEnabled;
            public MotionAxis rotationAxes;
            public Vector3 rotationSpeed;
            public Space rotationSpace;

            public GameObjectAutoRotComponer rotComponent;


            public GameObjectAutoRotData(bool rotationEnabled, MotionAxis rotationAxes, Vector3 rotationSpeed, Space rotationSpace)
            {
                this.rotationEnabled = rotationEnabled;
                this.rotationAxes = rotationAxes;
                this.rotationSpeed = rotationSpeed;
                this.rotationSpace = rotationSpace;
            }



            public override void Enter()
            {
                base.Enter();
                rotComponent.Execution(rotationEnabled, rotationAxes, rotationSpeed, rotationSpace);
            }

            public override void Exit()
            {
                base.Exit();
                rotComponent.Execution(!rotationEnabled, rotationAxes, rotationSpeed, rotationSpace);
            }


        }

    }
}



