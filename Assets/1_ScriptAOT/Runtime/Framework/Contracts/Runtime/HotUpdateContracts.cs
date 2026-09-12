using System.Threading;
using System.Threading.Tasks;

namespace AOT.HotUpdate.Framework
{
    /// <summary>
    /// AOT 主程序调用热更新程序集的唯一入口。
    /// 主程序只引用该接口，不直接引用 Museum.HotUpdate，
    /// 因此更新 DLL 后不需要重新打 Android/iOS 安装包。
    /// </summary>
    public interface IHotUpdateEntry
    {
        Task InitializeAsync(
            IServiceRegistry services,
            CancellationToken cancellationToken);

        Task ShutdownAsync(CancellationToken cancellationToken);
    }
}
