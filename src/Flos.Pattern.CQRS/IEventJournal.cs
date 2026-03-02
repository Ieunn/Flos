namespace Flos.Pattern.CQRS;

/// <summary>Append-only log of domain events, indexed by tick number.</summary>
public interface IEventJournal
{
    /// <summary>Records an event at the given tick.</summary>
    /// <param name="tick">The tick number at which the event occurred.</param>
    /// <param name="evt">The domain event to record.</param>
    void Append(long tick, IEvent evt);

    /// <summary>Returns all entries in the inclusive tick range.</summary>
    /// <param name="fromTick">The start of the tick range (inclusive).</param>
    /// <param name="toTick">The end of the tick range (inclusive).</param>
    /// <returns>A read-only list of journal entries within the specified range.</returns>
    IReadOnlyList<JournalEntry> GetRange(long fromTick, long toTick);

    /// <summary>Removes all entries before the given tick.</summary>
    /// <param name="beforeTick">The tick before which all entries are removed.</param>
    void Truncate(long beforeTick);
}
