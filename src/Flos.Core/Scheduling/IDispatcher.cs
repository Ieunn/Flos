namespace Flos.Core.Scheduling;

/// <summary>
/// Thread-safe action queue. External threads enqueue work; the scheduler drains it
/// on the main thread at the start of each tick.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Queues an action for execution on the main thread.
    /// </summary>
    /// <param name="action">The action to enqueue.</param>
    void Enqueue(Action action);

    /// <summary>
    /// Executes and removes all queued actions. Called by the scheduler before each tick.
    /// </summary>
    void DrainAll();

    /// <summary>
    /// Optional callback invoked when a dispatched action throws.
    /// If <see langword="null"/> (default), exceptions are collected and rethrown as <see cref="Errors.FlosException"/>
    /// after all queued actions have run.
    /// Set this to a custom callback to handle exceptions differently (e.g., log-and-continue).
    /// </summary>
    Action<Exception>? OnActionException { get; set; }
}
