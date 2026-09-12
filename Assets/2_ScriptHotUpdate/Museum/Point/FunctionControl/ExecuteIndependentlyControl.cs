using HotUpdate.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 独立执行控制体。只执行一个物体
/// </summary>
public class ExecuteIndependentlyControl : FunctionControl
{
    [SerializeReference]
    protected List<FunctionBase> functionObjects = new();
    protected List<FunctionBase> executionFunctions = new();

    public override void Initialize(InputRouting interaction)
    {
        foreach (var item in functionObjects)
        {
            item.Initialize(gameObject);
        }
    }


    public override void Handle(InteractionData interactionData)
    {

        foreach (var item in functionObjects)
        {
            if (item.interactionSignal != InputRoutingSignal.None &&
                item.interactionSignal != interactionData.interactionSignal)
                continue;

            if (MFEnum.HasFlagFast(interactionData.objectStart, item.objectStart))
            {
                if (interactionData.objectStateNOT)
                {
                    FunctionBase functionBase = executionFunctions.Find(x => x == item);
                    if (functionBase != null)
                    {
                        item.Exit();
                        executionFunctions.Remove(item);


                        //查看还有没有在执行被覆盖的然后执行
                        foreach (var execution in executionFunctions)
                        {
                            execution.Enter();
                        }


                    }

                }
                else
                {
                    FunctionBase functionBase = executionFunctions.Find(x => x == item);
                    if (functionBase == null)
                    {
                        item.Enter();
                        executionFunctions.Add(item);
                    }

                }
            }
        }

    }
}
