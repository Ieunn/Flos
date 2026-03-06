namespace Flos.Core.Logging;

/// <summary>
/// Runtime debug flags for the Flos framework.
/// Thread-static so each session's main thread can have its own configuration.
/// </summary>
public static class FlosDebug
{
    /// <summary>
    /// When <see langword="true"/>, Core subsystems (MessageBus, World, Scheduler)
    /// verify that they are accessed from the thread that created them, and throw
    /// <see cref="Errors.FlosException"/> on violation.
    /// Thread-static: each thread (and thus each session's main thread) has its own setting.
    /// Call <see cref="EnableForCurrentThread"/> to enable on the current thread.
    /// </summary>
    [ThreadStatic]
    public static bool EnforceThreadSafety;

    /// <summary>
    /// Enables thread-safety enforcement on the current thread.
    /// Called automatically by <see cref="Sessions.Session.Initialize"/> in DEBUG builds.
    /// </summary>
    public static void EnableForCurrentThread()
    {
        EnforceThreadSafety = true;
    }
}
