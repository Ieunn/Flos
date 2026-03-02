namespace Flos.Testing;

/// <summary>
/// Thrown when a deterministic replay produces different results.
/// </summary>
public sealed class DeterminismException : Exception
{
    public DeterminismException(string message) : base(message) { }
}
