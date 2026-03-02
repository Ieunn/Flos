using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using Flos.Core.Logging;

namespace Flos.Collections;

/// <summary>
/// Sorted array-backed map with deterministic iteration order.
/// O(log n) lookup, O(n) insert/remove. Uses ArrayPool for backing arrays.
/// Zero-allocation on read paths (TryGetValue, indexer, ContainsKey, struct enumerator).
/// </summary>
public sealed class SortedArrayMap<TKey, TValue> : IOrderedMap<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDisposable
    where TKey : IComparable<TKey>
{
    private const int DefaultCapacity = 4;

    private TKey[] _keys;
    private TValue[] _values;
    private int _count;
    private bool _disposed;

    public SortedArrayMap()
    {
        _keys = ArrayPool<TKey>.Shared.Rent(DefaultCapacity);
        _values = ArrayPool<TValue>.Shared.Rent(DefaultCapacity);
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SortedArrayMap<TKey, TValue>));
    }

    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ThrowIfDisposed();
            int index = FindIndex(key);
            if (index < 0)
                throw new KeyNotFoundException($"Key '{key}' not found.");
            return _values[index];
        }
        set
        {
            ThrowIfDisposed();
            int index = FindIndex(key);
            if (index >= 0)
            {
                _values[index] = value;
            }
            else
            {
                InsertAt(~index, key, value);
            }
        }
    }

    public void Add(TKey key, TValue value)
    {
        ThrowIfDisposed();
        int index = FindIndex(key);
        if (index >= 0)
        {
            CoreLog.Warn($"SortedArrayMap: key '{key}' already exists. Overwriting.");
            _values[index] = value;
            return;
        }
        InsertAt(~index, key, value);
    }

    public bool Remove(TKey key)
    {
        ThrowIfDisposed();
        int index = FindIndex(key);
        if (index < 0)
            return false;

        _count--;
        if (index < _count)
        {
            Array.Copy(_keys, index + 1, _keys, index, _count - index);
            Array.Copy(_values, index + 1, _values, index, _count - index);
        }

        _keys[_count] = default!;
        _values[_count] = default!;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        ThrowIfDisposed();
        int index = FindIndex(key);
        if (index >= 0)
        {
            value = _values[index];
            return true;
        }
        value = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
    {
        ThrowIfDisposed();
        return FindIndex(key) >= 0;
    }

    public void Clear()
    {
        ThrowIfDisposed();
        if (_count > 0)
        {
            Array.Clear(_keys, 0, _count);
            Array.Clear(_values, 0, _count);
            _count = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindIndex(TKey key) =>
        Array.BinarySearch(_keys, 0, _count, key);

    private void InsertAt(int index, TKey key, TValue value)
    {
        if (_count == _keys.Length)
            Grow();

        if (index < _count)
        {
            Array.Copy(_keys, index, _keys, index + 1, _count - index);
            Array.Copy(_values, index, _values, index + 1, _count - index);
        }

        _keys[index] = key;
        _values[index] = value;
        _count++;
    }

    private void Grow()
    {
        int newCapacity = _keys.Length * 2;
        var newKeys = ArrayPool<TKey>.Shared.Rent(newCapacity);
        var newValues = ArrayPool<TValue>.Shared.Rent(newCapacity);

        Array.Copy(_keys, newKeys, _count);
        Array.Copy(_values, newValues, _count);

        ArrayPool<TKey>.Shared.Return(_keys, clearArray: true);
        ArrayPool<TValue>.Shared.Return(_values, clearArray: true);

        _keys = newKeys;
        _values = newValues;
    }

    public Enumerator GetEnumerator()
    {
        ThrowIfDisposed();
        return new(this);
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        ThrowIfDisposed();
        return new EnumeratorObject(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        ThrowIfDisposed();
        return new EnumeratorObject(this);
    }

    /// <summary>Gets the keys in sorted order as a span (zero-allocation).</summary>
    public ReadOnlySpan<TKey> KeysSpan => _keys.AsSpan(0, _count);

    /// <summary>Gets the values in key-sorted order as a span (zero-allocation).</summary>
    public ReadOnlySpan<TValue> ValuesSpan => _values.AsSpan(0, _count);

    /// <summary>Gets the keys in sorted order.</summary>
    public IEnumerable<TKey> Keys
    {
        get
        {
            ThrowIfDisposed();
            for (int i = 0; i < _count; i++)
                yield return _keys[i];
        }
    }

    /// <summary>Gets the values in key-sorted order.</summary>
    public IEnumerable<TValue> Values
    {
        get
        {
            ThrowIfDisposed();
            for (int i = 0; i < _count; i++)
                yield return _values[i];
        }
    }

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ArrayPool<TKey>.Shared.Return(_keys, clearArray: true);
        ArrayPool<TValue>.Shared.Return(_values, clearArray: true);
        _keys = [];
        _values = [];
        _count = 0;
    }

    public struct Enumerator
    {
        private readonly TKey[] _keys;
        private readonly TValue[] _values;
        private readonly int _count;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(SortedArrayMap<TKey, TValue> map)
        {
            _keys = map._keys;
            _values = map._values;
            _count = map._count;
            _index = -1;
        }

        public KeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_keys[_index], _values[_index]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_index < _count;
    }

    private sealed class EnumeratorObject(SortedArrayMap<TKey, TValue> map)
        : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private int _index = -1;
        private readonly int _count = map._count;

        public KeyValuePair<TKey, TValue> Current => new(map._keys[_index], map._values[_index]);
        object IEnumerator.Current => Current;
        public bool MoveNext() => ++_index < _count;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }
}
