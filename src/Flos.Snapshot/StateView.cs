using Flos.Core.Errors;
using Flos.Core.State;

namespace Flos.Snapshot;

/// <summary>
/// Immutable point-in-time snapshot holding deep-cloned state slices.
/// </summary>
internal sealed class StateView : IStateView
{
    private readonly Dictionary<Type, IStateSlice> _slices;

    internal StateView(Dictionary<Type, IStateSlice> slices)
    {
        _slices = slices;
    }

    public T Get<T>() where T : class, IStateSlice
    {
        if (_slices.TryGetValue(typeof(T), out var slice))
        {
            return (T)slice;
        }
        throw new FlosException(SnapshotErrors.SliceNotFound,
            $"Snapshot does not contain state slice '{typeof(T).Name}'.");
    }

    internal IReadOnlyDictionary<Type, IStateSlice> Slices => _slices;
}
