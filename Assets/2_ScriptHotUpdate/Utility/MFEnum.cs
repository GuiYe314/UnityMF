using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MFEnum
{
    /// <summary>
    /// 对指定的 Flag 进行独立取反（Toggle）
    /// </summary>
    public static T ToggleFlag<T>(ref this T flags, T flagToToggle) where T : struct, Enum
    {
        ulong current = Convert.ToUInt64(flags);
        ulong target = Convert.ToUInt64(flagToToggle);

        ulong result = current ^ target;
        flags = (T)Enum.ToObject(typeof(T), result);
        return flags;
    }

    /// <summary>
    /// 对指定的 Flag 进行独立取反（Toggle）
    /// </summary>
    public static T ToggleFlag<T>(T flags, T flagToToggle) where T : struct, Enum
    {
        ulong current = Convert.ToUInt64(flags);
        ulong target = Convert.ToUInt64(flagToToggle);

        ulong result = current ^ target;
        flags = (T)Enum.ToObject(typeof(T), result);
        return flags;
    }

    /// <summary>
    /// 强制添加某个 Flag
    /// </summary>
    public static T AddFlag<T>(ref this T flags, T flagToAdd) where T : struct, Enum
    {
        ulong current = Convert.ToUInt64(flags);
        ulong target = Convert.ToUInt64(flagToAdd);

        ulong result = current | target;
        flags = (T)Enum.ToObject(typeof(T), result);
        return flags;
    }

    /// <summary>
    /// 强制移除某个 Flag
    /// </summary>
    public static T RemoveFlag<T>(ref this T flags, T flagToRemove) where T : struct, Enum
    {
        ulong current = Convert.ToUInt64(flags);
        ulong target = Convert.ToUInt64(flagToRemove);

        ulong result = current & ~target;
        flags = (T)Enum.ToObject(typeof(T), result);
        return flags;
    }

    /// <summary>
    /// 根据 bool 值设置指定 Flag 状态
    /// </summary>
    public static T SetFlag<T>(ref this T flags, T flagToSet, bool enabled) where T : struct, Enum
    {
        return enabled ? flags.AddFlag(flagToSet) : flags.RemoveFlag(flagToSet);
    }


    /// <summary>
    /// 判断是否包含指定的 Flag（无装箱开销）
    /// </summary>
    public static bool HasFlagFast<T>(this T flags, T flagToCheck) where T : struct, Enum
    {
        ulong current = Convert.ToUInt64(flags);
        ulong target = Convert.ToUInt64(flagToCheck);

        return (current & target) == target;
    }

    /// <summary>
    /// 判断是否包含任意一个指定的 Flag（满足其一即为 true）
    /// </summary>
    public static bool HasAnyFlag<T>(this T flags, T flagsToCheck) where T : struct, Enum
    {
        ulong current = Convert.ToUInt64(flags);
        ulong target = Convert.ToUInt64(flagsToCheck);

        return (current & target) != 0;
    }

    /// <summary>
    /// 判断是否【仅】包含某个单独的 Flag（没有任何其他比特位）
    /// </summary>
    public static bool IsExact<T>(this T flags, T exactFlag) where T : struct, Enum
    {
        ulong current = Convert.ToUInt64(flags);
        ulong target = Convert.ToUInt64(exactFlag);

        return current == target;
    }

    /// <summary>
    /// 判断是否为空（未设定任何 Flag / 值为 0）
    /// </summary>
    public static bool IsNone<T>(this T flags) where T : struct, Enum
    {
        return Convert.ToUInt64(flags) == 0;
    }
}
