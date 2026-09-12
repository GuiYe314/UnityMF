using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Point
{
    /// <summary>
    /// 输入路由信号。
    ///
    /// MRTK、UGUI、碰撞器等外部输入都应该先转换成这里的信号，
    /// Rule 只关心信号，不需要了解信号来自哪一种输入系统。
    /// </summary>
    public enum InputRoutingSignal
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

    [Flags]
    public enum ObjectState
    {

        None = 1 << 0,
        /// <summary>
        /// 临时状态
        /// </summary>
        Temporary = 1 << 1,

        /// <summary>
        /// 选择状态
        /// </summary>
        Select = 1 << 2,

        /// <summary>
        /// 播放状态
        /// </summary>
        PlayVideo = 1 << 3,

    }

    /// <summary>
    /// 事件名字
    /// </summary>
    public enum FunctionName
    {
        /// <summary>
        /// 物体激活
        /// </summary>
        GameObjectActive
    }

    /// <summary>
    /// 事件路由
    /// </summary>
    public enum FunctionRoute
    {
        oneToMany,
        oneToOne,
        manyToOne,
        manyToMany,

    }



}