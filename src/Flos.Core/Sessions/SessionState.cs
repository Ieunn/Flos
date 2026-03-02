namespace Flos.Core.Sessions;

/// <summary>
/// Represents the lifecycle state of an <see cref="ISession"/>.
/// </summary>
public enum SessionState
{
    /// <summary>Initial state before <see cref="ISession.Initialize"/> is called.</summary>
    Created,

    /// <summary>Modules are being loaded and initialized.</summary>
    Initializing,

    /// <summary>Initialization is complete; waiting for <see cref="ISession.Start"/>.</summary>
    Initialized,

    /// <summary>Session is active and accepting ticks.</summary>
    Running,

    /// <summary>Session is paused; ticks are buffered.</summary>
    Paused,

    /// <summary>Modules are being shut down in reverse order.</summary>
    ShuttingDown,

    /// <summary>Session resources have been released.</summary>
    Disposed
}
