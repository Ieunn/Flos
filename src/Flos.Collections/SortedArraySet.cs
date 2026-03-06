using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Flos.Collections;

/// <summary>
/// Sorted array-backed set with deterministic iteration order.
/// O(log n) lookup, O(n) insert/remove. Uses ArrayPool for backing array.
/// Zero-allocation on read paths (Contains, struct enumerator).
/// </summary>
public sealed class SortedArraySet<T> : IOrderedSet<T>, IReadOnlyCollection<T>, IDisposable
    where T : IComparable<T>
{
    private const int DefaultCapacity = 4;

    private T[] _items;
    private int _count;
    private bool _disposed;

    public SortedArraySet()
    {
        _items = ArrayPool<T>.Shared.Rent(DefaultCapacity);
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SortedArraySet<T>));
    }

    /// <summary>Gets the items in sorted order as a span (zero-allocation).</summary>
    public ReadOnlySpan<T> ItemsSpan
    {
        get
        {
            ThrowIfDisposed();
            return _items.AsSpan(0, _count);
        }
    }

    public bool Add(T item)
    {
        ThrowIfDisposed();
        int index = FindIndex(item);
        if (index >= 0)
            return false;

        InsertAt(~index, item);
        return true;
    }

    public bool Remove(T item)
    {
        ThrowIfDisposed();
        int index = FindIndex(item);
        if (index < 0)
            return false;

        _count--;
        if (index < _count)
        {
            Array.Copy(_items, index + 1, _items, index, _count - index);
        }

        _items[_count] = default!;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        ThrowIfDisposed();
        return FindIndex(item) >= 0;
    }

    public void Clear()
    {
        ThrowIfDisposed();
        if (_count > 0)
        {
            Array.Clear(_items, 0, _count);
            _count = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(T item) =>
        Array.BinarySearch(_items, 0, _count, item);

    private void InsertAt(int index, T item)
    {
        if (_count == _items.Length)
            Grow();

        if (index < _count)
        {
            Array.Copy(_items, index, _items, index + 1, _count - index);
        }

        _items[index] = item;
        _count++;
    }

    private void Grow()
    {
        int newCapacity = (int)Math.Min((long)_items.Length * 2, Array.MaxLength);
        if (newCapacity <= _items.Length)
            throw new InvalidOperationException("SortedArraySet has reached maximum capacity.");

        var newItems = ArrayPool<T>.Shared.Rent(newCapacity);
        Array.Copy(_items, newItems, _count);
        ArrayPool<T>.Shared.Return(_items, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        _items = newItems;
    }

    /// <summary>
    /// Ensures the backing array can hold at least <paramref name="capacity"/> elements
    /// without further allocation.
    /// </summary>
    public void EnsureCapacity(int capacity)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        if (capacity > _items.Length)
        {
            var newItems = ArrayPool<T>.Shared.Rent(capacity);
            Array.Copy(_items, newItems, _count);
            ArrayPool<T>.Shared.Return(_items, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _items = newItems;
        }
    }

    /// <summary>
    /// Returns excess pooled memory. The array is shrunk to the smallest pool bucket
    /// that fits <see cref="Count"/>.
    /// </summary>
    public void TrimExcess()
    {
        ThrowIfDisposed();
        int target = Math.Max(_count, DefaultCapacity);

        if (_items.Length > target * 2)
        {
            var newItems = ArrayPool<T>.Shared.Rent(target);
            Array.Copy(_items, newItems, _count);
            ArrayPool<T>.Shared.Return(_items, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _items = newItems;
        }
    }

    public Enumerator GetEnumerator()
    {
        ThrowIfDisposed();
        return new(this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        ThrowIfDisposed();
        return new EnumeratorObject(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        ThrowIfDisposed();
        return new EnumeratorObject(this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ArrayPool<T>.Shared.Return(_items, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        _items = [];
        _count = 0;
    }

    public struct Enumerator
    {
        private readonly T[] _items;
        private readonly int _count;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(SortedArraySet<T> set)
        {
            _items = set._items;
            _count = set._count;
            _index = -1;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _items[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_index < _count;
    }

    private sealed class EnumeratorObject(SortedArraySet<T> set)
        : IEnumerator<T>
    {
        private int _index = -1;
        private readonly int _count = set._count;

        public T Current => set._items[_index];
        object? IEnumerator.Current => Current;
        public bool MoveNext() => ++_index < _count;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }
}
