namespace Flos.Adapter;

/// <summary>
/// Default no-op profiler. All operations are zero-cost.
/// </summary>
public sealed class NoOpProfiler : IProfiler
{
    public static readonly NoOpProfiler Instance = new();

    public IDisposable BeginSample(string name) => NullDisposable.Instance;
}
