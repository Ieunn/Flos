# Flos.Diagnostics

Adapter contracts for tracing and profiling. Bridges to engine-native tools (Unity `ProfilerMarker`, Godot `Performance`). Ships with no-op defaults.

## Installation

```xml
<PackageReference Include="Flos.Diagnostics" />
```

## Quick Usage

```csharp
// Resolve from scope (no-op by default):
var profiler = scope.Resolve<IProfiler>();
using (profiler.BeginSample("AI Update"))
{
    // profiled code
}

var tracer = scope.Resolve<ITracer>();
using (tracer.BeginSpan("Network Sync"))
{
    tracer.Log(TraceLevel.Info, "Syncing...");
}
```

## API Overview

| Type | Description |
|------|-------------|
| `ITracer` | Tracing contract: `BeginSpan`, `Log` |
| `IProfiler` | Profiling contract: `BeginSample` |
| `NoOpTracer` | Default tracer that discards all output |
| `NoOpProfiler` | Default profiler that discards all output |
| `ProfilerExtensions` | Convenience extension methods |
| `CoreLogBridge` | Bridges `CoreLog.Handler` to `ITracer.Log` |
| `NullDisposable` | Shared `IDisposable` that does nothing on `Dispose` |

## Notes

- Engine adapters replace the no-op defaults with native implementations (e.g., Unity's `ProfilerMarker`).
- `CoreLogBridge` unifies Core internal diagnostics with the project-wide tracing pipeline.
