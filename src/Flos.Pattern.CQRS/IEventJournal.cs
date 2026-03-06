namespace Flos.Pattern.CQRS;

/// <summary>Append-only log of domain events, indexed by tick number.</summary>
public interface IEventJournal
{
    /// <summary>Records an event at the given tick. Accepts an <see cref="EventBuffer.EventSlot"/>
    /// to avoid boxing struct events during append.</summary>
    /// <param name="tick">The tick number at which the event occurred.</param>
    /// <param name="slot">The event slot to record.</param>
    void Append(long tick, in EventBuffer.EventSlot slot);

    /// <summary>Returns all entries in the inclusive tick range.</summary>
    /// <param name="fromTick">The start of the tick range (inclusive).</param>
    /// <param name="toTick">The end of the tick range (inclusive).</param>
    /// <param name="result">List to populate with matching entries.</param>
    /// <returns><see langword="true"/> if any entries were added.</returns>
    bool GetRange(long fromTick, long toTick, List<JournalEntry> result);

    /// <summary>Removes all entries before the given tick.</summary>
    /// <param name="beforeTick">The tick before which all entries are removed.</param>
    void Truncate(long beforeTick);
}
