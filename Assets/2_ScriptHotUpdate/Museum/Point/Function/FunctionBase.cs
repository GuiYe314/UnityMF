using HotUpdate.Point;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class FunctionBase 
{
    [SerializeField]
    protected string name;

    protected InputRouting interaction;

    [SerializeField]
    public InputRoutingSignal interactionSignal;

    [SerializeField]
    public ObjectState objectStart;


    protected FunctionDataBase functionDataBase;

    protected GameObject obj;

    public virtual void Initialize(GameObject obj)
    {
        this.obj = obj;

    }

    protected virtual FunctionDataBase Get_FunctionData()
    {
        return default;
    }


    public virtual void Enter(InteractionData interactionData = null)
    {
        functionDataBase.Enter();
    }

    public virtual void Exit(InteractionData interactionData = null)
    {
        functionDataBase.Exit();    
    }


    public virtual void Execution(InteractionData interactionData)
    {
        
    }


}
