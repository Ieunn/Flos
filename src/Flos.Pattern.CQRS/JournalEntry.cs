namespace Flos.Pattern.CQRS;

/// <summary>A single entry in the event journal.</summary>
/// <param name="Tick">The tick at which the event was recorded.</param>
/// <param name="Event">The domain event.</param>
public readonly record struct JournalEntry(long Tick, IEvent Event);
