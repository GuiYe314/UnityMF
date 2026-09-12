using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectDotweenScaleDataComponent : MonoBehaviour
{
    public float zoomSize = 1.5f;

    protected Vector3 oldZoom = Vector3.zero;

    protected bool zoomEnabled;


    private void Awake()
    {
        oldZoom = transform.localScale;
    }


    public void Execution(bool zoomEnabled, float zoomSize)
    {
        this.zoomEnabled = zoomEnabled;
        this.zoomSize = zoomSize;

        LocalScale();
    }

    public void LocalScale()
    {
        transform.DOKill();
        Vector3 vector3 = zoomEnabled ? oldZoom * zoomSize : oldZoom;
        transform.DOScale(vector3, 0.7f);

    }
}
