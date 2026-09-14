
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FunctionEnum;


namespace HotUpdate.Museum.Function
{

    [DisallowMultipleComponent]
    public class FunctionObjAutoRot : FunctionBases
    {

        [SerializeField]
        protected bool rotationEnabled = false;

        [SerializeField]
        protected MotionAxis rotationAxes = MotionAxis.Y;

        [SerializeField]
        protected Vector3 rotationSpeed = new Vector3(0f, 30f, 0f);

        [SerializeField]
        protected Space rotationSpace = Space.Self;


        public override void Open(FunctionDataContext functionBasesData)
        {
            base.Open(functionBasesData);

            switch (functionBasesData.eventName)
            {
                case Input.FunctionEventName.FunctionObjAutoRot_AutoRotOpen:
                    FunctionObjAutoRotOpen();
                    break;
                case Input.FunctionEventName.FunctionObjAutoRot_AutoRotClose:
                    FunctionObjAutoRotClose();
                    break;
                default:
                    break;
            }


        }

        protected void FunctionObjAutoRotOpen()
        {
            rotationEnabled = true;
        }
        protected void FunctionObjAutoRotClose()
        {
            rotationEnabled = false;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (rotationEnabled)
            {
                Rotate(deltaTime);
            }
        }



        private void Rotate(float deltaTime)
        {
            Vector3 activeRotationSpeed = FilterByAxes(rotationSpeed, rotationAxes);

            if (activeRotationSpeed.sqrMagnitude <= 0f)
            {
                return;
            }

            transform.Rotate(activeRotationSpeed * deltaTime, rotationSpace);
        }




        private static Vector3 FilterByAxes(Vector3 speed, MotionAxis axes)
        {
            return new Vector3(
                HasAxis(axes, MotionAxis.X) ? speed.x : 0f,
                HasAxis(axes, MotionAxis.Y) ? speed.y : 0f,
                HasAxis(axes, MotionAxis.Z) ? speed.z : 0f);
        }

        private static bool HasAxis(MotionAxis selectedAxes, MotionAxis axis)
        {
            return (selectedAxes & axis) != 0;
        }


    }

}
