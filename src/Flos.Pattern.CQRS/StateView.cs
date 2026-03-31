using Flos.Core.Errors;
using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Resettable point-in-time snapshot holding deep-cloned state slices.
/// Types are stored in registration order for deterministic iteration.
/// List-backed storage (typical slice count &lt; 20 — linear search is cache-friendly).
/// Can be returned to a pool via <see cref="Reset"/> for reuse.
/// </summary>
internal sealed class StateView : IStateView
{
    private readonly List<SliceEntry> _entries;

    internal StateView(int capacity)
    {
        _entries = new List<SliceEntry>(capacity);
    }

    internal void Set(Type type, IStateSlice slice)
    {
        _entries.Add(new SliceEntry(type, slice));
    }

    /// <summary>
    /// Takes a slice out of the view by type, removing it from internal storage.
    /// Returns null if the type is not present.
    /// </summary>
    internal IStateSlice? Take(Type type)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == type)
            {
                var slice = _entries[i].Slice;
                _entries[i] = new SliceEntry(_entries[i].Type, null!);
                return slice;
            }
        }
        return null;
    }

    internal void Reset()
    {
        _entries.Clear();
    }

    public T Get<T>() where T : class, IStateSlice
    {
        var target = typeof(T);
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == target)
                return (T)_entries[i].Slice;
        }
        throw new FlosException(CQRSErrors.SnapshotSliceNotFound,
            $"Snapshot does not contain state slice '{typeof(T).Name}'.");
    }

    public bool TryGet<T>(out T? value) where T : class, IStateSlice
    {
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

    public IReadOnlyList<Type> RegisteredTypes => new TypeProjection(_entries);

    public IStateSlice GetSlice(Type type)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Type == type)
                return _entries[i].Slice;
        }
        throw new FlosException(CQRSErrors.SnapshotSliceNotFound,
            $"Snapshot does not contain state slice '{type.Name}'.");
    }

    private record struct SliceEntry(Type Type, IStateSlice Slice);

    /// <summary>
    /// Zero-allocation projection that presents entry types as IReadOnlyList&lt;Type&gt;.
    /// </summary>
    private readonly struct TypeProjection(List<SliceEntry> source) : IReadOnlyList<Type>
    {
        public int Count => source.Count;
        public Type this[int index] => source[index].Type;

        public IEnumerator<Type> GetEnumerator()
        {
            for (int i = 0; i < source.Count; i++)
                yield return source[i].Type;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
