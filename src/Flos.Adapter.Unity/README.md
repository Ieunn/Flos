# Flos.Adapter.Unity

Unity engine adapter for Flos. Provides MonoBehaviour-based session management, bridges for logging, assets, save storage, and profiling, plus optional VContainer DI integration.

## Installation

Add the `Flos.Adapter.Unity` package to your Unity project (via local reference or UPM).

> **Note:** This package is compiled by Unity and excluded from the .NET solution builds (`ExcludeFromBuild=true`).

## Quick Usage

1. Add the `FlosSession` component to a GameObject.
2. Configure `TickMode`, `FixedTimeStep`, `RandomSeed`, and `AutoInitialize` in the Inspector.
3. Subclass `FlosSession` and override `GetModules()` to add your game modules.
4. The component manages the full `Initialize` -> `Start` -> `Tick` -> `Shutdown` lifecycle automatically.

```csharp
public class MyGameSession : FlosSession
{
    protected override IReadOnlyList<IModule> GetModules()
    {
        return [new UnityAdapterModule(), new RandomModule(), /* game modules */];
    }
}
```

### Manual Setup

```csharp
var session = new Session();
session.Initialize(new SessionConfig
{
    Modules = [new UnityAdapterModule(), new RandomModule(), /* game modules */],
    TickMode = TickMode.FixedTick,
    RandomSeed = 42
});
session.Start();

// In FixedUpdate:
session.Scheduler.Tick(Time.fixedDeltaTime);
```

### VContainer Integration

Define `FLOS_VCONTAINER` to enable the DI adapter. Then set the `DIAdapter` property on `FlosSession` before initialization:

```csharp
flosSession.DIAdapter = new VContainerDIAdapter(container);
```

## API Overview

| Type | Description |
|------|-------------|
| `FlosSession` | MonoBehaviour that owns and drives an `ISession`. Subclass and override `GetModules()` to add game modules. |
| `UnityAdapterModule` | Registers Unity bridges for logging (`CoreLog`), save storage (`ISaveStorage`), and profiling (`IProfiler`). |
| `UnityLogBridge` | Static bridge from `CoreLog` to `Debug.Log` / `LogWarning` / `LogError`. |
| `UnityInputBridge` | Abstract `IInputProvider` base -- subclass to map Unity InputActions to Flos messages. |
| `UnityAssetBridge` | `IAssetProvider` backed by Addressables. Completion callbacks dispatched via `IDispatcher`. |
| `UnitySaveBridge` | `ISaveStorage` backed by `Application.persistentDataPath`. File I/O on background threads. |
| `UnityProfilerBridge` | `IProfiler` backed by `ProfilerMarker`. Caches markers by name. |
| `VContainerDIAdapter` | `IDIAdapter` wrapping VContainer's `IObjectResolver`. Requires `FLOS_VCONTAINER` define. |
| `VContainerServiceScope` | `IServiceScope` backed by VContainer. Accumulates registrations, builds child scope on `Lock`. |
| `FlosSessionEditor` | Custom Inspector showing runtime session state, tick count, elapsed time, state slices, and pause/resume controls. |

## Lifecycle Mapping

| Unity | Flos |
|-------|------|
| `Awake` | `Session.Initialize` |
| `Start` | `Session.Start` |
| `FixedUpdate` | `Scheduler.Tick(Time.fixedDeltaTime)` |
| `OnApplicationPause(true)` | `Session.Pause` |
| `OnApplicationPause(false)` | `Session.Resume` |
| `OnDestroy` | `Session.Shutdown` + `Dispose` |

## Notes

- `UnityInputBridge` is abstract -- input mapping is game-specific. Subclass it and register your implementation as `IInputProvider`.
- `UnityAssetBridge` uses Addressables (`LoadAssetAsync<T>`). Results are dispatched to the main thread via `IDispatcher.Enqueue`.
- Save files are stored under `Application.persistentDataPath/saves/{slot}.sav`.
- `FlosSession.Step()` is available for `StepBased` tick mode (e.g., turn-based games).
- The custom editor (`FlosSessionEditor`) displays session state, current tick, elapsed time, and registered state slices at runtime.
