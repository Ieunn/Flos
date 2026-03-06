namespace Flos.Pattern.CQRS;

/// <summary>
/// A journal entry holding a tick number and a type-erased event.
/// The event is stored without boxing; use <see cref="BoxedEvent"/> when an <see cref="IEvent"/>
/// reference is needed (e.g., for serialization or replay).
/// </summary>
public readonly struct JournalEntry
{
    /// <summary>The tick at which the event was recorded.</summary>
    public readonly long Tick;

    /// <summary>The runtime type of the stored event.</summary>
    public readonly Type EventType;

    internal readonly IJournalEventHolder Holder;

    internal JournalEntry(long tick, Type eventType, IJournalEventHolder holder)
    {
        Tick = tick;
        EventType = eventType;
        Holder = holder;
    }

    /// <summary>
    /// Gets the event as an <see cref="IEvent"/> reference.
    /// This boxes struct events on access — prefer typed access patterns when possible.
    /// </summary>
    public IEvent BoxedEvent => Holder.BoxedEvent;
}

/// <summary>
/// Type-erased holder for journal events. Avoids boxing struct events in storage.
/// </summary>
internal interface IJournalEventHolder
{
    IEvent BoxedEvent { get; }
}

/// <summary>
/// Typed journal event holder that stores the event value without boxing.
/// </summary>
internal sealed class JournalEventHolder<T> : IJournalEventHolder where T : IEvent
{
    internal T Event;

    internal JournalEventHolder(T evt) => Event = evt;

    public IEvent BoxedEvent => Event;
}
