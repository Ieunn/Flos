# Flos.Adapter.Godot

Godot engine adapter for Flos. Provides Node-based session management and bridges for logging, assets, save storage, and profiling.

## Installation

Add the `Flos.Adapter.Godot` package to your Godot C# project (via local reference).

> **Note:** This package is compiled by Godot and excluded from the .NET solution builds (`ExcludeFromBuild=true`).

## Quick Usage

1. Subclass `FlosSession` and override `GetModules()` to provide your game modules.
2. Add the node to your scene tree.
3. Configure `TickMode`, `FixedTimeStep`, and `AutoInitialize` via `[Export]` properties in the Inspector.

```csharp
public partial class MyGameSession : FlosSession
{
    protected override IReadOnlyList<IModule> GetModules()
    {
        return [new GodotAdapterModule(), /* game modules */];
    }
}
```

### Manual Setup

For full control without `FlosSession`:

```csharp
public override void _Ready()
{
    _session = new Session();
    _session.Initialize(new SessionConfig
    {
        Modules = [new GodotAdapterModule(), /* game modules */],
        TickMode = TickMode.FixedTick,
    });
    _session.Start();
}

public override void _PhysicsProcess(double delta)
{
    _session.Scheduler.Tick(delta);
}
```

## API Overview

| Type | Description |
|------|-------------|
| `FlosSession` | Abstract `partial class Node` that owns and drives an `ISession`. Subclass and override `GetModules()`. |
| `GodotAdapterModule` | Registers Godot bridges for logging (`CoreLog`), assets (`IAssetProvider`), save storage (`ISaveStorage`), and profiling (`IProfiler`). |
| `GodotLogBridge` | Static bridge from `CoreLog` to `GD.Print` / `GD.PushWarning` / `GD.PushError`. |
| `GodotInputBridge` | Abstract `IInputProvider` base — subclass to map Godot InputActions/InputEvents to Flos messages. |
| `GodotAssetBridge` | `IAssetProvider` backed by `ResourceLoader.LoadThreadedRequest`. Supports `CancellationToken`. Polls completion each tick, dispatches results via `IDispatcher`. |
| `GodotSaveBridge` | `ISaveStorage` backed by `user://saves/` directory. File I/O on background threads. Supports `CancellationToken`. |
| `GodotProfilerBridge` | `IProfiler` using per-call `Stopwatch` timing. Logs elapsed time when `FLOS_PROFILING` is defined. |

## Lifecycle Mapping

| Godot | Flos |
|-------|------|
| `_Ready` | `Session.Initialize` + `Session.Start` |
| `_PhysicsProcess(delta)` | `Scheduler.Tick(delta)` |
| `NotificationApplicationPaused` | `Session.Pause` |
| `NotificationApplicationResumed` | `Session.Resume` |
| `NotificationWMWindowFocusOut` | `Session.Pause` (when `PauseOnFocusLoss` enabled) |
| `NotificationWMWindowFocusIn` | `Session.Resume` (when `PauseOnFocusLoss` enabled) |
| `_ExitTree` | `Session.Shutdown` + `Dispose` |

## Notes

- `FlosSession` is abstract — you must subclass it and provide modules via `GetModules()`. Module composition is a game-level decision.
- `GodotInputBridge` is abstract — input mapping is game-specific. Subclass it and register your implementation as `IInputProvider`.
- `GodotAssetBridge` uses `ResourceLoader.LoadThreadedRequest` for thread-safe async loading. The `GodotAdapterModule` polls load status each tick via `TickMessage` subscription and dispatches completion callbacks to the main thread via `IDispatcher.Enqueue`. Pass a `CancellationToken` to cancel pending loads (cancelled entries are skipped during polling).
- Save files are stored under `user://saves/{slot}.sav` (resolved via `ProjectSettings.GlobalizePath`).
- `FlosSession.Step()` is available for `StepBased` tick mode (e.g., turn-based games).
- Pause/resume is handled for mobile (`NotificationApplicationPaused/Resumed`). Desktop focus-loss pausing (`NotificationWMWindowFocusOut/In`) is opt-in via the `PauseOnFocusLoss` export property.
- `GodotProfilerBridge` uses per-call `Stopwatch` instances since Godot lacks a `ProfilerMarker` equivalent. Define `FLOS_PROFILING` to enable console output of timing data.
