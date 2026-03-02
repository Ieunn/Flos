using Flos.Random;

namespace Flos.Testing;

/// <summary>
/// Deterministic mock of IRandom for unit tests that need precise control over randomness.
/// Can be configured with fixed sequences of return values.
/// </summary>
public sealed class FakeRandom : IRandom
{
    private int[] _intValues = [];
    private float[] _floatValues = [];
    private int _intIndex;
    private int _floatIndex;
    private bool _cycle;

    /// <summary>
    /// Configures the sequence of values returned by <see cref="Next"/>.
    /// </summary>
    public FakeRandom Returns(params int[] values)
    {
        _intValues = values;
        _intIndex = 0;
        return this;
    }

    /// <summary>
    /// Configures the sequence of values returned by <see cref="NextFloat"/>.
    /// </summary>
    public FakeRandom ReturnsFloat(params float[] values)
    {
        _floatValues = values;
        _floatIndex = 0;
        return this;
    }

    /// <summary>
    /// When true, sequences cycle back to the beginning when exhausted.
    /// When false (default), throws <see cref="InvalidOperationException"/> when exhausted.
    /// </summary>
    public FakeRandom WithCycling(bool cycle = true)
    {
        _cycle = cycle;
        return this;
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        if (_intValues.Length == 0)
            throw new InvalidOperationException("FakeRandom: no int values configured. Call Returns() first.");

        if (_intIndex >= _intValues.Length)
        {
            if (_cycle)
                _intIndex = 0;
            else
                throw new InvalidOperationException("FakeRandom: int sequence exhausted.");
        }

        var value = _intValues[_intIndex++];
        if (value < minInclusive || value >= maxExclusive)
        {
            throw new InvalidOperationException(
                $"FakeRandom: configured value {value} is outside requested range [{minInclusive}, {maxExclusive}).");
        }
        return value;
    }

    public float NextFloat()
    {
        if (_floatValues.Length == 0)
            throw new InvalidOperationException("FakeRandom: no float values configured. Call ReturnsFloat() first.");

        if (_floatIndex >= _floatValues.Length)
        {
            if (_cycle)
                _floatIndex = 0;
            else
                throw new InvalidOperationException("FakeRandom: float sequence exhausted.");
        }

        return _floatValues[_floatIndex++];
    }

    public void SetSeed(int seed)
    {
        _intIndex = 0;
        _floatIndex = 0;
    }

    public int StateSize => 0;

    public void GetFullState(Span<byte> destination) { }

    public void RestoreFullState(ReadOnlySpan<byte> state) { }
}
