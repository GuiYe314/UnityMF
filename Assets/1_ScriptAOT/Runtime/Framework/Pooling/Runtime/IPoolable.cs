namespace AOT.HotUpdate.Framework.Pooling
{
    /// <summary>
    /// 可被 <see cref="ReferencePool"/> 管理的纯 C# 引用对象。
    /// 实现类必须是非抽象引用类型，并提供公开无参构造函数。
    /// 此接口不适用于 GameObject、Component 或其他 UnityEngine.Object。
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 实例从池中取出后调用。
        /// 在这里写入一次使用所需的默认状态，但不要保存跨次使用的数据。
        /// </summary>
        void OnAcquire();

        /// <summary>
        /// 实例归还池之前调用。
        /// 必须清理引用、委托、集合及本次业务状态，防止脏数据和对象泄漏。
        /// 如果该方法抛出异常，实例会被丢弃，不再进入空闲队列。
        /// </summary>
        void OnRelease();
    }
}
