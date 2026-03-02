namespace Flos.Identity;

/// <summary>
/// Shared logical identity for game objects across module boundaries.
/// Long-backed, 8 bytes, O(1) equality, no GC pressure.
/// </summary>
/// <param name="Value">The underlying 64-bit identifier.</param>
public readonly record struct EntityId(long Value) : IEquatable<EntityId>
{
    /// <summary>
    /// Sentinel value representing no entity.
    /// </summary>
    public static readonly EntityId None = new(0L);

    /// <summary>
    /// Returns <see langword="true"/> if this identifier is not <see cref="None"/>.
    /// </summary>
    public bool IsValid => Value != 0L;
}
