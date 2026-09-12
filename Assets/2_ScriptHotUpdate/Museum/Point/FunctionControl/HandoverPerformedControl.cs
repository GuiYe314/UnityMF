using HotUpdate.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 多任务只选一个执行任务状态，根据对象名执行
/// </summary>
public class HandoverPerformedControl : FunctionControl
{
    [SerializeReference]
    protected List<FunctionBase> functionObjects = new();

    [SerializeField]
    public FunctionControl[] controls;


    public override void Handle(InteractionData interactionData)
    {
        base.Handle(interactionData);


        foreach (var item in controls)
        {
            InteractionData interactionDataNew = InteractionData.Acquire(interactionData);
            if (item.gameObject == interactionData.targetObj)
            {
          
                interactionDataNew.objectStateNOT = false;
            }
    
            else
            {
                interactionDataNew.objectStateNOT = true;
            }
             

            item.Handle(interactionDataNew);

            interactionDataNew.Release();
        }



    }
    
}
