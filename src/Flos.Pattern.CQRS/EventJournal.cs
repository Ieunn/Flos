namespace Flos.Pattern.CQRS;

/// <summary>
/// Ring-buffer-backed event journal. O(1) append at capacity, O(log n) range queries.
/// When <see cref="MaxEntries"/> is reached, the oldest entries are silently overwritten.
/// Events are stored without boxing via <see cref="IJournalEventHolder"/>; boxing is deferred
/// to <see cref="JournalEntry.BoxedEvent"/> access at query time.
/// </summary>
internal sealed class EventJournal : IEventJournal
{
    private JournalEntry[] _buffer;
    private int _head;
    private int _count;
    private int _maxEntries;

    internal EventJournal()
    {
        _buffer = new JournalEntry[16];
    }

    internal int MaxEntries
    {
        get => _maxEntries;
        set => _maxEntries = Math.Max(0, value);
    }

    /// <summary>Current number of entries in the journal.</summary>
    internal int Count => _count;

    public void Append(long tick, in EventBuffer.EventSlot slot)
    {
        var entry = slot.ToJournalEntry(tick);

        if (_maxEntries > 0)
        {
            EnsureCapacity(_maxEntries);

            if (_count < _maxEntries)
            {
                int index = (_head + _count) % _buffer.Length;
                _buffer[index] = entry;
                _count++;
            }
            else
            {
                _buffer[_head] = entry;
                _head = (_head + 1) % _buffer.Length;
            }
        }
        else
        {
            EnsureCapacity(_count + 1);
            int index = (_head + _count) % _buffer.Length;
            _buffer[index] = entry;
            _count++;
        }
    }

    public bool GetRange(long fromTick, long toTick, List<JournalEntry> result)
    {
        result.Clear();

        if (_count == 0)
            return false;

        int start = LowerBound(fromTick);
        if (start >= _count)
            return false;

        int end = UpperBound(toTick);
        if (end < start)
            return false;

        int resultCount = end - start + 1;
        for (int i = 0; i < resultCount; i++)
        {
            var entry = _buffer[(_head + start + i) % _buffer.Length];
            result.Add(entry);
        }
        return true;
    }

    public void Truncate(long beforeTick)
    {
        int cutCount = LowerBound(beforeTick);
        if (cutCount > 0)
        {
            for (int i = 0; i < cutCount; i++)
            {
                _buffer[(_head + i) % _buffer.Length] = default;
            }
            _head = (_head + cutCount) % _buffer.Length;
            _count -= cutCount;
        }
    }

    /// <summary>Returns logical index of first entry with Tick >= target.</summary>
    private int LowerBound(long target)
    {
        int lo = 0, hi = _count;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (_buffer[(_head + mid) % _buffer.Length].Tick < target)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

    /// <summary>Returns logical index of last entry with Tick <= target.</summary>
    private int UpperBound(long target)
    {
        int lo = 0, hi = _count;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (_buffer[(_head + mid) % _buffer.Length].Tick <= target)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo - 1;
    }

    private void EnsureCapacity(int needed)
    {
        if (needed <= _buffer.Length)
            return;

        int newSize = Math.Max(_buffer.Length * 2, needed);
        var newBuffer = new JournalEntry[newSize];

        for (int i = 0; i < _count; i++)
        {
            newBuffer[i] = _buffer[(_head + i) % _buffer.Length];
        }

        _buffer = newBuffer;
        _head = 0;
    }
}
