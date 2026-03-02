namespace Flos.Identity;

/// <summary>
/// Generates unique <see cref="EntityId"/> values.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Returns the next unique entity identifier.
    /// </summary>
    /// <returns>A new, unique <see cref="EntityId"/>.</returns>
    EntityId Next();
}
