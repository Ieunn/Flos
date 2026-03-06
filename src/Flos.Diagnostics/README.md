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
| `ITracer` | Tracing contract: `BeginSpan(string name)` returns `IDisposable`, `Log(TraceLevel level, string message)` |
| `TraceLevel` | Enum: `Debug`, `Info`, `Warning`, `Error` |
| `IProfiler` | Profiling contract: `BeginSample(string name)` returns `IDisposable` |
| `NoOpTracer` | Default tracer that discards all output |
| `NoOpProfiler` | Default profiler that discards all output |
| `ProfilerExtensions` | Compile-stripped conditional profiling (`BeginSampleConditional`/`EndSampleConditional`). Use in `try/finally` for exception safety. Calls are stripped when `FLOS_PROFILING` is not defined. |
| `ProfilerSampleStack` | (internal) Thread-local bounded stack for conditional samples. Capped at depth 32 to prevent unbounded growth from mismatched calls. |
| `CoreLogBridge` | Bridges `CoreLog.Handler` to `ITracer.Log` |
| `NullDisposable` | Shared `IDisposable` that does nothing on `Dispose` |

## Notes

- Engine adapters replace the no-op defaults with native implementations (e.g., Unity's `ProfilerMarker`).
- `CoreLogBridge` unifies Core internal diagnostics with the project-wide tracing pipeline.
- `ProfilerSampleStack` uses a bounded depth of 32. If mismatched `BeginSampleConditional`/`EndSampleConditional` calls exceed this depth, additional samples are silently dropped.
