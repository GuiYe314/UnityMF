using HotUpdate.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayVideoControl : FunctionControl
{
    [SerializeReference]
    protected List<FunctionBase> functionObjects = new();



    public override void Handle(InteractionData interactionData)
    {
        base.Handle(interactionData);



        foreach (var item in functionObjects)
        {
            if (item.interactionSignal != InputRoutingSignal.None &&
                item.interactionSignal != interactionData.interactionSignal
                )
                continue;

            if (MFEnum.HasFlagFast(interactionData.objectStart, item.objectStart))
            {
                if (!interactionData.objectStateNOT)
                {
                    item.Enter(interactionData);

                }
                else
                {
                    item.Exit(interactionData);
                }
            }
        }


    }

}
