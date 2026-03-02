# Flos.Adapter.Godot

Godot engine adapter for Flos. Provides Node-based session management and bridges for logging, assets, save storage, and profiling.

## Installation

Add the `Flos.Adapter.Godot` package to your Godot C# project (via local reference).

> **Note:** This package is compiled by Godot and excluded from the .NET solution builds (`ExcludeFromBuild=true`).

## Quick Usage

1. Add the `FlosSession` node to your scene tree.
2. Configure `TickMode`, `FixedTimeStep`, `RandomSeed`, and `AutoInitialize` via `[Export]` properties in the Inspector.
3. Subclass `FlosSession` and override `GetModules()` to add your game modules.
4. The node manages the full `Initialize` -> `Start` -> `Tick` -> `Shutdown` lifecycle automatically.

```csharp
public partial class MyGameSession : FlosSession
{
    protected override IReadOnlyList<IModule> GetModules()
    {
        return [new GodotAdapterModule(), new RandomModule(), /* game modules */];
    }
}
```

### Manual Setup

```csharp
var session = new Session();
session.Initialize(new SessionConfig
{
    Modules = [new GodotAdapterModule(), new RandomModule(), /* game modules */],
    TickMode = TickMode.FixedTick,
    RandomSeed = 42
});
session.Start();

// In _PhysicsProcess:
session.Scheduler.Tick((float)delta);
```

## API Overview

| Type | Description |
|------|-------------|
| `FlosSession` | `partial class Node` that owns and drives an `ISession`. Subclass and override `GetModules()` to add game modules. |
| `GodotAdapterModule` | Registers Godot bridges for logging (`CoreLog`), assets (`IAssetProvider`), save storage (`ISaveStorage`), and profiling (`IProfiler`). |
| `GodotLogBridge` | Static bridge from `CoreLog` to `GD.Print` / `GD.PushWarning` / `GD.PushError`. |
| `GodotInputBridge` | Abstract `IInputProvider` base -- subclass to map Godot InputActions/InputEvents to Flos messages. |
| `GodotAssetBridge` | `IAssetProvider` backed by `ResourceLoader`. Loads on a background thread, dispatches results via `IDispatcher`. |
| `GodotSaveBridge` | `ISaveStorage` backed by `user://saves/` directory. File I/O on background threads. |
| `GodotProfilerBridge` | `IProfiler` using `Stopwatch`-based timing. Logs elapsed time when `FLOS_PROFILING` is defined. |

## Lifecycle Mapping

| Godot | Flos |
|-------|------|
| `_Ready` | `Session.Initialize` + `Session.Start` |
| `_PhysicsProcess(delta)` | `Scheduler.Tick((float)delta)` |
| `NotificationApplicationPaused` | `Session.Pause` |
| `NotificationApplicationResumed` | `Session.Resume` |
| `NotificationWMWindowFocusOut` | `Session.Pause` |
| `NotificationWMWindowFocusIn` | `Session.Resume` |
| `_ExitTree` | `Session.Shutdown` + `Dispose` |

## Notes

- `GodotInputBridge` is abstract -- input mapping is game-specific. Subclass it and register your implementation as `IInputProvider`.
- `GodotAssetBridge` uses `ResourceLoader.Load` on a background thread via `Task.Run`. Results are dispatched to the main thread via `IDispatcher.Enqueue`.
- Save files are stored under `user://saves/{slot}.sav` (resolved via `ProjectSettings.GlobalizePath`).
- `FlosSession.Step()` is available for `StepBased` tick mode (e.g., turn-based games).
- Pause/resume is handled for both mobile (`NotificationApplicationPaused/Resumed`) and desktop (`NotificationWMWindowFocusOut/In`) scenarios.
- `GodotProfilerBridge` uses `Stopwatch` since Godot lacks a `ProfilerMarker` equivalent. Define `FLOS_PROFILING` to enable console output of timing data.
