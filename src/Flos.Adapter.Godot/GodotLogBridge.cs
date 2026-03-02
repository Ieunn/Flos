using Flos.Core.Logging;

namespace Flos.Adapter.Godot;

/// <summary>
/// Bridges <see cref="CoreLog"/> to Godot's GD.Print/PushWarning/PushError.
/// </summary>
public static class GodotLogBridge
{
    /// <summary>
    /// Log handler suitable for assigning to <see cref="CoreLog.Handler"/>.
    /// </summary>
    public static void Handler(LogLevel level, string message)
    {
        switch (level)
        {
            case LogLevel.Log:
                GD.Print($"[Flos] {message}");
                break;
            case LogLevel.Warn:
                GD.PushWarning($"[Flos] {message}");
                break;
            case LogLevel.Error:
                GD.PushError($"[Flos] {message}");
                break;
        }
    }
}
