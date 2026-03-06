namespace Flos.Identity;

/// <summary>
/// Monotonically increasing ID generator.
/// Main-thread only — no thread-safety required.
/// </summary>
public sealed class SequentialIdGenerator : IIdGenerator
{
    private long _next;

    /// <param name="startValue">
    /// The first EntityId value that <see cref="Next"/> will return.
    /// Values &lt;= 0 default to 1 (since <see cref="EntityId.None"/> has value 0).
    /// </param>
    public SequentialIdGenerator(long startValue)
    {
        _next = (startValue <= 0 ? 1 : startValue) - 1;
    }

    /// <inheritdoc />
    /// <returns>A new, unique <see cref="EntityId"/> with a monotonically increasing value.</returns>
    /// <exception cref="OverflowException">Thrown if the generator exceeds <see cref="long.MaxValue"/>.</exception>
    public EntityId Next()
    {
        long value = checked(++_next);
        if (value == 0L)
            throw new OverflowException("SequentialIdGenerator overflowed past long.MaxValue and produced EntityId.None.");
        return new EntityId(value);
    }

    /// <inheritdoc />
    public long GetState() => _next;

    /// <inheritdoc />
    public void RestoreState(long state) => _next = state;
}
