using HotUpdate.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionDataBase
{
    public InputRoutingSignal interactionSignal;
    public ObjectState objectStart;


    public virtual void Enter()
    {

    }

    public virtual void Exit() { }
}
