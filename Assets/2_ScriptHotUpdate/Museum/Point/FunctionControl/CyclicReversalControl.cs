using HotUpdate.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 循环反向控制器
/// </summary>
public class CyclicReversalControl : FunctionControl
{
    [SerializeReference]
    protected List<FunctionBase> functionObjects = new();

    [SerializeField]
    public FunctionControl[] controls;

    [SerializeField]
    public bool isReversal = false;

    public override void Handle(InteractionData interactionData)
    {
        base.Handle(interactionData);
        isReversal  = controls[0].gameObject.activeInHierarchy;

        foreach (var item in controls)
        {
            InteractionData interactionDataNew = InteractionData.Acquire(interactionData);

            interactionDataNew.objectStateNOT = isReversal;

            item.Handle(interactionDataNew);

            interactionDataNew.Release();
        }




    }



}
