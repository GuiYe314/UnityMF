using HotUpdate.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchingControl : FunctionControl
{
    [SerializeReference]
    protected List<FunctionBase> functionObjects = new();

    public override void Handle(InteractionData interactionData)
    {
        base.Handle(interactionData);


        foreach (FunctionBase function in functionObjects) {

            function.Enter(interactionData);
        
        }

    }
}
