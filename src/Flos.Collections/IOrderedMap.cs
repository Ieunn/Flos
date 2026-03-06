namespace Flos.Collections;

/// <summary>
/// A key-value map with deterministic (sorted by key) iteration order.
/// Replaces Dictionary/HashSet in IStateSlice fields for determinism (FLOS005).
/// </summary>
public interface IOrderedMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    int Count { get; }
    TValue this[TKey key] { get; set; }
    void Add(TKey key, TValue value);
    bool Remove(TKey key);
    bool TryGetValue(TKey key, out TValue value);
    bool ContainsKey(TKey key);
    void Clear();
}
