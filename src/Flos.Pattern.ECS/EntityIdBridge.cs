using Flos.Identity;

namespace Flos.Pattern.ECS;

/// <summary>
/// Default dictionary-backed identity bridge.
/// Thread-safe: uses a lock for concurrent access from ECS worker threads.
/// </summary>
public sealed class EntityIdBridge<TEntity> : IEntityIdBridge<TEntity> where TEntity : struct
{
    private readonly Dictionary<EntityId, TEntity> _flosToEcs = [];
    private readonly Dictionary<TEntity, EntityId> _ecsToFlos = [];
    private readonly object _lock = new();

    public int Count
    {
        get { lock (_lock) return _flosToEcs.Count; }
    }

    public void Link(EntityId flosId, TEntity ecsEntity)
    {
        lock (_lock)
        {
            _flosToEcs[flosId] = ecsEntity;
            _ecsToFlos[ecsEntity] = flosId;
        }
    }

    public void Unlink(EntityId flosId)
    {
        lock (_lock)
        {
            if (_flosToEcs.Remove(flosId, out var ecsEntity))
            {
                _ecsToFlos.Remove(ecsEntity);
            }
        }
    }

    public bool TryGetEntity(EntityId flosId, out TEntity ecsEntity)
    {
        lock (_lock)
        {
            return _flosToEcs.TryGetValue(flosId, out ecsEntity);
        }
    }

    public bool TryGetFlosId(TEntity ecsEntity, out EntityId flosId)
    {
        lock (_lock)
        {
            return _ecsToFlos.TryGetValue(ecsEntity, out flosId);
        }
    }
}
