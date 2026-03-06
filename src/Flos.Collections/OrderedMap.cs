using System.Collections;

namespace Flos.Collections;

/// <summary>
/// Red-black tree backed ordered map with O(log n) insert, remove, and lookup.
/// Deterministic iteration order via <see cref="IComparable{T}"/> key constraint.
/// Suitable for large collections where <see cref="SortedArrayMap{TKey,TValue}"/>'s
/// O(n) insert/remove becomes a bottleneck.
/// </summary>
public sealed class OrderedMap<TKey, TValue> : IOrderedMap<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : IComparable<TKey>
{
    private readonly SortedDictionary<TKey, TValue> _inner;

    public OrderedMap()
    {
        _inner = new SortedDictionary<TKey, TValue>(Comparer<TKey>.Default);
    }

    public OrderedMap(int capacity)
    {
        // SortedDictionary doesn't support capacity hint; ignore gracefully
        _inner = new SortedDictionary<TKey, TValue>(Comparer<TKey>.Default);
    }

    public int Count => _inner.Count;

    public TValue this[TKey key]
    {
        get => _inner[key];
        set => _inner[key] = value;
    }

    public void Add(TKey key, TValue value) => _inner.Add(key, value);

    public bool Remove(TKey key) => _inner.Remove(key);

    public bool TryGetValue(TKey key, out TValue value) => _inner.TryGetValue(key, out value!);

    public bool ContainsKey(TKey key) => _inner.ContainsKey(key);

    public void Clear() => _inner.Clear();

    public IEnumerable<TKey> Keys => _inner.Keys;

    public IEnumerable<TValue> Values => _inner.Values;

    public SortedDictionary<TKey, TValue>.Enumerator GetEnumerator() => _inner.GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        => _inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
}
