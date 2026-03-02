namespace Flos.Pattern.CQRS;

internal sealed class EventJournal : IEventJournal
{
    private readonly List<JournalEntry> _entries = [];

    public void Append(long tick, IEvent evt)
    {
        _entries.Add(new JournalEntry(tick, evt));
    }

    public IReadOnlyList<JournalEntry> GetRange(long fromTick, long toTick)
    {
        if (_entries.Count == 0)
            return [];

        int start = LowerBound(fromTick);
        if (start >= _entries.Count)
            return [];

        int end = UpperBound(toTick);
        if (end < start)
            return [];

        int count = end - start + 1;
        var result = new List<JournalEntry>(count);
        for (int i = start; i <= end; i++)
        {
            result.Add(_entries[i]);
        }
        return result;
    }

    public void Truncate(long beforeTick)
    {
        int cutIndex = LowerBound(beforeTick);
        if (cutIndex > 0) _entries.RemoveRange(0, cutIndex);
    }

    /// <summary>Returns index of first entry with Tick >= target.</summary>
    private int LowerBound(long target)
    {
        int lo = 0, hi = _entries.Count;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (_entries[mid].Tick < target)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

    /// <summary>Returns index of last entry with Tick <= target.</summary>
    private int UpperBound(long target)
    {
        int lo = 0, hi = _entries.Count;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (_entries[mid].Tick <= target)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo - 1;
    }
}
