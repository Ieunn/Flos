namespace Flos.Adapter;

public enum TraceLevel { Debug, Info, Warning, Error }

/// <summary>
/// Adapter contract for distributed/structured tracing.
/// Bridges to engine-native tools (Unity Profiler, Godot Performance, OpenTelemetry, etc.).
/// </summary>
public interface ITracer
{
    IDisposable BeginSpan(string name);
    void Log(TraceLevel level, string message);
}
