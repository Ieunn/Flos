# Flos.Adapter.Unity

Unity engine adapter for Flos. Provides MonoBehaviour-based session management and bridges for logging, assets, save storage, and profiling, plus optional VContainer DI integration.

## Installation

Add the `Flos.Adapter.Unity` package to your Unity project (via local reference or UPM).

> **Note:** This package is compiled by Unity and excluded from the .NET solution builds (`ExcludeFromBuild=true`).

## Quick Usage

1. Subclass `FlosSession` and override `GetModules()` to provide your game modules.
2. Add the component to a GameObject.
3. Configure `TickMode`, `FixedTimeStep`, and `AutoInitialize` in the Inspector.

```csharp
public class MyGameSession : FlosSession
{
    protected override IReadOnlyList<IModule> GetModules()
    {
        return [new UnityAdapterModule(), /* game modules */];
    }
}
```

### Manual Setup

For full control without `FlosSession`:

```csharp
void Awake()
{
    _session = new Session();
    _session.Initialize(new SessionConfig
    {
        Modules = [new UnityAdapterModule(), /* game modules */],
        TickMode = TickMode.FixedTick,
    });
}

void Start() => _session.Start();
void FixedUpdate() => _session.Scheduler.Tick(Time.fixedDeltaTime);
void OnDestroy() { _session.Shutdown(); _session.Dispose(); }
```

### VContainer Integration

Define `FLOS_VCONTAINER` to enable the DI adapter. Then set the `ScopeFactory` property on `FlosSession` before initialization:

```csharp
flosSession.ScopeFactory = new VContainerScopeFactory(container);
```

## API Overview

| Type | Description |
|------|-------------|
| `FlosSession` | Abstract MonoBehaviour that owns and drives an `ISession`. Subclass and override `GetModules()`. |
| `UnityAdapterModule` | Registers Unity bridges for logging (`CoreLog`), assets (`IAssetProvider`), save storage (`ISaveStorage`), and profiling (`IProfiler`). |
| `UnityLogBridge` | Static bridge from `CoreLog` to `Debug.Log` / `LogWarning` / `LogError`. |
| `UnityInputBridge` | Abstract `IInputProvider` base — subclass to map Unity InputActions to Flos messages. |
| `UnityAssetBridge` | `IAssetProvider` backed by Addressables. Supports `CancellationToken`. Completion callbacks dispatched via `IDispatcher`. |
| `UnitySaveBridge` | `ISaveStorage` backed by `Application.persistentDataPath`. File I/O on background threads. Supports `CancellationToken`. |
| `UnityProfilerBridge` | `IProfiler` backed by `ProfilerMarker`. Caches markers by name. |
| `VContainerScopeFactory` | `IScopeFactory` wrapping VContainer's `IObjectResolver`. Requires `FLOS_VCONTAINER` define. |
| `VContainerServiceScope` | `IServiceRegistry` backed by VContainer. Accumulates registrations, builds child scope on `Lock`. |
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

- `FlosSession` is abstract — you must subclass it and provide modules via `GetModules()`. Module composition is a game-level decision.
- `UnityInputBridge` is abstract — input mapping is game-specific. Subclass it and register your implementation as `IInputProvider`.
- `UnityAssetBridge` uses Addressables (`LoadAssetAsync<T>`). Results are dispatched to the main thread via `IDispatcher.Enqueue`. Pass a `CancellationToken` to cancel in-flight loads.
- Save files are stored under `Application.persistentDataPath/saves/{slot}.sav`.
- `FlosSession.Step()` is available for `StepBased` tick mode (e.g., turn-based games).
- The custom editor (`FlosSessionEditor`) displays session state, current tick, elapsed time, and registered state slices at runtime.
