namespace Flos.Core.Scheduling;

/// <summary>
/// Controls simulation tick dispatch. Supports both step-based and fixed-timestep modes.
/// </summary>
public interface IScheduler
{
    /// <summary>
    /// The tick mode this scheduler operates in.
    /// </summary>
    TickMode Mode { get; }

    /// <summary>
    /// Fires a single tick. Only valid in <see cref="TickMode.StepBased"/> mode.
    /// </summary>
    void Step();

    /// <summary>
    /// Accumulates delta time and fires zero or more fixed-timestep ticks.
    /// Only valid in <see cref="TickMode.FixedTick"/> mode.
    /// </summary>
    /// <param name="deltaTime">Elapsed real time in seconds since the last call.</param>
    void Tick(double deltaTime);

    /// <summary>
    /// The number of ticks fired since session start.
    /// </summary>
    long CurrentTick { get; }

    /// <summary>
    /// Total simulated time in seconds. Only meaningful in <see cref="TickMode.FixedTick"/> mode.
    /// Returns 0 in <see cref="TickMode.StepBased"/> mode since steps have no time dimension.
    /// Uses <c>double</c> to maintain precision over long play sessions.
    /// </summary>
    double ElapsedTime { get; }

    /// <summary>
    /// Maximum ticks per frame to prevent spiral-of-death in FixedTick mode.
    /// </summary>
    int MaxCatchUpTicks { get; set; }

    /// <summary>
    /// True when the scheduler is paused.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Pauses or unpauses the scheduler.
    /// </summary>
    /// <param name="paused">True to pause, false to unpause.</param>
    void SetPaused(bool paused);

    /// <summary>
    /// Replays buffered ticks/time accumulated during pause, up to <see cref="MaxCatchUpTicks"/>.
    /// </summary>
    /// <returns>The number of ticks actually fired.</returns>
    int DrainPausedBuffer();
}
