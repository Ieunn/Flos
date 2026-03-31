using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Default implementation of <see cref="ISnapshots"/>. AOT-safe: no runtime reflection.
/// Only slices explicitly registered via <see cref="RegisterSlice{T}"/> are captured.
/// Unregistered slices are silently skipped — this allows non-cloneable slices
/// (e.g., config, ECS world wrappers) to coexist in the world without breaking snapshots.
/// <para>
/// StateView objects are pooled internally to minimize allocations on hot paths.
/// Call <see cref="Return"/> to recycle a snapshot when it is no longer needed.
/// </para>
/// </summary>
public sealed class Snapshots : ISnapshots
{
    private readonly Dictionary<Type, SliceAccessors> _registered = new Dictionary<Type, SliceAccessors>();
    private readonly List<Type> _registrationOrder = new List<Type>();
    private readonly Stack<StateView> _viewPool = new(2);

    /// <inheritdoc />
    public void RegisterSlice<T>() where T : class, IStateSlice, IDeepCloneable<T>
    {
        var type = typeof(T);
        if (!_registered.ContainsKey(type))
        {
            _registrationOrder.Add(type);
        }
        _registered[type] = new SliceAccessors(
            world => world.Get<T>(),
            slice => ((T)slice).DeepClone());
    }

    /// <inheritdoc />
    public IStateView Capture(IWorld world)
    {
        var view = RentView();

        try
        {
            for (int i = 0; i < _registrationOrder.Count; i++)
            {
                var type = _registrationOrder[i];
                var accessor = _registered[type];
                var slice = accessor.GetSlice(world);
                view.Set(type, accessor.CloneSlice(slice));
            }
        }
        catch
        {
            view.Reset();
            _viewPool.Push(view);
            throw;
        }

        return view;
    }

    /// <inheritdoc />
    public void RestoreTo(IWorld world, IStateView snapshot)
    {
        var types = snapshot.RegisteredTypes;

        for (int i = 0; i < types.Count; i++)
        {
            var type = types[i];

            if (!_registered.TryGetValue(type, out var accessor))
                continue;

            var slice = snapshot.GetSlice(type);
            var cloned = accessor.CloneSlice(slice);
            world.SetSlice(type, cloned);
        }
    }

    /// <inheritdoc />
    public void RestoreAndConsume(IWorld world, IStateView snapshot)
    {
        if (snapshot is StateView view)
        {
            var types = view.RegisteredTypes;
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];

                if (!_registered.ContainsKey(type))
                    continue;

                var slice = view.Take(type);
                if (slice is not null)
                    world.SetSlice(type, slice);
            }
            view.Reset();
            _viewPool.Push(view);
        }
        else
        {
            RestoreTo(world, snapshot);
        }
    }

    /// <inheritdoc />
    public void Return(IStateView snapshot)
    {
        if (snapshot is StateView view)
        {
            view.Reset();
            _viewPool.Push(view);
        }
    }

    private StateView RentView()
    {
        if (_viewPool.TryPop(out var view))
        {
            view.Reset();
            return view;
        }
        return new StateView(_registrationOrder.Count);
    }

    private readonly record struct SliceAccessors(
        Func<IWorld, IStateSlice> GetSlice,
        Func<IStateSlice, IStateSlice> CloneSlice);
}
