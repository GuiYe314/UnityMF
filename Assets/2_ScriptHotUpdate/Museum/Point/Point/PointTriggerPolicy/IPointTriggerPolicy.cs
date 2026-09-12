using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Point
{

    /// <summary>
    /// 点位触发策略
    /// </summary>
    public interface IPointTriggerPolicy
    {


        /// <summary>
        /// 确定执行点
        /// </summary>
        public void DetermineExecutionPoint();

    }

}

