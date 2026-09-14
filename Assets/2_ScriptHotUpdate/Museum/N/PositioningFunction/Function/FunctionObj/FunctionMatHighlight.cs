using System.Collections.Generic;
using HotUpdate.Museum.Function;
using HotUpdate.Museum.Input;
using UnityEngine;

/// <summary>
/// 模型材质高亮功能。
///
/// 状态优先级：
/// Select > Hovered > None
///
/// 示例：
/// 1. 移入：添加 Hovered，显示悬停高亮。
/// 2. 点击：添加 Select，显示选中高亮。
/// 3. 移出：移除 Hovered；如果仍有 Select，继续保持选中高亮。
/// 4. 取消选择：移除 Select；没有其他状态时恢复原始材质。
///
/// 注意：
/// ObjState 是 Flags 枚举，一个对象可以同时拥有 Hovered 和 Select。
/// 不应使用大于号比较状态，应使用位运算判断状态。
/// </summary>
[DisallowMultipleComponent]
public sealed class FunctionMatHighlight : FunctionBases
{
    private const string EmissionColorProperty = "_EmissionColor";
    private const string EmissionKeyword = "_EMISSION";

    #region Inspector配置

    [Header("高亮对象")]

    [Tooltip(
        "需要高亮的对象。会自动查找对象及其子对象上的所有Renderer。" +
        "列表为空时，默认从当前GameObject开始查找。")]
    [SerializeField]
    private List<GameObject> objs = new();

    [Header("高亮颜色")]

    [Tooltip("指针移入，但对象没有被选中时使用的高亮颜色。")]
    [ColorUsage(true, true)]
    [SerializeField]
    private Color hoveredEmissionColor =
        new Color(1f, 0.65f, 0f, 1f) * 2f;

    [Tooltip("对象被选中时使用的高亮颜色。Select优先于Hovered。")]
    [ColorUsage(true, true)]
    [SerializeField]
    private Color selectedEmissionColor =
        Color.red * 3f;

    #endregion


    public override void Initialize()
    {
        base.Initialize();




    }

    public override void Open(FunctionDataContext functionBasesData)
    {
        base.Open(functionBasesData);



    }


}