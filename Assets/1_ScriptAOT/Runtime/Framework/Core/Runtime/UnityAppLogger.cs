using UnityEngine;

namespace AOT.HotUpdate.Framework
{
    /// <summary>
    /// 正式运行时日志实现。
    /// 通过接口包装 Unity Debug 后，业务模块在测试中不必依赖 Console 窗口。
    /// </summary>
    public sealed class UnityAppLogger : IAppLogger
    {
        private readonly string prefix;

        public UnityAppLogger(string prefix = "[Museum]")
        {
            this.prefix = string.IsNullOrWhiteSpace(prefix)
                ? "[Museum]"
                : prefix;
        }

        public void Log(string message)
        {
            Debug.Log(prefix + " " + message);
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(prefix + " " + message);
        }

        public void LogError(string message)
        {
            Debug.LogError(prefix + " " + message);
        }
    }
}
