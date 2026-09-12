using System;
using System.Collections.Generic;

namespace AOT.HotUpdate.Experience
{
    /// <summary>
    /// 加载内容节点可以发出的交互事件。
    /// 空间 PointAnchor 的距离/碰撞状态由 PointStreamingManager 处理，不进入此枚举。
    /// </summary>
    public enum InteractionEventType
    {
        // 显式数值保证追加拖拽事件后，旧 ScriptableObject 数据不发生枚举漂移。
        Click = 0,
        Enter = 1,
        Exit = 2,
        BeginDrag = 3,
        Drag = 4,
        EndDrag = 5
    }

    /// <summary>
    /// 功能模块标识。
    /// 工作流只记录模块标识，不直接引用某个 MonoBehaviour，从而让同一份点位数据可以复用。
    /// </summary>
    public enum ExperienceModuleId
    {
        Video = 0,
        Exhibit = 1,
        Model = 2,
        Vfx = 3,
        Animation = 4,
        Ui = 5
    }

    /// <summary>
    /// 基础闭环支持的动作命令。
    /// ModuleId 决定由哪个模块执行，Command 决定模块内部执行哪项操作。
    /// </summary>
    public enum ExperienceCommand
    {
        // 显式固定数值，避免以后追加命令时破坏已经发布的 ScriptableObject 数据。
        VideoPlay = 0,
        VideoStop = 1,
        ExhibitShow = 2,
        ExhibitHideCurrent = 3,
        ExhibitHideTarget = 4,
        ModelSelect = 5,
        ModelShow = 6,
        ModelHide = 7,
        VfxPlay = 8,
        VfxStop = 9,

        /// <summary>
        /// 使用 AssetAddress 异步加载模型 Prefab，并以 TargetId 作为运行时实例 ID。
        /// </summary>
        ModelLoad = 10,

        /// <summary>
        /// 销毁 TargetId 对应的动态实例，并归还它持有的资源引用。
        /// </summary>
        ModelUnload = 11,

        AnimationPlay = 12,
        AnimationStop = 13,
        AnimationPause = 14,
        AnimationResume = 15,
        AnimationSetTrigger = 16,

        UiShow = 17,
        UiHide = 18,
        UiToggle = 19
    }

    /// <summary>
    /// 整个交互系统共享的轻量运行时状态。
    /// 它不负责存档，只用于本次运行期间判断“当前正在展示谁”，
    /// 例如切换按钮会根据 ActiveExhibitId 选择下一条工作流。
    /// </summary>
    public sealed class InteractionState
    {
        public string ActiveNodeId { get; internal set; } = string.Empty;

        public string ActiveExhibitId { get; internal set; } = string.Empty;
        public string SelectedModelId { get; internal set; } = string.Empty;

        /// <summary>
        /// 由展品模块更新当前展品。使用方法而不是公开 setter，既允许热更新
        /// 程序集修改状态，也避免外部代码绕过语义随意赋值。
        /// </summary>
        public void SetActiveExhibit(string exhibitId)
        {
            ActiveExhibitId = exhibitId ?? string.Empty;
        }

        /// <summary>由热更新调度器记录最近触发的内容节点。</summary>
        public void SetActiveNode(string nodeId)
        {
            ActiveNodeId = nodeId ?? string.Empty;
        }

        /// <summary>
        /// 由模型模块记录当前选中的模型。
        /// </summary>
        public void SelectModel(string modelId)
        {
            SelectedModelId = modelId ?? string.Empty;
        }
    }

    /// <summary>
    /// 内容交互节点的运行时只读定义，不代表场景里的空间 PointAnchor。
    /// </summary>
    public sealed class InteractionNodeDefinition
    {
        public string Id { get; }
        public IReadOnlyList<InteractionBindingDefinition> Bindings { get; }

        public InteractionNodeDefinition(
            string id,
            IReadOnlyList<InteractionBindingDefinition> bindings)
        {
            Id = id;
            Bindings = bindings;
        }
    }

    /// <summary>
    /// “某个点位事件 -> 某条工作流”的绑定关系。
    /// RequiredActiveExhibitId 为空时是默认规则；不为空时是带状态条件的规则。
    /// Priority 越高越先匹配。
    /// </summary>
    public sealed class InteractionBindingDefinition
    {
        public InteractionEventType EventType { get; }
        public string WorkflowId { get; }
        public string RequiredActiveExhibitId { get; }
        public int Priority { get; }

        public InteractionBindingDefinition(
            InteractionEventType eventType,
            string workflowId,
            string requiredActiveExhibitId,
            int priority)
        {
            EventType = eventType;
            WorkflowId = workflowId;
            RequiredActiveExhibitId = requiredActiveExhibitId ?? string.Empty;
            Priority = priority;
        }
    }

    /// <summary>
    /// 一条可复用工作流。
    /// Actions 按 StepIndex 分组：同一步并行，不同步依次执行。
    /// </summary>
    public sealed class WorkflowDefinition
    {
        public string Id { get; }

        /// <summary>
        /// 为以后扩展多通道预留，例如 main、ambient、guide。
        /// 当前基础版统一使用 main。
        /// </summary>
        public string Channel { get; }

        public IReadOnlyList<WorkflowActionDefinition> Actions { get; }

        public WorkflowDefinition(string id, string channel, IReadOnlyList<WorkflowActionDefinition> actions)
        {
            Id = id;
            Channel = string.IsNullOrWhiteSpace(channel) ? "main" : channel;
            Actions = actions;
        }
    }

    /// <summary>
    /// 工作流中的一个原子动作。
    /// TargetId 指向场景对象；AssetAddress 指向资源地址；Parameter 为简单扩展参数。
    /// </summary>
    public sealed class WorkflowActionDefinition
    {
        public int StepIndex { get; }
        public ExperienceModuleId ModuleId { get; }
        public ExperienceCommand Command { get; }
        public string TargetId { get; }
        public string AssetAddress { get; }
        public string Parameter { get; }

        public WorkflowActionDefinition(
            int stepIndex,
            ExperienceModuleId moduleId,
            ExperienceCommand command,
            string targetId,
            string assetAddress,
            string parameter)
        {
            StepIndex = stepIndex;
            ModuleId = moduleId;
            Command = command;
            TargetId = targetId ?? string.Empty;
            AssetAddress = assetAddress ?? string.Empty;
            Parameter = parameter ?? string.Empty;
        }
    }
}
