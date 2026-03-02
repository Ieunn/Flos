namespace Flos.Random;

/// <summary>
/// Deterministic random number generator contract. All game logic must use this instead of <see cref="System.Random"/>.
/// </summary>
public interface IRandom
{
    /// <summary>
    /// Returns a random integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
    /// </summary>
    /// <param name="minInclusive">The inclusive lower bound of the random number.</param>
    /// <param name="maxExclusive">The exclusive upper bound of the random number.</param>
    /// <returns>A random integer in the specified range.</returns>
    int Next(int minInclusive, int maxExclusive);

    /// <summary>
    /// Returns a random float in [0, 1).
    /// </summary>
    /// <returns>A random float value greater than or equal to 0.0 and less than 1.0.</returns>
    float NextFloat();

    /// <summary>
    /// Resets the generator to a deterministic state derived from the given seed.
    /// </summary>
    /// <param name="seed">The seed value used to initialize the generator state.</param>
    void SetSeed(int seed);

    /// <summary>
    /// Size in bytes of the full internal state (for snapshot/restore).
    /// </summary>
    int StateSize { get; }

    /// <summary>
    /// Copies the full internal state into the destination span.
    /// </summary>
    /// <param name="destination">The span to receive the internal state bytes.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is smaller than <see cref="StateSize"/>.</exception>
    void GetFullState(Span<byte> destination);

    /// <summary>
    /// Restores previously captured internal state.
    /// </summary>
    /// <param name="state">The span containing the previously captured state bytes.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="state"/> is smaller than <see cref="StateSize"/>.</exception>
    void RestoreFullState(ReadOnlySpan<byte> state);
}
