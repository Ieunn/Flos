namespace Flos.Core.Module;

/// <summary>
/// Default implementation of <see cref="IPatternRegistry"/>.
/// Uses a list-backed store for deterministic iteration order.
/// </summary>
public sealed class PatternRegistry : IPatternRegistry
{
    private readonly List<PatternId> _loaded = [];
    private readonly ReadOnlySetAdapter _readOnlyAdapter;

    public PatternRegistry()
    {
        _readOnlyAdapter = new ReadOnlySetAdapter(_loaded);
    }

    /// <inheritdoc />
    public void Register(PatternId id)
    {
        if (!_loaded.Contains(id))
            _loaded.Add(id);
    }

    /// <inheritdoc />
    public bool IsLoaded(PatternId id) => _loaded.Contains(id);

    /// <inheritdoc />
    public IReadOnlySet<PatternId> LoadedPatterns => _readOnlyAdapter;

    /// <summary>
    /// Adapts the internal list to IReadOnlySet for backward compatibility.
    /// </summary>
    private sealed class ReadOnlySetAdapter(List<PatternId> list) : IReadOnlySet<PatternId>
    {
        public int Count => list.Count;
        public bool Contains(PatternId item) => list.Contains(item);
        public bool IsProperSubsetOf(IEnumerable<PatternId> other) => other.Except(list).Any() && list.All(other.Contains);
        public bool IsProperSupersetOf(IEnumerable<PatternId> other) => other.All(list.Contains) && list.Except(other).Any();
        public bool IsSubsetOf(IEnumerable<PatternId> other) => list.All(other.Contains);
        public bool IsSupersetOf(IEnumerable<PatternId> other) => other.All(list.Contains);
        public bool Overlaps(IEnumerable<PatternId> other) => other.Any(list.Contains);
        public bool SetEquals(IEnumerable<PatternId> other) => list.Count == other.Count() && other.All(list.Contains);
        public IEnumerator<PatternId> GetEnumerator() => list.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => list.GetEnumerator();
    }
}
