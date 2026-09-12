using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FunctionEnum;

public class GameObjectAutoRotComponer : MonoBehaviour
{

    [SerializeField]
    protected bool rotationEnabled = false;

    [SerializeField]
    protected MotionAxis rotationAxes = MotionAxis.Y;

    [SerializeField]
    protected Vector3 rotationSpeed = new Vector3(0f, 30f, 0f);

    [SerializeField]
    protected Space rotationSpace = Space.Self;


    public void Open()
    {
        rotationEnabled = true;
    }

    private void OnEnable()
    {
        rotationEnabled = false;
    }

    public void Execution(bool rotationEnabled, MotionAxis rotationAxes, Vector3 rotationSpeed, Space rotationSpace)
    {
        this.rotationEnabled = rotationEnabled;
        this.rotationAxes = rotationAxes;
        this.rotationSpeed = rotationSpeed;
        this.rotationSpace = rotationSpace;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (rotationEnabled)
        {
            Rotate(deltaTime);
        }
    }


    /// <summary>
    /// 根据勾选的轴过滤旋转速度，然后执行这一帧的旋转。
    /// 使用 deltaTime 后，运动速度不会随帧率变化。
    /// </summary>
    private void Rotate(float deltaTime)
    {
        Vector3 activeRotationSpeed = FilterByAxes(rotationSpeed, rotationAxes);

        if (activeRotationSpeed.sqrMagnitude <= 0f)
        {
            return;
        }

        transform.Rotate(activeRotationSpeed * deltaTime, rotationSpace);
    }



    /// <summary>
    /// 未选择的轴速度强制变为 0。
    /// 例如只选择 Y，那么即使 X、Z 填了数值也不会生效。
    /// </summary>
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
