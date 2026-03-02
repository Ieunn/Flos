using System.Diagnostics;
using Flos.Diagnostics;

namespace Flos.Adapter.Godot;

/// <summary>
/// Bridges <see cref="IProfiler"/> to a Stopwatch-based implementation.
/// Godot lacks Unity's ProfilerMarker API, so we measure named spans with
/// <see cref="Stopwatch"/> and log elapsed time when <c>FLOS_PROFILING</c> is defined.
/// Caches stopwatch instances per name to reduce allocations.
/// </summary>
public sealed class GodotProfilerBridge : IProfiler
{
    private readonly Dictionary<string, Stopwatch> _stopwatches = new();

    public IDisposable BeginSample(string name)
    {
        if (!_stopwatches.TryGetValue(name, out var sw))
        {
            sw = new Stopwatch();
            _stopwatches[name] = sw;
        }

        sw.Restart();
        return new StopwatchScope(name, sw);
    }

    private readonly struct StopwatchScope : IDisposable
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
