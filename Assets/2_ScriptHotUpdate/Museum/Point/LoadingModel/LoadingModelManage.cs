using HotUpdate.Point;
using Slate;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 加载模型管理器
/// </summary>
public class LoadingModelManage : MonoBehaviour
{

    public Cutscene cutscene { get;protected set; }

    [SerializeField]
    protected List<LoadingModeControllerBase> loadingModeControllerBases;

    public void Initialize(Cutscene cutscene)
    {
        this.cutscene = cutscene;

        foreach (var item in loadingModeControllerBases)
        {
            item.Initialize(this);
        }
    }
}
