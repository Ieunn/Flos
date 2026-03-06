using System.Collections;

namespace Flos.Collections;

/// <summary>
/// Red-black tree backed ordered set with O(log n) insert, remove, and lookup.
/// Deterministic iteration order via <see cref="IComparable{T}"/> constraint.
/// Suitable for large collections where <see cref="SortedArraySet{T}"/>'s
/// O(n) insert/remove becomes a bottleneck.
/// </summary>
public sealed class OrderedSet<T> : IOrderedSet<T>, IReadOnlyCollection<T>
    where T : IComparable<T>
{
    private readonly SortedSet<T> _inner;

    public OrderedSet()
    {
        _inner = new SortedSet<T>(Comparer<T>.Default);
    }

    public int Count => _inner.Count;

    public bool Add(T item) => _inner.Add(item);

    public bool Remove(T item) => _inner.Remove(item);

    public bool Contains(T item) => _inner.Contains(item);

    public void Clear() => _inner.Clear();

    public SortedSet<T>.Enumerator GetEnumerator() => _inner.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _inner.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
}
