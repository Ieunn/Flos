# Flos.Core

Pattern-neutral microkernel providing messaging, state management, scheduling, module loading, and error handling for game logic.

## Installation

```xml
<PackageReference Include="Flos.Core" />
```

## Quick Usage

```csharp
using Flos.Core.Sessions;
using Flos.Core.Scheduling;

var session = new Session();
session.Initialize(new SessionConfig
{
    Modules = [/* your modules here */],
    TickMode = TickMode.StepBased,
});
session.Start();
session.Scheduler.Step(); // fires one tick
session.Shutdown();
session.Dispose();
```

## Subsystem Overview

### Messaging (`IMessageBus`)

The message bus is the central communication channel. All module-to-module interaction goes through typed pub/sub messages. Subscriptions are priority-ordered (lower executes first) and dispatched synchronously on the main thread. Middleware interceptors can be installed to add cross-cutting behavior (e.g., CQRS command routing). The bus is re-entrant — publishing during a publish is safe.

### State (`IWorld`)

The world is the single source of truth for all game state. It stores typed `IStateSlice` instances, each registered once by type. Slices are mutable reference types (classes). `IWorld` extends `IStateReader` (read-only view) with mutation operations: `Register<T>`, `Unregister<T>`, and `SetSlice`. When a slice is replaced or unregistered, `IDisposable` slices are disposed automatically.

State slices that participate in snapshots must implement `IDeepCloneable<T>` (defined in `Flos.Core.State`). Use the `[DeepClone]` generator from `Flos.Generators` for automatic implementation.

### Scheduling (`IScheduler`)

The scheduler controls tick dispatch. In `StepBased` mode, each `Step()` call fires one tick with `DeltaTime = 0`. In `FixedTick` mode, `Tick(deltaTime)` accumulates time and fires fixed-step ticks at `SessionConfig.FixedTimeStep` intervals. Each tick drains the `IDispatcher` queue, then publishes a `TickMessage`. `ElapsedTime` is tracked as `double` for sub-millisecond precision over long sessions.

### Sessions (`ISession`)

The session is the top-level container that owns the world, scheduler, message bus, service scope, and module lifecycle. Its lifecycle is forward-only: `Created` → `Initializing` → `Initialized` → `Running` ↔ `Paused` → `ShuttingDown` → `Disposed`. Any failure during initialization transitions directly to `Disposed` — to retry, create a new `Session` instance.

### Modules (`ModuleBase`)

Modules are black boxes with a defined lifecycle: `OnLoad(IServiceRegistry)` → `OnInitialize` → `OnStart` → `OnPause`/`OnResume` → `OnShutdown`. During `OnLoad`, the scope is open — register services and resolve pre-registered infrastructure (IWorld, IMessageBus, etc.) via `scope.Resolve<T>()`. During `OnInitialize`, the scope is locked — resolve cross-module services and cache references via `Scope` (the same `IServiceRegistry`). Modules are loaded in topological dependency order and shut down in reverse order.

```csharp
public class MyModule : ModuleBase
{
    public override string Id => "My";
    public override IReadOnlyList<string> Dependencies => ["Identity"];

    private IMessageBus _bus = null!;

    public override void OnLoad(IServiceRegistry scope)
    {
        base.OnLoad(scope);
        Scope.Resolve<IWorld>().Register(new MyState());
    }

    public override void OnInitialize()
    {
        _bus = Scope.Resolve<IMessageBus>();
        _bus.Subscribe<TickMessage>(OnTick);
    }

    private void OnTick(TickMessage tick) { /* per-tick logic */ }
}
```

### Errors (`ErrorCode`, `Result<T>`, `FlosException`)

`ErrorCode` is a structured identifier (Category + Code) for classifiable errors. `Result<T>` is a value-type monad for expected domain failures — supports `Map`, `Bind`, `Match`, and implicit conversion from `ErrorCode`. `FlosException` wraps an `ErrorCode` and is thrown for infrastructure-level errors (missing slice, scope locked, thread violation). Use `Result<T>` for game logic; reserve exceptions for fatal developer bugs.

## Session Lifecycle Quick-Reference

```
Created ──Initialize()──► Initializing ──success──► Initialized ──Start()──► Running
                               │                         │                    ↕
                               └──failure──► Disposed     │              Paused
                                                          │                 │
                                                          └──Shutdown()──►  │
                                                                            └──► ShuttingDown ──► Disposed
```

Lifecycle messages published: `SessionInitializedMessage`, `SessionStartedMessage`, `SessionPausedMessage`, `SessionResumedMessage`, `SessionShutdownMessage`.

## API Overview

### Errors

| Type | Description |
|------|-------------|
| `ErrorCode` | Structured error identifier (Category + Code) |
| `Result<T>` | Value-type result monad for expected failures (`Map`, `Bind`, `Match`, implicit `ErrorCode` conversion) |
| `FlosException` | Infrastructure exception carrying an `ErrorCode` |
| `Unit` | Void replacement for `Result<Unit>` |
| `CoreErrors` | Well-known Core error codes |

### Messaging

| Type | Description |
|------|-------------|
| `IMessage` | Marker interface for all messages |
| `IMessageBus` | Pub/sub: `Subscribe` (zero-alloc, returns `long`), `Unsubscribe`, `Listen` (returns `IDisposable`), `Publish`, `Use` (middleware) |
| `IMessageMiddleware` | Pipeline interceptor for cross-cutting concerns |
| `MessageBus` | Default zero-allocation implementation. `OnHandlerException` callback for custom error handling. |

### State

| Type | Description |
|------|-------------|
| `IStateSlice` | Marker for mutable state containers in `IWorld` |
| `IDeepCloneable<T>` | Deep-clone contract for snapshot support. Use `[DeepClone]` generator for auto-implementation. |
| `IStateReader` | Read-only view: `Get<T>`, `TryGet<T>`, `GetAll`, `Contains<T>` |
| `IWorld` | Extends `IStateReader` with mutation: `Register<T>`, `Unregister<T>`, `SetSlice` |
| `World` | Default list-backed implementation. Disposes old `IDisposable` slices on replace/unregister. |

### Scheduling

| Type | Description |
|------|-------------|
| `TickMode` | `StepBased` or `FixedTick` |
| `TickMessage` | Published every simulation tick. Carries `DeltaTime` and `Tick` number. |
| `IScheduler` | Controls tick dispatch: `Step()`, `Tick(double)`, `CurrentTick`, `ElapsedTime` (double) |
| `IDispatcher` | Thread-safe action queue for cross-thread bridging. `OnActionException` callback for custom error handling. |

### Sessions

| Type | Description |
|------|-------------|
| `ISession` | Top-level game session owning world, scheduler, bus. Forward-only lifecycle: failure transitions to `Disposed`. |
| `SessionConfig` | Configuration: `Modules`, `TickMode`, `FixedTimeStep`, `ScopeFactory` |
| `SessionState` | Lifecycle enum: `Created` → `Initializing` → `Initialized` → `Running` ↔ `Paused` → `ShuttingDown` → `Disposed` |
| Lifecycle messages | `SessionInitializedMessage`, `SessionStartedMessage`, etc. |

### Modules

| Type | Description |
|------|-------------|
| `IModule` | Module lifecycle contract |
| `ModuleBase` | Convenience base class with no-op defaults |
| `IServiceRegistry` | Singleton service locator with two-phase lifecycle (register, then lock). `Register`, `TryRegister`, `Resolve`, `TryResolve`, `IsRegistered`, `Lock`. |
| `IScopeFactory` | Factory for external DI container integration |
| `IPatternRegistry` | Registry of loaded gameplay patterns |
| `ModuleLoader` | Topological sort and pattern validation |

### Annotations

| Type | Description |
|------|-------------|
| `[HotPath]` | Marks performance-critical code for analyzer enforcement |

## Determinism Notes

Core is deterministic by design: single-threaded execution, no internal randomness, deterministic subscription ordering. External threads must use `IDispatcher.Enqueue()` to safely interact with Core services.

`IScheduler.ElapsedTime` is `double` (not `float`) to maintain sub-millisecond precision over long play sessions.

`ThreadGuard` (internal) verifies that Core subsystems (MessageBus, World, Scheduler) are accessed from their owning thread. In DEBUG builds, violations trigger `Debug.Assert`. When `FlosDebug.EnforceThreadSafety` is enabled (automatically in DEBUG during `Session.Initialize()`), violations throw `FlosException` with `CoreErrors.ThreadViolation`. Use `IDispatcher.Enqueue()` for all cross-thread access.
