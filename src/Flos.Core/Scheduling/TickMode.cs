namespace Flos.Core.Scheduling;

/// <summary>
/// Determines how the scheduler advances simulation time.
/// </summary>
public enum TickMode
{
    /// <summary>
    /// Each call to <see cref="IScheduler.Step"/> fires exactly one tick.
    /// Suitable for turn-based games and deterministic replay.
    /// </summary>
    StepBased,

    /// <summary>
    /// The scheduler accumulates real-time delta and fires fixed-timestep ticks.
    /// Suitable for real-time simulations.
    /// </summary>
    FixedTick
}
