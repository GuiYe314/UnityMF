using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MuseumOpenAnimation : MonoBehaviour
{



    [SerializeField]
    protected List<GameObject> museumModels;


    public UnityEvent doTweenCallback;

    private void OnEnable()
    {
        OpenMode();
    }

    protected void OpenMode()
    {
        for (int i = 0; i < museumModels.Count; i++)
        {
            museumModels[i].transform.localPosition = Vector3.zero;

            Vector3 pos = GetPosition(0.3f, museumModels.Count, i);
            museumModels[i].transform.DOLocalMove(pos, 0.9f).OnComplete(() => {

                doTweenCallback?.Invoke();
            });
        }
    }

    /// <summary>
    /// 获取直线排列位置
    /// </summary>
    /// <param name="height">高度</param>
    /// <param name="count">物体数量</param>
    /// <param name="index">当前物体第几个(从0开始)</param>
    /// <param name="spacing">间距</param>
    public static Vector3 GetPosition(
        float height,
        int count,
        int index,
        float spacing = 0.5f)
    {

        if (count <= 0)
        {
            return Vector3.zero;
        }


        if (index < 0 || index >= count)
        {
            return Vector3.zero;
        }



        //总长度
        float totalLength =
            (count - 1) * spacing;



        //当前X位置
        float x =
            index * spacing
            - totalLength / 2f;



        return new Vector3(
            x,
            height,
            0
        );

    }
}
