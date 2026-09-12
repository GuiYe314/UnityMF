# ReferencePool 使用说明

## 用途

ReferencePool 是一个面向纯 C# 引用对象的全局复用池，用于减少短生命周期对象反复 new 所产生的 GC 分配。

适合：

- 事件参数
- 命令对象
- 临时上下文
- 网络消息包装
- 计算过程中的临时数据容器

不适合：

- GameObject
- Component
- ScriptableObject
- Texture、Material 等 UnityEngine.Object
- 含有必须立即释放的非托管资源的对象

命名空间：

```csharp
using AOT.HotUpdate.Framework.Pooling;
```

## 创建可池化类型

对象必须：

1. 是引用类型；
2. 实现 IPoolable；
3. 具有公开无参构造函数；
4. 在 OnRelease 中清空全部本次使用状态。

```csharp
public sealed class DamageContext : IPoolable
{
    public int Damage;
    public object Attacker;
    public object Target;

    public void OnAcquire()
    {
        // 可选：设置每次取出时的默认状态。
    }

    public void OnRelease()
    {
        Damage = 0;
        Attacker = null;
        Target = null;
    }
}
```

## 基本使用

```csharp
DamageContext context = ReferencePool.Acquire<DamageContext>();

try
{
    context.Damage = 100;
    context.Attacker = attacker;
    context.Target = target;

    ApplyDamage(context);
}
finally
{
    ReferencePool.Release(context);
}
```

建议使用 try/finally，确保业务异常时对象仍能归还。

对象调用 Release 后，所有权已经回到对象池。调用方不得继续读写该对象，也不要把它保存在字段、闭包或异步回调中。

## 预热

```csharp
ReferencePool.Prewarm<DamageContext>(32);
```

Prewarm 的参数表示“新增多少个对象”，不是最终容量。适合在加载阶段提前创建，减少首次业务高峰中的分配。

## 裁剪与清理

```csharp
// 只保留 16 个空闲对象。
int removed = ReferencePool.Trim<DamageContext>(16);

// 清除该类型全部空闲对象。
ReferencePool.Clear<DamageContext>();

// 清除所有类型池的空闲对象。
ReferencePool.ClearAll();
```

Clear 和 ClearAll 不会破坏已经借出的对象；这些对象之后仍可正常 Release。

## 统计信息

```csharp
ReferencePoolInfo info = ReferencePool.GetInfo<DamageContext>();

UnityEngine.Debug.Log(info);
UnityEngine.Debug.Log($"使用中: {info.UsingCount}");
UnityEngine.Debug.Log($"空闲: {info.UnusedCount}");
UnityEngine.Debug.Log($"实际创建: {info.CreateCount}");
```

字段说明：

| 字段 | 含义 |
| --- | --- |
| ReferenceType | 对象池管理的实际类型 |
| UsingCount | 已借出且尚未归还的数量 |
| UnusedCount | 当前可复用数量 |
| AcquireCount | 历史获取次数 |
| ReleaseCount | 历史成功归还次数 |
| CreateCount | 历史实际创建数量 |
| DiscardCount | 清理失败、裁剪或清空产生的丢弃数量 |

## 错误保护

对象池会拒绝：

- 归还 null；
- 同一实例重复归还；
- 归还不是由当前类型池借出的实例；
- 将不满足泛型约束的类型加入池。

如果 OnAcquire 抛出异常，对象会从使用中集合移除并丢弃。

如果 OnRelease 抛出异常，对象不会回到空闲栈，避免后续业务取得状态不完整的实例。

## 线程说明

池的内部集合和统计值使用锁保护，但 IPoolable 生命周期方法属于业务代码。

建议仍然在 Unity 主线程使用。不要在 OnAcquire 或 OnRelease 中执行长耗时操作，也不要依赖 Unity 场景对象的跨线程访问。

## AOT 与 HybridCLR

公共获取入口使用泛型和 new() 约束，不依赖 Activator、MakeGenericType 或运行时泛型反射。

对需要在 AOT 环境直接使用的具体类型，仍应确保 IL2CPP 能看到对应的 Acquire<T>、Release<T> 调用，或按照项目现有 HybridCLR 泛型补充策略处理。

## API 一览

| API | 作用 |
| --- | --- |
| Acquire<T>() | 获取实例 |
| Release<T>(instance) | 按编译期类型归还 |
| Release(IPoolable) | 按运行时类型归还已借出的对象 |
| Prewarm<T>(count) | 新增预热实例 |
| Trim<T>(target) | 裁剪空闲数量 |
| Clear<T>() | 清除某类型空闲对象 |
| ClearAll() | 清除所有类型空闲对象 |
| GetInfo<T>() | 获取某类型统计 |
| GetAllInfos() | 获取全部统计 |
