using Flos.Core.Messaging;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.State;

namespace Flos.Core.Sessions;

/// <summary>
/// Top-level game session that owns the world, scheduler, message bus, and module lifecycle.
/// </summary>
public interface ISession : IDisposable
{
    /// <summary>
    /// Current lifecycle state.
    /// </summary>
    SessionState State { get; }

    /// <summary>
    /// The session's state container.
    /// </summary>
    IWorld World { get; }

    /// <summary>
    /// The session's tick scheduler.
    /// </summary>
    IScheduler Scheduler { get; }

    /// <summary>
    /// The session's pub/sub message bus.
    /// </summary>
    IMessageBus MessageBus { get; }

    /// <summary>
    /// The session's root service scope.
    /// </summary>
    IServiceScope RootScope { get; }

    /// <summary>
    /// Loads modules, registers core services, and transitions to <see cref="SessionState.Initializing"/>.
    /// Publishes <see cref="SessionInitializedMessage"/> on success.
    /// </summary>
    /// <param name="config">The session configuration describing modules, tick mode, and DI adapter.</param>
    /// <exception cref="Errors.FlosException">Thrown with <see cref="Errors.CoreErrors.InitializationFailed"/> when module loading or initialization fails.</exception>
    void Initialize(SessionConfig config);

    /// <summary>
    /// Transitions from <see cref="SessionState.Initializing"/> to <see cref="SessionState.Running"/>.
    /// Calls <see cref="Module.IModule.OnStart"/> on all modules and publishes <see cref="SessionStartedMessage"/>.
    /// </summary>
    void Start();

    /// <summary>
    /// Transitions from <see cref="SessionState.Running"/> to <see cref="SessionState.Paused"/>.
    /// Publishes <see cref="SessionPausedMessage"/>.
    /// </summary>
    void Pause();

    /// <summary>
    /// Transitions from <see cref="SessionState.Paused"/> to <see cref="SessionState.Running"/>.
    /// Drains buffered ticks and publishes <see cref="SessionResumedMessage"/>.
    /// </summary>
    void Resume();

    /// <summary>
    /// Shuts down all modules in reverse order and publishes <see cref="SessionShutdownMessage"/>.
    /// </summary>
    void Shutdown();
}
