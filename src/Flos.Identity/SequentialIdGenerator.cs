namespace Flos.Identity;

/// <summary>
/// Monotonically increasing ID generator seeded from a start value.
/// Main-thread only — no thread-safety required.
/// </summary>
public sealed class SequentialIdGenerator : IIdGenerator
{
    private long _next;

    /// <param name="startValue">Starting value for the ID sequence. Values &lt;= 0 default to 1.</param>
    public SequentialIdGenerator(long startValue)
    {
        _next = startValue <= 0 ? 1 : startValue;
    }

    /// <inheritdoc />
    /// <returns>A new, unique <see cref="EntityId"/> with a monotonically increasing value.</returns>
    public EntityId Next() => new(++_next);
}
