using Flos.Core.Messaging;

namespace Flos.Core.Scheduling;

/// <summary>
/// Published by the <see cref="IScheduler"/> on every simulation tick.
/// Patterns and modules subscribe to this to perform per-tick work.
/// </summary>
/// <param name="Tick">The monotonically increasing tick number.</param>
/// <param name="DeltaTime">
/// Elapsed time in seconds since the previous tick.
/// In <see cref="TickMode.FixedTick"/> mode, this equals <c>SessionConfig.FixedTimeStep</c>.
/// In <see cref="TickMode.StepBased"/> mode, this is always <c>0f</c> — discrete-event
/// simulations should rely solely on <see cref="Tick"/> for ordering and use domain-level
/// time representations (e.g. turn numbers, action points) rather than wall-clock deltas.
/// The <c>float</c> type was chosen to align with game engine conventions (Unity, Godot)
/// where frame deltas are single-precision.
/// </param>
public readonly record struct TickMessage(long Tick, float DeltaTime) : IMessage;
