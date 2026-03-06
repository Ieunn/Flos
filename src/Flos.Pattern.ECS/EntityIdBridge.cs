namespace Flos.Pattern.ECS;

/// <summary>
/// Default dictionary-backed identity bridge.
/// Thread-safe: uses a lock for concurrent access from ECS worker threads.
/// </summary>
public sealed class EntityIdBridge<TId, TEntity> : IEntityIdBridge<TId, TEntity>
    where TId : notnull
    where TEntity : struct
{
    private readonly Dictionary<TId, TEntity> _flosToEcs = new Dictionary<TId, TEntity>();
    private readonly Dictionary<TEntity, TId> _ecsToFlos = new Dictionary<TEntity, TId>();
    private readonly object _lock = new();

    public int Count
    {
        get { lock (_lock) return _flosToEcs.Count; }
    }

    public void Link(TId flosId, TEntity ecsEntity)
    {
        lock (_lock)
        {
            if (_flosToEcs.TryGetValue(flosId, out var oldEcs))
                _ecsToFlos.Remove(oldEcs);

            if (_ecsToFlos.TryGetValue(ecsEntity, out var oldFlos))
                _flosToEcs.Remove(oldFlos);

            _flosToEcs[flosId] = ecsEntity;
            _ecsToFlos[ecsEntity] = flosId;
        }
    }

    public void Unlink(TId flosId)
    {
        lock (_lock)
        {
            if (_flosToEcs.Remove(flosId, out var ecsEntity))
            {
                _ecsToFlos.Remove(ecsEntity);
            }
        }
    }

    public bool TryGetEntity(TId flosId, out TEntity ecsEntity)
    {
        lock (_lock)
        {
            return _flosToEcs.TryGetValue(flosId, out ecsEntity);
        }
    }

    public bool TryGetFlosId(TEntity ecsEntity, out TId flosId)
    {
        lock (_lock)
        {
            return _ecsToFlos.TryGetValue(ecsEntity, out flosId!);
        }
    }
}
