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
| FLOS001 | Error | `System.Random` in game logic — use `IRandom` |
| FLOS002 | Error | `DateTime.Now` / `Environment.TickCount` in handlers |
| FLOS003 | Error | `async/await` in handlers |
| FLOS004 | Error | File/network I/O in handlers |
| FLOS005 | Warning | `Dictionary`/`HashSet` in `IStateSlice` fields |
| FLOS006 | Warning | Mutable static field access |
| FLOS007 | Warning | `ICommand`/`IEvent` as class (prefer `readonly record struct`) |
| FLOS008 | Configurable | Mutable call on `IStateSlice` inside handler |
| FLOS009 | Configurable | `float`/`double` in handler (consider fixed-point) |
| FLOS010 | Error | Core service call from worker thread |
| FLOS012 | Error | Allocation in `[HotPath]` code |
| FLOS013 | Warning | `Resolve<T>()` in `[HotPath]` code |
| FLOS016 | Error | `Guid.NewGuid()` or `new Random()` in handlers |
| FLOS017 | Error | Implementation types in Contract package |

## Scope

Analyzers apply to code within:
- `ICommandHandler<T>.Handle` implementations
- `IEventApplier<TEvent, TState>.Apply` implementations
- Methods or types annotated with `[HotPath]`

Code outside these scopes is not flagged.

## Code Fix

FLOS001 includes a code fix provider that suggests replacing `System.Random` with `IRandom`.
