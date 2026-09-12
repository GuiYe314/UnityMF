using Cysharp.Threading.Tasks;
using DG.Tweening;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class DistanceFollowing : MonoBehaviour
{
    [SerializeField]
    RadialView radialView;
    [SerializeField]
    Transform cameraObj;

    CancellationTokenSource cts = null;

    Vector3 oldPos = Vector3.zero;

    private void Start()
    {
        radialView = radialView == null ? GetComponent<RadialView>(): radialView;
        cameraObj = cameraObj == null ? Camera.main.transform: cameraObj;
        oldPos = transform.position;
        cts = new CancellationTokenSource();
        Run(cts.Token).Forget();
    }


    async UniTask Run(CancellationToken token)
    {

        while (true)
        {

            await UniTask.Delay(300, cancellationToken: token);

            float distance = Vector3.Distance(cameraObj.position, oldPos);


            if (distance < 1.5f)
            {

                radialView.enabled = true;

            }
            else
            {

                transform.DOKill();
                radialView.enabled = false;

                transform.DOMove(oldPos, 0.6f).SetEase(Ease.InOutSine);
            }

        }

    }

    private void OnDestroy()
    {
        cts.Dispose();
    }
}
