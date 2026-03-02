namespace Flos.Core.Logging;

/// <summary>
/// Defines the severity levels for messages emitted through <see cref="CoreLog"/>.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Informational message for normal operation.
    /// </summary>
    Log,

    /// <summary>
    /// Warning message indicating a potential issue that does not prevent operation.
    /// </summary>
    Warn,

    /// <summary>
    /// Error message indicating a failure or critical problem.
    /// </summary>
    Error
}

/// <summary>
/// Minimal, static logging facade for the Core microkernel.
/// Adapters and host applications attach a handler via <see cref="Handler"/> to receive log messages.
/// </summary>
public static class CoreLog
{
    /// <summary>
    /// Gets or sets the callback that receives all log messages.
    /// Set this to a delegate that routes messages to the host logging system.
    /// When <see langword="null"/>, all log messages are silently discarded.
    /// </summary>
    public static Action<LogLevel, string>? Handler { get; set; }

    /// <summary>Logs an informational message.</summary>
    /// <param name="msg">The message text.</param>
    public static void Log(string msg) => Handler?.Invoke(LogLevel.Log, msg);

    /// <summary>Logs a warning message.</summary>
    /// <param name="msg">The message text.</param>
    public static void Warn(string msg) => Handler?.Invoke(LogLevel.Warn, msg);

    /// <summary>Logs an error message.</summary>
    /// <param name="msg">The message text.</param>
    public static void Error(string msg) => Handler?.Invoke(LogLevel.Error, msg);
}
