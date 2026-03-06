namespace Flos.Collections;

/// <summary>
/// A set with deterministic (sorted) iteration order.
/// Replaces HashSet in IStateSlice fields for determinism (FLOS005).
/// </summary>
public interface IOrderedSet<T> : IEnumerable<T>
    where T : IComparable<T>
{
    int Count { get; }
    bool Add(T item);
    bool Remove(T item);
    bool Contains(T item);
    void Clear();
}
