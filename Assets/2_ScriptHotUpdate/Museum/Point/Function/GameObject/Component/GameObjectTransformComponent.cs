using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameObjectTransformComponent : MonoBehaviour
{
    #region 参数
    /// <summary>
    /// 可以同时选择多个轴。
    /// Flags 可以让 Unity Inspector 将该枚举显示成多选菜单。
    /// </summary>
    [Flags]
    public enum MotionAxis
    {
        None = 0,
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,
        Everything = X | Y | Z
    }


    public bool rotationEnabled = false;


    public MotionAxis rotationAxes = MotionAxis.Y;


    public Vector3 rotationSpeed = new Vector3(0f, 30f, 0f);


    public Space rotationSpace = Space.Self;


    public bool movementEnabled;


    public MotionAxis movementAxes = MotionAxis.None;


    public Vector3 movementSpeed = Vector3.zero;


    public Space movementSpace = Space.Self;

    public bool zoomEnabled;

    public float zoomSize = 1.5f;

    protected Vector3 oldZoom = Vector3.zero;
    #endregion




    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (rotationEnabled)
        {
            Rotate(deltaTime);
        }

        if (movementEnabled)
        {
            Move(deltaTime);
        }
    }


    public void LocalScale()
    {
        transform.DOKill();
        Vector3 vector3 = zoomEnabled ? oldZoom * zoomSize : oldZoom;
        transform.DOScale(vector3, 0.7f);

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
    /// 根据勾选的轴过滤移动速度，然后执行这一帧的移动。
    /// </summary>
    private void Move(float deltaTime)
    {
        Vector3 activeMovementSpeed = FilterByAxes(movementSpeed, movementAxes);

        if (activeMovementSpeed.sqrMagnitude <= 0f)
        {
            return;
        }

        transform.Translate(activeMovementSpeed * deltaTime, movementSpace);
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

    /// <summary>
    /// 供其它脚本在运行时统一开关旋转。
    /// </summary>
    public void SetRotationEnabled(bool enabled)
    {
        rotationEnabled = enabled;
    }

    /// <summary>
    /// 供其它脚本在运行时统一开关移动。
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    /// <summary>
    /// 同时停止旋转和移动。
    /// </summary>
    public void StopAllMotion()
    {
        rotationEnabled = false;
        movementEnabled = false;
    }

}
