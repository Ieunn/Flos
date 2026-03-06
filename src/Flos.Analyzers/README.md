# Flos.Analyzers

Roslyn analyzers that enforce Flos framework rules at compile time. Catches determinism violations, threading errors, and performance issues.

## Installation

```xml
<PackageReference Include="Flos.Analyzers" />
```

Analyzers run automatically during build. No code changes needed.

## Analyzer Rules

| Rule | Severity | Description |
|------|----------|-------------|
| FLOS001 | Warning | `System.Random` in game logic — use `IRandom`. Upgrade to Error with `<FlosEnforceDeterminism>true</FlosEnforceDeterminism>` in your project file. |
| FLOS002 | Warning | `DateTime.Now` / `DateTime.Today` / `DateTimeOffset.Now` / `Environment.TickCount` / `Stopwatch` in handlers |
| FLOS003 | Error | `async/await` in handlers |
| FLOS004 | Error | File/network I/O in handlers (`File`, `Directory`, `FileStream`, `StreamReader`, `StreamWriter`, `FileInfo`, `DirectoryInfo`, `Socket`, `TcpClient`, `UdpClient`, `HttpClient`, `WebRequest`) |
| FLOS005 | Warning | `Dictionary`/`HashSet` in `IStateSlice` fields — use `IOrderedMap`/`IOrderedSet` |
| FLOS006 | Warning | Mutable static field access |
| FLOS007 | Warning | `ICommand`/`IEvent` as class or record class (prefer `readonly record struct`) |
| FLOS008 | Hidden | Mutable call on `IStateSlice` inside handler. Default: Hidden (disabled). Enable via `.editorconfig`: `dotnet_diagnostic.FLOS008.severity = warning` |
| FLOS009 | Hidden | `float`/`double` in handler (consider fixed-point). Default: Hidden (disabled). Enable via `.editorconfig`: `dotnet_diagnostic.FLOS009.severity = warning` |
| FLOS010 | Error | Core service call from worker thread |
| FLOS011 | Error | Allocation in `[HotPath]` code (closures, LINQ, new objects, arrays, string interpolation, yield) |
| FLOS012 | Warning | `Resolve<T>()` in handler/applier or `[HotPath]` code |
| FLOS013 | Warning | `Guid.NewGuid()` or `new Random()` in handlers |
| FLOS014 | Error | Implementation types in Contract package (value types allowed) |

## Scope

Analyzers apply to code within:
- `ICommandHandler<T>.Handle` implementations
- `IEventApplier<TEvent, TState>.Apply` implementations
- Methods or types annotated with `[HotPath]`

Code outside these scopes is not flagged.

## Code Fix

FLOS001 includes a code fix provider that suggests replacing `System.Random` with `IRandom`.
