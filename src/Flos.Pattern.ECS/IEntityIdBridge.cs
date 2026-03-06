namespace Flos.Pattern.ECS;

/// <summary>
/// Maps between a logical game identity and ECS-native entity identifiers.
/// This is a mapping contract, not a forced abstraction over ECS entities.
/// The generic parameter TId represents the logical identity type (e.g., EntityId).
/// The generic parameter TEntity represents the ECS framework's native entity type.
/// </summary>
public interface IEntityIdBridge<TId, TEntity>
    where TId : notnull
    where TEntity : struct
{
    /// <summary>
    /// Links a logical id to an ECS-native entity.
    /// </summary>
    void Link(TId flosId, TEntity ecsEntity);

    /// <summary>
    /// Removes the link for a logical id.
    /// </summary>
    void Unlink(TId flosId);

    /// <summary>
    /// Gets the ECS-native entity linked to a logical id.
    /// Returns false if no mapping exists.
    /// </summary>
    bool TryGetEntity(TId flosId, out TEntity ecsEntity);

    /// <summary>
    /// Gets the logical id linked to an ECS-native entity.
    /// Returns false if no mapping exists.
    /// </summary>
    bool TryGetFlosId(TEntity ecsEntity, out TId flosId);

    /// <summary>
    /// Returns the number of active links.
    /// </summary>
    int Count { get; }
}
