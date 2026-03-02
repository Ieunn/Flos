using Flos.Core.Logging;

namespace Flos.Diagnostics;

/// <summary>
/// Bridges <see cref="CoreLog.Handler"/> to an <see cref="ITracer"/> instance,
/// unifying Core internal diagnostics with the project-wide tracing pipeline.
/// </summary>
public static class CoreLogBridge
{
    /// <summary>
    /// Sets <see cref="CoreLog.Handler"/> to forward all Core log messages
    /// to the given <paramref name="tracer"/>.
    /// </summary>
    public static void Install(ITracer tracer)
    {
        CoreLog.Handler = (level, message) =>
        {
            var traceLevel = level switch
            {
                LogLevel.Log => TraceLevel.Info,
                LogLevel.Warn => TraceLevel.Warning,
                LogLevel.Error => TraceLevel.Error,
                _ => TraceLevel.Debug,
            };
            tracer.Log(traceLevel, message);
        };
    }

    /// <summary>
    /// Removes the CoreLog bridge (sets Handler to null).
    /// </summary>
    public static void Uninstall()
    {
        CoreLog.Handler = null;
    }
}
