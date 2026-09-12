using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionEnum 
{
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

}
