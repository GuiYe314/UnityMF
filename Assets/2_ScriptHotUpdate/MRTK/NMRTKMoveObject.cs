using Cysharp.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NMRTKMoveObject : MonoBehaviour
{

    /// <summary>
    /// 物体操作类型
    /// </summary>
    [SerializeField]
    private ObjectType objectOperateType;

    public ObjectType ObjectOperateType { get => objectOperateType; }

    /// <summary>
    /// 物体模式是否可进入编辑模式
    /// </summary>
    [SerializeField]
    private ObjectModel objectModel;

    public ObjectModel ObjectModel { get => objectModel; }

    /// <summary>
    /// 移动操作
    /// </summary>
    protected ObjectManipulator manipulator;


    private void Awake()
    {
        manipulator = gameObject.AddComponent<ObjectManipulator>();

        if (ObjectModel == ObjectModel.Editor)
            Run(false).Forget();

    }

    private void Start()
    {
        Debug.Log(
            typeof(ObjectManipulator)
                .Assembly
                .GetName()
                .Name);
    }


    /// <summary>
    /// 初始化延迟关闭编辑模式拖动按钮
    /// </summary>
    /// <param name="isEditor"></param>
    /// <returns></returns>
    async UniTask Run(bool isEditor)
    {

        await UniTask.Delay(1000);
        if (ObjectModel != ObjectModel.Editor) return;
        manipulator.enabled = isEditor;

    }

}


public enum ObjectType
{

    None,
    Move,
    Transform
}

/// <summary>
/// 物体模式
/// </summary>
public enum ObjectModel
{
    None,
    Editor
}