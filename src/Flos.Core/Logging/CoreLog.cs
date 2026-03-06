using System.Runtime.CompilerServices;

namespace Flos.Core.Logging;

/// <summary>
/// Defines the severity levels for messages emitted through <see cref="CoreLog"/>.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Diagnostic message only relevant during development. Stripped in Release builds.
    /// </summary>
    Debug,

    /// <summary>
    /// Informational message for normal operation.
    /// </summary>
    Info,

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
/// Minimal logging facade for the Core microkernel.
/// Uses a thread-static handler to support per-session isolation.
/// Adapters and host applications attach a handler via <see cref="Handler"/> to receive log messages.
/// When the thread-local <see cref="Handler"/> is <see langword="null"/>, the static
/// <see cref="FallbackHandler"/> is tried before discarding the message.
/// </summary>
public static class CoreLog
{
    /// <summary>
    /// Gets or sets the callback that receives all log messages for the current session/thread.
    /// Set this to a delegate that routes messages to the host logging system.
    /// When <see langword="null"/>, <see cref="FallbackHandler"/> is tried before discarding.
    /// Thread-static: each thread (and thus each session's main thread) has its own handler.
    /// </summary>
    [ThreadStatic]
    public static Action<LogLevel, string>? Handler;

    /// <summary>
    /// Gets or sets a fallback handler that fires when the thread-local <see cref="Handler"/> is
    /// <see langword="null"/>. Useful for capturing log messages from background threads (e.g.,
    /// asset loading, save I/O) that don't have a thread-local handler set.
    /// Not thread-static: shared across all threads.
    /// </summary>
    private static volatile Action<LogLevel, string>? _fallbackHandler;

    public static Action<LogLevel, string>? FallbackHandler
    {
        get => _fallbackHandler;
        set => _fallbackHandler = value;
    }

    /// <summary>
    /// Returns <see langword="true"/> when at least one handler (thread-local or
    /// fallback) is attached. Used by <see cref="CoreLogInterpolatedStringHandler"/>
    /// to skip interpolation entirely when nobody is listening.
    /// </summary>
    public static bool HasHandler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Handler is not null || _fallbackHandler is not null;
    }

    /// <summary>
    /// Interpolated string overloads of debug message. (zero-alloc when no handler)
    /// </summary>
    /// <param name="handler">Interpolated string handler.</param>
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Debug(
        [InterpolatedStringHandlerArgument()] ref InterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
            (Handler ?? _fallbackHandler)!.Invoke(LogLevel.Debug, handler.ToStringAndClear());
    }

    /// <summary>
    /// Interpolated string overloads of informational message.
    /// </summary>
    /// <param name="handler">Interpolated string handler.</param>
    public static void Info(
        [InterpolatedStringHandlerArgument()] ref InterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
            (Handler ?? _fallbackHandler)!.Invoke(LogLevel.Info, handler.ToStringAndClear());
    }

    /// <summary>
    /// Interpolated string overloads of warning message.
    /// </summary>
    /// <param name="handler">Interpolated string handler.</param>
    public static void Warn(
        [InterpolatedStringHandlerArgument()] ref InterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
            (Handler ?? _fallbackHandler)!.Invoke(LogLevel.Warn, handler.ToStringAndClear());
    }

    /// <summary>
    /// Interpolated string overloads of error message.
    /// </summary>
    /// <param name="handler">Interpolated string handler.</param>
    public static void Error(
        [InterpolatedStringHandlerArgument()] ref InterpolatedStringHandler handler)
    {
        if (handler.IsEnabled)
            (Handler ?? _fallbackHandler)!.Invoke(LogLevel.Error, handler.ToStringAndClear());
    }

    /// <summary>Logs a debug message. Compiled away in Release builds.</summary>
    /// <param name="msg">The message text.</param>
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Debug(string msg) => (Handler ?? _fallbackHandler)?.Invoke(LogLevel.Debug, msg);

    /// <summary>Logs an informational message.</summary>
    /// <param name="msg">The message text.</param>
    public static void Info(string msg) => (Handler ?? _fallbackHandler)?.Invoke(LogLevel.Info, msg);

    /// <summary>Logs a warning message.</summary>
    /// <param name="msg">The message text.</param>
    public static void Warn(string msg) => (Handler ?? _fallbackHandler)?.Invoke(LogLevel.Warn, msg);

    /// <summary>Logs an error message.</summary>
    /// <param name="msg">The message text.</param>
    public static void Error(string msg) => (Handler ?? _fallbackHandler)?.Invoke(LogLevel.Error, msg);
}
