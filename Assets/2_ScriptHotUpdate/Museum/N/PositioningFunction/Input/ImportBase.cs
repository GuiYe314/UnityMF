using HotUpdate.Museum.Function;
using HotUpdate.Point;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Museum.Input
{


    public class ImportBase : MonoBehaviour
    {


        public Action<ImportRoutingSignal, GameObject> OnTrigger;


        private void OnDestroy()
        {
            OnTrigger = null;
        }
    }



    /// <summary>
    /// 输入数据
    /// </summary>
    [Serializable]
    public class ImportData
    {

        [SerializeField]
        ImportRoutingSignal importRoutingSignal;
        [SerializeField]
        protected ObjState objState;
        [SerializeField]
        protected bool objStateReversal;

        [SerializeField]
        protected FunctionBases functionBase;

        public void OnImport(ImportRoutingSignal importRoutingSignal, GameObject obj)
        {
            FunctionBasesData functionBasesData = FunctionBasesData.Acquire(obj, objStateReversal, importRoutingSignal,objState);

            if (this.importRoutingSignal != importRoutingSignal)
                return;
         
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
    [Flags]
    public enum ObjState
    {
        None = 0,       // 没有任何状态
        Temporary = 1 << 0,
        Select = 1 << 1
    }



    /// <summary>
    /// 事件名
    /// </summary>
    public enum EventName
    {
        None,
        FunctionObjAutoRotOpen,
        FunctionObjAutoRotClose,

    }
    #endregion


}
