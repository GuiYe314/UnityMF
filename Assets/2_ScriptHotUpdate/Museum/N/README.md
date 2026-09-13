# Museum/N 强类型消息点位框架

本目录是旧 Museum/Point 的并行替代实现。当前点位激活只实现距离触发；点位内部的 MRTK 输入统一转换成强类型消息，输入脚本不直接引用视频、文物、高亮或选择模块。

## 核心调用链

~~~text
MRTK / UI / 后续手势和碰撞输入
  -> PointMessageInputBehaviour
  -> 点位私有 PointMessageBus
  -> Selection / Presentation 等业务模块
  -> VideoMessage / ModelVisualMessage / ArtifactMessage
  -> 对应基础能力模块
~~~

每个点位拥有独立消息总线，不使用全局静态事件，避免相邻点位串消息。消息负责业务通信，EnterPoint / ExitPoint / Shutdown 仍负责必须执行的生命周期清理。

## 最少挂载方式

场景公共根节点：

~~~text
MuseumPointSystem
  - MuseumPointCoordinator
      observer：主相机（可留空自动找 MainCamera）
      pointSearchRoot：所有新点位的公共父节点
~~~

一个使用“选择后播放视频，可切换文物”模式的点位：

~~~text
Point01
  - MuseumPoint
  - DistancePointTrigger
  Runtime
    - PointRuntimeController
    - MuseumPointSelectionModule
    - MuseumVideoArtifactPresentationModule
    - PointVideoModule
    - MuseumArtifactGroupModule
    SmallModel01
      - Collider
      - MuseumInteractionTarget
      - MuseumOptionView
      - MuseumOptionMrtkInput
    ToggleButton
      - Button
      - MuseumDisplayToggleInput
~~~

PointRuntimeController 自动收集子节点中的 PointModuleBehaviour 和当前点位下的 MuseumInteractionTarget，一般不用维护手工列表。

## 单个交互目标

每个小模型只在 MuseumInteractionTarget 中集中配置：

- targetId：不填时使用物体名称。
- selectable：小模型保持开启；只用于文物 Hover/旋转的目标应关闭，避免加入单选合集。
- view：同物体上的 MuseumOptionView，可留空自动查找。
- videoClip：该选项对应视频，可为空。
- artifactGroup：该选项对应文物组，可为空。
- actions：该选项独有技能，可为空。

MuseumOptionView 只处理 ModelVisualMessage：

- hoverRenderers 为空表示移入不高亮。
- selectedRenderers 可同时包含小模型和需要联动高亮的其他 Renderer。
- selectedScale = 1 表示选中不放大。
- 默认颜色属性为 _EmissionColor；材质需支持并启用该属性，其他 Shader 可改为 _BaseColor 或 _Color。
- Selected 优先于 Hovered，因此选中后移出仍保持选中视觉。

MuseumOptionMrtkInput 只把 FocusEnter、FocusExit、PointerClicked 转成输入消息。物体仍需 Collider，且 MRTK 指针必须能够命中它。

## 模块职责

- MuseumPointSelectionModule：只管理单选状态并发布 OptionSelectionChangedMessage，不引用视频或文物模块。
- MuseumVideoArtifactPresentationModule：可选业务流程，把选择消息转换成视频和文物命令；不需要这种模式的点位不要挂。
- PointVideoModule：只处理 VideoMessage，退出时强制停止并隐藏视频。
- MuseumArtifactGroupModule：只处理 ArtifactMessage，通过 ArtifactRotationMessage 控制旋转。
- ArtifactAutoRotation：处理旋转命令和对应目标的 Hover 输入；多个 Hover 来源全部退出后才恢复旋转，退出点位时强制停止。
- UnityEventOptionAction：处理目标独有的选中、取消、点位退出和销毁事件。

## 视频/文物切换

MuseumVideoArtifactPresentationModule.buttonRoot 配置按钮显示根节点。切换按钮添加 MuseumDisplayToggleInput；它只发布 MuseumDisplayToggleMessage。

~~~text
选择目标
  -> 播放目标 VideoClip
  -> 准备目标 ArtifactGroup
  -> 显示 buttonRoot

点击切换按钮
  -> 停止视频
  -> 显示文物并开始旋转

再次点击
  -> 隐藏文物并停止旋转
  -> 重新播放当前目标视频
~~~

## 两个物体交替显示及多来源控制

需要“按钮第一次显示 A、再次显示 B”，添加 ObjectSwitchModule：

- groupId：该切换组的唯一名称。
- firstObject / secondObject：两个互斥物体。
- initialState：进入点位后的初始状态。

按钮或 UnityEvent 使用 ObjectDisplayMessageInput，它不引用 ObjectSwitchModule，只需填写相同 groupId：

- Toggle()：切换基础状态。
- SetBaseFirst/Second/Hidden()：长期改变基础状态。
- OverrideFirst/Second/Hidden()：当前来源临时覆盖。
- ReleaseOverride()：释放当前来源并恢复原基础状态。

多个临时来源同时存在时优先级高者生效；同优先级采用最后更新者。退出点位时基础状态和全部临时来源都会清空，两个物体统一隐藏。

## 自定义消息和模块

新功能优先新增自己的消息结构并实现处理接口：

~~~csharp
public readonly struct AudioMessage : IPointMessage
{
    public MuseumPoint Point { get; }
    public AudioClip Clip { get; }
}

public sealed class PointAudioModule :
    PointMessageModule<AudioMessage>
{
    public override void Handle(in AudioMessage message)
    {
        // 只处理音频消息
    }
}
~~~

输入适配器继承 PointMessageInputBehaviour 或 TargetMessageInputBehaviour，只负责发布消息。不要让输入脚本直接调用具体业务模块，也不要让多个模块分别对同一个物体调用 SetActive；同一状态必须只有一个所有者模块。

## 统一退出

~~~text
距离退出 / 切换点位 / 卸载
  -> MuseumPoint.ExitPoint
  -> PointRuntimeController 逆序 ExitPoint
  -> 清空选择与展示状态
  -> 停止视频
  -> 隐藏文物
  -> 停止全部旋转
  -> 清空 ObjectSwitch 临时覆盖
  -> 发布 PointLifecycleMessage(Exited)
~~~

新代码不会自动迁移旧 SmallModel 预制体。建议先复制一个点位做模板，验证后再关闭旧 Trigger/InputRouting，避免两套输入同时触发。
