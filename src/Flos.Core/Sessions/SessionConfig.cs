using Flos.Core.Module;
using Flos.Core.Scheduling;

namespace Flos.Core.Sessions;

/// <summary>
/// Configuration record passed to <see cref="ISession.Initialize"/> to set up a game session.
/// </summary>
public sealed class SessionConfig
{
    /// <summary>
    /// Ordered list of modules to load. Dependencies are resolved via topological sort.
    /// </summary>
    public required IReadOnlyList<IModule> Modules { get; init; }

    /// <summary>
    /// Determines whether the session uses step-based or fixed-timestep ticking.
    /// </summary>
    public TickMode TickMode { get; init; }

    /// <summary>
    /// Seconds per tick when using <see cref="Scheduling.TickMode.FixedTick"/>. Defaults to 1/60.
    /// </summary>
    public float FixedTimeStep { get; init; } = 1f / 60f;

    /// <summary>
    /// Seed for the deterministic random number generator.
    /// </summary>
    public int RandomSeed { get; init; } = 0;

    /// <summary>
    /// Optional external DI container adapter. When <see langword="null"/>, the built-in <see cref="BuiltInMinimalScope"/> is used.
    /// </summary>
    public IDIAdapter? DIAdapter { get; init; }
}
