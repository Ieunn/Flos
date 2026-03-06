using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Flos.Collections;

public sealed class SortedArrayMap<TKey, TValue>
    : IOrderedMap<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDisposable
    where TKey : IComparable<TKey>
{
    private const int DefaultCapacity = 4;

    private TKey[] _keys;
    private TValue[] _values;
    private int _count;
    private bool _disposed;

    private int _keyCapacity;
    private int _valueCapacity;

    public SortedArrayMap() : this(DefaultCapacity) { }

    public SortedArrayMap(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        capacity = Math.Max(capacity, DefaultCapacity);

        _keys = ArrayPool<TKey>.Shared.Rent(capacity);
        _values = ArrayPool<TValue>.Shared.Rent(capacity);

        _keyCapacity = _keys.Length;
        _valueCapacity = _values.Length;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    /// <summary>
    /// The effective capacity before any growth is needed.
    /// Equal to the smaller of the two independent capacities.
    /// </summary>
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Math.Min(_keyCapacity, _valueCapacity);
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
            if (index < 0) throw new KeyNotFoundException($"Key '{key}' not found.");
            return _values[index];
        }
        set
        {
            ThrowIfDisposed();
            int index = FindIndex(key);
            if (index >= 0)
                _values[index] = value;
            else
                InsertAt(~index, key, value);
        }
    }

    public void Add(TKey key, TValue value)
    {
        ThrowIfDisposed();
        int index = FindIndex(key);
        if (index >= 0)
            throw new ArgumentException($"An element with the key '{key}' already exists.");
        InsertAt(~index, key, value);
    }

    public bool Remove(TKey key)
    {
        ThrowIfDisposed();
        int index = FindIndex(key);
        if (index < 0) return false;

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
        if (index >= 0) { value = _values[index]; return true; }
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
        if (_count == _keyCapacity)
            GrowKeys();
        if (_count == _valueCapacity)
            GrowValues();

        if (index < _count)
        {
            Array.Copy(_keys, index, _keys, index + 1, _count - index);
            Array.Copy(_values, index, _values, index + 1, _count - index);
        }

        _keys[index] = key;
        _values[index] = value;
        _count++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowKeys()
    {
        int newCapacity = ComputeNewCapacity(_keyCapacity);

        var newKeys = ArrayPool<TKey>.Shared.Rent(newCapacity);
        Array.Copy(_keys, newKeys, _count);
        ArrayPool<TKey>.Shared.Return(_keys,
            clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TKey>());

        _keys = newKeys;
        _keyCapacity = newKeys.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowValues()
    {
        int newCapacity = ComputeNewCapacity(_valueCapacity);

        var newValues = ArrayPool<TValue>.Shared.Rent(newCapacity);
        Array.Copy(_values, newValues, _count);
        ArrayPool<TValue>.Shared.Return(_values,
            clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TValue>());

        _values = newValues;
        _valueCapacity = newValues.Length;
    }

    /// <summary>
    /// Shared growth policy: double, clamped to Array.MaxLength.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeNewCapacity(int currentCapacity)
    {
        int newCapacity = (int)Math.Min((long)currentCapacity * 2, Array.MaxLength);
        if (newCapacity <= currentCapacity)
            throw new InvalidOperationException("SortedArrayMap has reached maximum capacity.");
        return newCapacity;
    }

    /// <summary>
    /// Ensures both backing arrays can hold at least <paramref name="capacity"/> elements
    /// without further allocation.
    /// </summary>
    public void EnsureCapacity(int capacity)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        if (capacity > _keyCapacity)
        {
            var newKeys = ArrayPool<TKey>.Shared.Rent(capacity);
            Array.Copy(_keys, newKeys, _count);
            ArrayPool<TKey>.Shared.Return(_keys,
                clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TKey>());
            _keys = newKeys;
            _keyCapacity = newKeys.Length;
        }

        if (capacity > _valueCapacity)
        {
            var newValues = ArrayPool<TValue>.Shared.Rent(capacity);
            Array.Copy(_values, newValues, _count);
            ArrayPool<TValue>.Shared.Return(_values,
                clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TValue>());
            _values = newValues;
            _valueCapacity = newValues.Length;
        }
    }


    /// <summary>
    /// Returns excess pooled memory. Both arrays are shrunk independently
    /// to the smallest pool bucket that fits <see cref="Count"/>.
    /// </summary>
    public void TrimExcess()
    {
        ThrowIfDisposed();
        int target = Math.Max(_count, DefaultCapacity);

        if (_keyCapacity > target * 2)
        {
            var newKeys = ArrayPool<TKey>.Shared.Rent(target);
            Array.Copy(_keys, newKeys, _count);
            ArrayPool<TKey>.Shared.Return(_keys,
                clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TKey>());
            _keys = newKeys;
            _keyCapacity = newKeys.Length;
        }

        if (_valueCapacity > target * 2)
        {
            var newValues = ArrayPool<TValue>.Shared.Rent(target);
            Array.Copy(_values, newValues, _count);
            ArrayPool<TValue>.Shared.Return(_values,
                clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TValue>());
            _values = newValues;
            _valueCapacity = newValues.Length;
        }
    }

    public ReadOnlySpan<TKey> KeysSpan => _keys.AsSpan(0, _count);
    public ReadOnlySpan<TValue> ValuesSpan => _values.AsSpan(0, _count);

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

    public IEnumerable<TKey> Keys
    {
        get { ThrowIfDisposed(); return new KeyCollection(this); }
    }

    public IEnumerable<TValue> Values
    {
        get { ThrowIfDisposed(); return new ValueCollection(this); }
    }

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ArrayPool<TKey>.Shared.Return(_keys,
            clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TKey>());
        ArrayPool<TValue>.Shared.Return(_values,
            clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<TValue>());
        _keys = [];
        _values = [];
        _keyCapacity = 0;
        _valueCapacity = 0;
        _count = 0;
    }

    private sealed class KeyCollection(SortedArrayMap<TKey, TValue> map) : IEnumerable<TKey>
    {
        public IEnumerator<TKey> GetEnumerator() => new KeyEnumerator(map);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ValueCollection(SortedArrayMap<TKey, TValue> map) : IEnumerable<TValue>
    {
        public IEnumerator<TValue> GetEnumerator() => new ValueEnumerator(map);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class KeyEnumerator(SortedArrayMap<TKey, TValue> map) : IEnumerator<TKey>
    {
        private int _index = -1;
        private readonly int _count = map._count;
        public TKey Current => map._keys[_index];
        object IEnumerator.Current => Current!;
        public bool MoveNext() => ++_index < _count;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }

    private sealed class ValueEnumerator(SortedArrayMap<TKey, TValue> map) : IEnumerator<TValue>
    {
        private int _index = -1;
        private readonly int _count = map._count;
        public TValue Current => map._values[_index];
        object? IEnumerator.Current => Current;
        public bool MoveNext() => ++_index < _count;
        public void Reset() => _index = -1;
        public void Dispose() { }
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