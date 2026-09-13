# Museum/N 点位交互第一版

本目录是新点位交互方案的独立实现，暂不删除或修改旧的 Museum/Point。第一阶段只有点位级距离触发；碰撞体、手势和手动触发后续只需调用 MuseumPointCoordinator.ReportEnter/ReportExit，不需要修改点位内部业务。

## 最少挂载方式

场景公共根节点：

~~~text
MuseumPointSystem
  - MuseumPointCoordinator
      observer：主相机（可留空自动找 MainCamera）
      pointSearchRoot：所有新点位的公共父节点
~~~

每个点位：

~~~text
PointXX
  - MuseumPoint
  - DistancePointTrigger
  RuntimeModel
    - PointRuntimeController
    - MuseumPointSelectionModule
    - PointVideoModule
    - MuseumArtifactGroupModule
~~~

PointRuntimeController 默认自动收集子节点中的所有 PointModuleBehaviour，一般不用手工维护模块列表。

## SmallModel 配置

1. 在四个可点击小模型上分别添加 MuseumOptionView 和 MuseumOptionMrtkInput。
2. hoverRenderers 只放当前小模型的 Renderer。
3. selectedRenderers 放当前小模型和对应 BigModel/PointXX 的 Renderer。
   高亮默认写入 `_EmissionColor`；材质需支持并启用该属性，其他 Shader 可直接修改 colorProperty。
4. 在 MuseumPointSelectionModule.options 中，每个展品只增加一项，配置 View、VideoClip、对应 CulturalObjsXX 和可选 Actions。
5. PointVideoModule 配置共享的 VideoPlayer 与 Video 根节点。
6. MuseumArtifactGroupModule 配置 CulturalObjs 根节点；子组为空时会自动收集直接子节点。
7. 每个需要旋转的文物添加 ArtifactAutoRotation。它自己处理 MRTK 移入暂停、移出恢复，不再配置 16 条 InputRouting 路由。
8. 视频/文物切换按钮直接交给 MuseumPointSelectionModule.displayToggleButton。

## 生命周期

~~~text
距离进入
  -> Coordinator 仲裁
  -> MuseumPoint.EnterPoint
  -> PointRuntimeController.EnterPoint
  -> 各功能模块 EnterPoint

距离退出/切换点位/卸载
  -> MuseumPoint.ExitPoint
  -> 模块逆序 ExitPoint
  -> 停止视频、停止旋转、清空选中、关闭 UI

销毁
  -> Shutdown
  -> 移除按钮监听并释放模块引用
~~~

ExitPoint 和 Shutdown 均按可重复调用设计。

## 当前边界

- 点位级激活只实现 DistancePointTrigger。
- MuseumOptionMrtkInput 只处理已经进入点位后的展品移入、移出和点击，不参与点位激活仲裁。
- 新代码尚未自动迁移现有 SmallModel.prefab；确认脚本编译和测试通过后再单独迁移预制体引用。
