using Flos.Identity;

namespace Flos.Pattern.ECS;

/// <summary>
/// Maps between Flos EntityId (logical game identity) and ECS-native entity identifiers.
/// This is a mapping contract, not a forced abstraction over ECS entities.
/// The generic parameter TEntity represents the ECS framework's native entity type.
/// </summary>
public interface IEntityIdBridge<TEntity> where TEntity : struct
{
    /// <summary>
    /// Links a Flos EntityId to an ECS-native entity.
    /// </summary>
    void Link(EntityId flosId, TEntity ecsEntity);

    /// <summary>
    /// Removes the link for a Flos EntityId.
    /// </summary>
    void Unlink(EntityId flosId);

    /// <summary>
    /// Gets the ECS-native entity linked to a Flos EntityId.
    /// Returns false if no mapping exists.
    /// </summary>
    bool TryGetEntity(EntityId flosId, out TEntity ecsEntity);

    /// <summary>
    /// Gets the Flos EntityId linked to an ECS-native entity.
    /// Returns false if no mapping exists.
    /// </summary>
    bool TryGetFlosId(TEntity ecsEntity, out EntityId flosId);

    /// <summary>
    /// Returns the number of active links.
    /// </summary>
    int Count { get; }
}
