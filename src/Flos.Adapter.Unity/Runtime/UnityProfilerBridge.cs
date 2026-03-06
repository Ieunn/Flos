using System;
using System.Collections.Generic;
using Flos.Diagnostics;
using Unity.Profiling;

namespace Flos.Adapter.Unity
{
    /// <summary>
    /// Bridges <see cref="IProfiler"/> to Unity's <see cref="ProfilerMarker"/>.
    /// Caches markers by name to avoid repeated allocations.
    /// Uses a sealed class scope to avoid boxing allocation when returned as IDisposable.
    /// </summary>
    public sealed class UnityProfilerBridge : IProfiler
    {
        private readonly Dictionary<string, ProfilerMarker> _markers = new Dictionary<string, ProfilerMarker>();

        public IDisposable BeginSample(string name)
        {
            if (!_markers.TryGetValue(name, out var marker))
            {
                marker = new ProfilerMarker(name);
                _markers[name] = marker;
            }

            marker.Begin();
            return new MarkerScope(marker);
        }

        private sealed class MarkerScope : IDisposable
        {
            private readonly ProfilerMarker _marker;

            public MarkerScope(ProfilerMarker marker)
            {
                _marker = marker;
            }

            public void Dispose()
            {
                _marker.End();
            }
        }
    }
}
