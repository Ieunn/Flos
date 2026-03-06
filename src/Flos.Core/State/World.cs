using System.Collections;
using System.Runtime.CompilerServices;
using Flos.Core.Errors;
using Flos.Core.Logging;

namespace Flos.Core.State;

/// <summary>
/// Default implementation of <see cref="IWorld"/>.
/// List-backed state container with deterministic iteration.
/// Typical slice count is &lt; 20 — linear search is cache-friendly and sufficient.
/// </summary>
public sealed class World : IWorld
{
    private readonly List<SliceEntry> _entries = new List<SliceEntry>();
    private readonly TypeProjection _typeProjection;
    private readonly ThreadGuard _threadGuard = new("World");

    public World()
    {
        _typeProjection = new TypeProjection(_entries);
    }

    /// <summary>
    /// Retrieves the state slice of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The slice type.</typeparam>
    /// <returns>The registered slice instance.</returns>
    /// <exception cref="FlosException">
    /// Thrown with <see cref="CoreErrors.SliceNotFound"/> when no slice of that type is registered.
    /// </exception>
    public T Get<T>() where T : class, IStateSlice
    {
        _threadGuard.Assert();
        var target = typeof(T);
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == target)
                return (T)_entries[i].Slice;
        }
        throw new FlosException(CoreErrors.SliceNotFound, $"State slice '{typeof(T).Name}' is not registered.");
    }

    /// <inheritdoc />
    public bool TryGet<T>(out T? value) where T : class, IStateSlice
    {
        _threadGuard.Assert();
        var target = typeof(T);
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == target)
            {
                value = (T)_entries[i].Slice;
                return true;
            }
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Registers an initial state slice.
    /// If a slice of the same type is already registered, it is overwritten and a warning is logged.
    /// </summary>
    /// <typeparam name="T">The slice type.</typeparam>
    /// <param name="initialState">The initial state slice instance to register.</param>
    public void Register<T>(T initialState) where T : class, IStateSlice
    {
        _threadGuard.Assert();
        var type = typeof(T);
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == type)
            {
                CoreLog.Warn($"State slice '{type.Name}' is already registered. Overwriting.");
                var old = _entries[i].Slice;
                _entries[i] = new SliceEntry(type, initialState);
                DisposeSlice(old);
                return;
            }
        }
        _entries.Add(new SliceEntry(type, initialState));
    }
    
    /// <summary>
    /// Unregister certain type of state slice.
    /// </summary>
    /// <typeparam name="T">The slice type to be unregistered.</typeparam>
    public void Unregister<T>() where T : class, IStateSlice
    {
        _threadGuard.Assert();
        var type = typeof(T);
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == type)
            {
                var old = _entries[i].Slice;
                _entries.RemoveAt(i);
                DisposeSlice(old);
                return;
            }
        }
    }

    /// <summary>
    /// Retrieves a state slice by its runtime type key.
    /// </summary>
    /// <param name="type">The type key of the slice.</param>
    /// <returns>The registered slice instance.</returns>
    /// <exception cref="FlosException">
    /// Thrown with <see cref="CoreErrors.SliceNotFound"/> when no slice of that type is registered.
    /// </exception>
    public IStateSlice GetSlice(Type type)
    {
        _threadGuard.Assert();
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == type)
                return _entries[i].Slice;
        }
        throw new FlosException(CoreErrors.SliceNotFound, $"State slice '{type.Name}' is not registered.");
    }

    /// <summary>
    /// Replaces a previously registered slice (used by snapshot restore).
    /// </summary>
    /// <param name="type">The type key of the slice to replace.</param>
    /// <param name="slice">The new slice instance.</param>
    /// <exception cref="FlosException">
    /// Thrown with <see cref="CoreErrors.SliceNotFound"/> if no slice of that type is registered.
    /// </exception>
    public void SetSlice(Type type, IStateSlice slice)
    {
        _threadGuard.Assert();
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == type)
            {
                var old = _entries[i].Slice;
                _entries[i] = new SliceEntry(type, slice);
                DisposeSlice(old);
                return;
            }
        }
        throw new FlosException(CoreErrors.SliceNotFound, $"State slice '{type.Name}' is not registered. Use Register<T> first.");
    }

    /// <summary>
    /// Types of all registered slices, in registration order.
    /// </summary>
    public IReadOnlyList<Type> RegisteredTypes => _typeProjection;

    private static void DisposeSlice(IStateSlice slice)
    {
        if (slice is IDisposable disposable)
            disposable.Dispose();
    }

    internal record struct SliceEntry(Type Type, IStateSlice Slice);

    /// <summary>
    /// Zero-allocation read-only projection that presents
    /// <see cref="SliceEntry.Type"/> as an <see cref="IReadOnlyList{Type}"/>.
    /// Backed directly by the entries list.
    /// Boxing occurs when explicitly use IEnumerable.
    /// </summary>
    internal sealed class TypeProjection : IReadOnlyList<Type>
    {
        private readonly List<SliceEntry> _source;

        internal TypeProjection(List<SliceEntry> source) => _source = source;

        public int Count => _source.Count;
        public Type this[int index] => _source[index].Type;

        public Enumerator GetEnumerator() => new(_source);

        IEnumerator<Type> IEnumerable<Type>.GetEnumerator() => new EnumeratorObject(_source);
        IEnumerator IEnumerable.GetEnumerator() => new EnumeratorObject(_source);

        public struct Enumerator
        {
            private readonly List<SliceEntry> _source;
            private readonly int _count;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(List<SliceEntry> source)
            {
                _source = source;
                _count = source.Count;
                _index = -1;
            }

            public Type Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _source[_index].Type;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _count;
        }

        private sealed class EnumeratorObject(List<SliceEntry> source) : IEnumerator<Type>
        {
            private int _index = -1;
            private readonly int _count = source.Count;

            public Type Current => source[_index].Type;
            object IEnumerator.Current => Current;
            public bool MoveNext() => ++_index < _count;
            public void Reset() => _index = -1;
            public void Dispose() { }
        }
    }
}