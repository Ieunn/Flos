namespace Flos.Adapter;

/// <summary>
/// Shared zero-allocation disposable that does nothing.
/// </summary>
internal sealed class NullDisposable : IDisposable
{
    public static readonly NullDisposable Instance = new();

    public void Dispose() { }
}
