using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Flos.Random;

/// <summary>
/// Xoshiro256** PRNG. State: 4 × ulong (32 bytes).
/// Seed derivation via SplitMix64.
/// </summary>
public sealed class Xoshiro256StarStarRandom : IRandom
{
    private ulong _s0, _s1, _s2, _s3;

    /// <summary>
    /// Returns 32 (4 x 8 bytes).
    /// </summary>
    public int StateSize => 32;

    /// <inheritdoc />
    /// <param name="seed">The seed value used to derive the full 256-bit state via SplitMix64.</param>
    public void SetSeed(int seed)
    {
        ulong sm = (ulong)seed;
        _s0 = SplitMix64(ref sm);
        _s1 = SplitMix64(ref sm);
        _s2 = SplitMix64(ref sm);
        _s3 = SplitMix64(ref sm);
    }

    /// <inheritdoc />
    /// <param name="minInclusive">The inclusive lower bound of the random number.</param>
    /// <param name="maxExclusive">The exclusive upper bound of the random number.</param>
    /// <returns>A random integer in the specified range.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minInclusive"/> is greater than or equal to <paramref name="maxExclusive"/>.</exception>
    public int Next(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentException("minInclusive must be less than maxExclusive.");

        ulong range = (ulong)(maxExclusive - minInclusive);
        ulong x = NextUlong();
        ulong hi = Math.BigMul(x, range, out ulong lo);
        if (lo < range)
        {
            ulong threshold = (0UL - range) % range;
            while (lo < threshold)
            {
                x = NextUlong();
                hi = Math.BigMul(x, range, out lo);
            }
        }
        return minInclusive + (int)hi;
    }

    /// <inheritdoc />
    /// <returns>A random float value greater than or equal to 0.0 and less than 1.0.</returns>
    public float NextFloat()
    {
        return (NextUlong() >> 40) * (1.0f / (1L << 24));
    }

    /// <inheritdoc />
    /// <param name="destination">The span to receive the 32-byte internal state.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is smaller than 32 bytes.</exception>
    public void GetFullState(Span<byte> destination)
    {
        if (destination.Length < 32)
            throw new ArgumentException("Destination buffer must be at least 32 bytes.", nameof(destination));

        BinaryPrimitives.WriteUInt64LittleEndian(destination, _s0);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], _s1);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[16..], _s2);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[24..], _s3);
    }

    /// <inheritdoc />
    /// <param name="state">The span containing the previously captured 32-byte state.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="state"/> is smaller than 32 bytes.</exception>
    public void RestoreFullState(ReadOnlySpan<byte> state)
    {
        if (state.Length < 32)
            throw new ArgumentException("State buffer must be at least 32 bytes.", nameof(state));

        _s0 = BinaryPrimitives.ReadUInt64LittleEndian(state);
        _s1 = BinaryPrimitives.ReadUInt64LittleEndian(state[8..]);
        _s2 = BinaryPrimitives.ReadUInt64LittleEndian(state[16..]);
        _s3 = BinaryPrimitives.ReadUInt64LittleEndian(state[24..]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong NextUlong()
    {
        ulong result = RotateLeft(_s1 * 5, 7) * 9;
        ulong t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;

        _s2 ^= t;
        _s3 = RotateLeft(_s3, 45);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong RotateLeft(ulong x, int k) => (x << k) | (x >> (64 - k));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong SplitMix64(ref ulong state)
    {
        ulong z = state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
