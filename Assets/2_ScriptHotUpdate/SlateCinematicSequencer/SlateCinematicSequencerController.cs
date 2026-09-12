using HotUpdate.Point;
using Slate;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// scs控制器
/// </summary>
public class SlateCinematicSequencerController : LoadingModeControllerBase
{
    [SerializeField]
    protected List<GameObject> scsObjs;

    protected LoadingModelManage loadingModelController;

    protected Cutscene cutscene;
    public override void Initialize(LoadingModelManage loadingModelManage)
    {
        base.Initialize(loadingModelManage);
        this.loadingModelController = loadingModelManage;

        cutscene = loadingModelController.cutscene;
        if (cutscene != null)
        {
            foreach (var obj in scsObjs)
            {
                cutscene.SetGroupActorOfName(obj.name, obj);
            }
        }


  
    }

    private void OnEnable()
    {
        cutscene?.PlaySection("Intro");
    }

}
