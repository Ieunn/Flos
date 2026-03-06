using System.Diagnostics;
using Flos.Diagnostics;

namespace Flos.Adapter.Godot;

/// <summary>
/// Bridges <see cref="IProfiler"/> to a Stopwatch-based implementation.
/// Godot lacks Unity's ProfilerMarker API, so we measure named spans with
/// <see cref="Stopwatch"/> and log elapsed time when <c>FLOS_PROFILING</c> is defined.
/// Each <see cref="BeginSample"/> creates a fresh Stopwatch to support nested/re-entrant calls.
/// </summary>
public sealed class GodotProfilerBridge : IProfiler
{
    public IDisposable BeginSample(string name)
    {
        var sw = Stopwatch.StartNew();
        return new StopwatchScope(name, sw);
    }

    private sealed class StopwatchScope : IDisposable
    {
        private readonly string _name;
        private readonly Stopwatch _stopwatch;

        public StopwatchScope(string name, Stopwatch stopwatch)
        {
            _name = name;
            _stopwatch = stopwatch;
        }

        public void Dispose()
        {
            _stopwatch.Stop();
#if FLOS_PROFILING
            GD.Print($"[Flos:Profile] {_name}: {_stopwatch.Elapsed.TotalMilliseconds:F3}ms");
#endif
        }
    }
}
