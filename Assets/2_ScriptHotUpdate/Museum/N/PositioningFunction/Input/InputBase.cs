using HotUpdate.Museum.Function;
using HotUpdate.Point;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Museum.Input
{


    public class InputBase : MonoBehaviour
    {

        [SerializeField]
        public List<InputData> inputDatas = new();

        public Action<ImportRoutingSignal, GameObject> OnTrigger;


        private void Start()
        {
            foreach (var item in inputDatas)
            {
                OnTrigger += item.OnImport;
            }
        }



        private void OnDestroy()
        {
            OnTrigger = null;

            
        }
    }



    /// <summary>
    /// 输入数据
    /// </summary>
    [Serializable]
    public class InputData
    {

        [SerializeField]
        protected string name;


        [SerializeField]
        protected ImportRoutingSignal importRoutingSignal;
        [SerializeField]
        protected ObjState objState;

        [SerializeField]
        protected bool objStateReversal;

        [SerializeField]
        protected List<InputEvent> inputEvent;

        public void OnImport(ImportRoutingSignal importRoutingSignal, GameObject obj)
        {
            //判断输入类型
            if (this.importRoutingSignal != importRoutingSignal) return;

            foreach (var item in inputEvent)
            {
                item.OnImport(importRoutingSignal, obj, objStateReversal, objState);
            }

        }

    }

    [Serializable]
    public class InputEvent
    {
        [SerializeField]
        protected FunctionEventName eventName;

        [SerializeField]
        protected List<FunctionControlBase> functionObj = new();


        public void OnImport(ImportRoutingSignal importRoutingSignal, GameObject obj, bool objStateReversal, ObjState objState)
        {
            FunctionDataContext functionBasesData = FunctionDataContext.Acquire(obj, objStateReversal, importRoutingSignal, objState, eventName);

            foreach (var item in functionObj)
            {
                item.Open(functionBasesData);
            }
            functionBasesData.Release();

        }
    }




    #region 枚举
    /// <summary>
    /// 输入路由信号。
    ///
    /// MRTK、UGUI、碰撞器等外部输入都应该先转换成这里的信号，
    /// Rule 只关心信号，不需要了解信号来自哪一种输入系统。
    /// </summary>
    public enum ImportRoutingSignal
    {

        None,
        /// <summary>
        /// 移入
        /// </summary>
        Enter,
        /// <summary>
        /// 移出
        /// </summary>
        Exit,
        /// <summary>
        /// 点击
        /// </summary>
        Click,
        /// <summary>
        /// 按下
        /// </summary>
        Down,
        /// <summary>
        /// 拖拽
        /// </summary>
        Dragged,
        /// <summary>
        /// 弹起
        /// </summary>
        Up
    }


    /// <summary>
    /// 物体状态
    /// </summary>
    public enum ObjState
    {
        None = 0,       // 没有任何状态
        Hovered,
        Select
    }



    /// <summary>
    /// 事件名
    /// </summary>
    public enum FunctionEventName
    {
        None,
        [InspectorName("物体状态/自动旋转打开")]
        FunctionObjAutoRot_AutoRotOpen,
        [InspectorName("物体状态/自动旋转关闭")]
        FunctionObjAutoRot_AutoRotClose,

        [InspectorName("物体状态/高亮")]
        FunctionMatHighlight_HighlightOpen,
        [InspectorName("物体状态/关闭高亮")]
        FunctionMatHighlight_HighlightClose,

        [InspectorName("物体状态/缩放")]
        GameObjectDotweenScale_Scale,
        [InspectorName("物体状态/缩放回归")]
        GameObjectDotweenScale_ReturnScale,

    }
    #endregion


}
