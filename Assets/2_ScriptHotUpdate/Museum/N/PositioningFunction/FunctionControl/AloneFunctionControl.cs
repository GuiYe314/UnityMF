using HotUpdate.Museum.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Museum.Function
{

    /// <summary>
    /// 独立功能控制
    /// </summary>
    public class AloneFunctionControl : FunctionControlBase
    {


        Dictionary<string, FunctionBases> functionBasesDic = null;

        public override void Initialize()
        {
            base.Initialize();
            functionBasesDic = new();
            //获取所有功能脚本
            FunctionBases[] functionBases = GetComponents<FunctionBases>();

            foreach (var item in functionBases)
            {
                item.Initialize();
                string str = item.GetType().Name;
                functionBasesDic.Add(str, item);

 
            }
        }



        public override void Open(FunctionDataContext functionBasesData)
        {
            base.Open(functionBasesData);

            //根据类型传递脚本
            FunctionBases functionBases = null;

            string eventName = functionBasesData.eventName.ToString().Split('_')[0];

            Debug.Log(name + ":调用：" + eventName);
            if (!functionBasesDic.TryGetValue(eventName,out functionBases))
            {
                Debug.LogError("未找到功能脚本：" + eventName);
                return;
            }
            functionBases.Open(functionBasesData);

        }









    }

}
