namespace Flos.Testing;

/// <summary>
/// Thrown when a test assertion in GameTestHarness fails.
/// </summary>
public sealed class TestAssertionException : Exception
{
    public TestAssertionException(string message) : base(message) { }
}
