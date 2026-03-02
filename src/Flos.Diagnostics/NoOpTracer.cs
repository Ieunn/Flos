namespace Flos.Diagnostics;

/// <summary>
/// Default no-op tracer. All operations are zero-cost.
/// </summary>
public sealed class NoOpTracer : ITracer
{
    public static readonly NoOpTracer Instance = new();

    public IDisposable BeginSpan(string name) => NullDisposable.Instance;
    public void Log(TraceLevel level, string message) { }
}
