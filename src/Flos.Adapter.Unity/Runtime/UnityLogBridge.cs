using Flos.Core.Logging;

namespace Flos.Adapter.Unity
{
    /// <summary>
    /// Bridges <see cref="CoreLog"/> to Unity's Debug.Log/LogWarning/LogError.
    /// </summary>
    public static class UnityLogBridge
    {
        /// <summary>
        /// Log handler suitable for assigning to <see cref="CoreLog.Handler"/>.
        /// </summary>
        public static void Handler(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    UnityEngine.Debug.Log($"[Flos:Debug] {message}");
                    break;
                case LogLevel.Info:
                    UnityEngine.Debug.Log($"[Flos] {message}");
                    break;
                case LogLevel.Warn:
                    UnityEngine.Debug.LogWarning($"[Flos] {message}");
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError($"[Flos] {message}");
                    break;
            }
        }
    }
}
