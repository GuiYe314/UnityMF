using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HotUpdate.Museum.Function
{
    public class SwitchFunctionControl : FunctionControlBase
    {
        [SerializeField]
        protected GameObject[] gameObjects;


        protected Dictionary<GameObject, SwitchFunctionControlData> switchDataDic = new();

        public override void Initialize()
        {
            base.Initialize();

            foreach (var item in gameObjects)
            {

                if (switchDataDic.ContainsKey(item)) continue;
                SwitchFunctionControlData switchFunctionControlData = new();

                switchFunctionControlData.Initialize(item);

                switchDataDic.Add(item, switchFunctionControlData);

            }
            

        }


        public override void Open(FunctionDataContext functionBasesData)
        {
            base.Open(functionBasesData);

            foreach (var switchData in switchDataDic)
            {

                //根据类型传递脚本
                FunctionBases functionBases = null;

                string eventName = functionBasesData.eventName.ToString().Split('_')[0];
                Debug.Log(name + ":调用：" + eventName);
                if (!switchData.Value.functionBasesDic.TryGetValue(eventName, out functionBases))
                {
                    Debug.LogError("未找到功能脚本：" + eventName);
                    return;
                }

                FunctionDataContext nweFunctionBasesData = FunctionDataContext.Acquire(functionBasesData);
                if (switchData.Key != functionBasesData.obj)
                {
                    nweFunctionBasesData.objStateReversal = true;
                }
                functionBases.Open(nweFunctionBasesData);

                nweFunctionBasesData.Release();
            }
        }



        protected class SwitchFunctionControlData
        {
            public Dictionary<string, FunctionBases> functionBasesDic = null;


            public void Initialize(GameObject obj)
            {
                functionBasesDic = new();
                //获取所有功能脚本
                FunctionBases[] functionBases = obj.GetComponents<FunctionBases>();

                foreach (var item in functionBases)
                {
                    item.Initialize();
                    string str = item.GetType().Name;
                    functionBasesDic.Add(str, item);

                }
            }

        }


    }

}

