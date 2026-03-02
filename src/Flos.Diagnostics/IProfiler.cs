namespace Flos.Diagnostics;

/// <summary>
/// Adapter contract for per-frame profiling.
/// Bridges to engine-native profilers (Unity ProfilerMarker, Godot Performance, etc.).
/// Hot-path spans should use <see cref="ProfilerExtensions"/> which are
/// compile-stripped via [Conditional("FLOS_PROFILING")].
/// </summary>
public interface IProfiler
{
    IDisposable BeginSample(string name);
}
