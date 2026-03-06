using System.Runtime.CompilerServices;
using Flos.Core.Errors;
using Flos.Core.Logging;
using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Lazily-cloning read-only view over a live <see cref="IWorld"/>.
/// On first access of each slice type, deep-clones if the slice implements
/// <see cref="IDeepCloneable{T}"/>; otherwise returns the live reference with a warning.
/// Pooled and reused across commands to avoid per-Send allocations.
/// </summary>
internal sealed class ReadOnlyWorldView : IStateReader
{
    private IWorld _world = null!;
    private ApplierFaultMode _faultMode;
    private readonly Dictionary<Type, IStateSlice> _cloneCache = new Dictionary<Type, IStateSlice>();

    internal void Bind(IWorld world, ApplierFaultMode faultMode = ApplierFaultMode.Strict)
    {
        _world = world;
        _faultMode = faultMode;
    }

    internal void Reset()
    {
        _cloneCache.Clear();
        _world = null!;
    }

    public T Get<T>() where T : class, IStateSlice
    {
        var key = typeof(T);

        if (_cloneCache.TryGetValue(key, out var cached))
            return (T)cached;

        var live = _world.Get<T>();
        var cloned = ResolveSlice(live);
        _cloneCache[key] = cloned;
        return (T)cloned;
    }

    public bool TryGet<T>(out T? value) where T : class, IStateSlice
    {
        var key = typeof(T);

        if (_cloneCache.TryGetValue(key, out var cached))
        {
            value = (T)cached;
            return true;
        }

        if (!_world.TryGet<T>(out var live))
        {
            value = null;
            return false;
        }

        var cloned = ResolveSlice(live!);
        _cloneCache[key] = cloned;
        value = (T)cloned;
        return true;
    }

    public IStateSlice GetSlice(Type type)
    {
        if (_cloneCache.TryGetValue(type, out var cached))
            return cached;

        var live = _world.GetSlice(type);
        var cloned = ResolveSlice(live);
        _cloneCache[type] = cloned;
        return cloned;
    }

    public IReadOnlyList<Type> RegisteredTypes => _world.RegisteredTypes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IStateSlice ResolveSlice(IStateSlice slice)
    {
        if (slice is IDeepCloneable<IStateSlice> cloneable)
            return cloneable.DeepClone();

        return _faultMode switch
        {
            ApplierFaultMode.Tolerant => FallbackLiveReference(slice),
            _ => throw new FlosException(
                CQRSErrors.SliceNotCloneable,
                $"State slice '{slice.GetType().Name}' does not implement IDeepCloneable<T>. (FaultMode={_faultMode}).")
        };
    }

    private static IStateSlice FallbackLiveReference(IStateSlice slice)
    {
        CoreLog.Warn($"State slice '{slice.GetType().Name}' does not implement IDeepCloneable<T>. " +
            "Handler receives a live reference — mutations will bypass CQRS pipeline.");
        return slice;
    }
}
