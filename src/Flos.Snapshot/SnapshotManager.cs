using Flos.Core.Errors;
using Flos.Core.Logging;
using Flos.Core.State;

namespace Flos.Snapshot;

/// <summary>
/// Default implementation of <see cref="ISnapshotManager"/>. AOT-safe: no runtime reflection.
/// Slice types can be pre-registered via <see cref="RegisterSlice{T}"/> for optimal performance,
/// or auto-discovered from <see cref="IWorld.RegisteredTypes"/> at capture time using interface casts.
/// </summary>
public sealed class SnapshotManager : ISnapshotManager
{
    private readonly Dictionary<Type, SliceAccessors> _registered = [];

    /// <inheritdoc />
    public void RegisterSlice<T>() where T : class, IStateSlice, IDeepCloneable<T>
    {
        _registered[typeof(T)] = new SliceAccessors(
            world => world.Get<T>(),
            slice => ((T)slice).DeepClone());
    }

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown with <see cref="SnapshotErrors.NotCloneable"/> when a slice does not implement <see cref="IDeepCloneable{T}"/>.</exception>
    public IStateView Capture(IWorld world)
    {
        var types = world.RegisteredTypes;
        var slices = new Dictionary<Type, IStateSlice>(types.Count);

        for (int i = 0; i < types.Count; i++)
        {
            var type = types[i];

            if (_registered.TryGetValue(type, out var accessor))
            {
                var slice = accessor.GetSlice(world);
                slices[type] = accessor.CloneSlice(slice);
            }
            else
            {
                var slice = world.GetSlice(type);
                slices[type] = TryClone(slice, type);
            }
        }

        return new StateView(slices);
    }

    /// <inheritdoc />
    public void RestoreTo(IWorld world, IStateView snapshot)
    {
        var stateView = (StateView)snapshot;

        foreach (var (type, slice) in stateView.Slices)
        {
            IStateSlice cloned;

            if (_registered.TryGetValue(type, out var accessor))
            {
                cloned = accessor.CloneSlice(slice);
            }
            else
            {
                cloned = TryClone(slice, type);
            }

            world.SetSlice(type, cloned);
        }
    }

    /// <summary>
    /// Attempts to clone a slice by checking if it implements IDeepCloneable via its concrete type.
    /// If the slice does not implement IDeepCloneable, logs an error and returns the original reference.
    /// No reflection — uses interface dispatch on the well-known DeepClone() method.
    /// </summary>
    private static IStateSlice TryClone(IStateSlice slice, Type type)
    {
        if (slice is IDeepCloneable<IStateSlice> cloneable)
        {
            return cloneable.DeepClone();
        }

        CoreLog.Error(
            $"State slice '{type.Name}' does not implement IDeepCloneable<{type.Name}>. " +
            $"Snapshot will contain a shared reference instead of a deep copy.");
        return slice;
    }

    private readonly record struct SliceAccessors(
        Func<IWorld, IStateSlice> GetSlice,
        Func<IStateSlice, IStateSlice> CloneSlice);
}
