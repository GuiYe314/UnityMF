using Microsoft.MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace HotUpdate.MRTK
{

    public class MRTKCamerController : MonoBehaviour
    {
        [SerializeField]
        protected Vector3 _Position = Vector3.zero;
        [SerializeField]
        protected Vector3 _Rotation = Vector3.zero;


        private void Awake()
        {
            Transform playspace = MixedRealityPlayspace.Transform;

            playspace.position = _Position;

            playspace.eulerAngles = _Rotation;
        }


    }

}
