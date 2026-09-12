using System;
using UnityEngine;

namespace AOT.HotUpdate.Framework.Serialization
{
    /// <summary>
    /// 为使用 SerializeReference 序列化的接口或基类字段提供实现类型选择菜单。
    /// 同时支持单个托管引用和 List/数组形式的托管引用集合，并按完整类继承链组织选择菜单。
    /// </summary>
    /// <example>
    /// [SerializeReference, ImplementationSelector]
    /// private List&lt;IMyAction&gt; actions;
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ImplementationSelectorAttribute : PropertyAttribute
    {
    }
}
