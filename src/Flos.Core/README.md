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

## API Overview

### Core/Errors

| Type | Description |
|------|-------------|
| `ErrorCode` | Structured error identifier (Category + Code) |
| `Result<T>` | Value-type result monad for expected failures (`Map`, `Bind`, `Match`, implicit `ErrorCode` conversion) |
| `FlosException` | Infrastructure exception carrying an `ErrorCode` |
| `Unit` | Void replacement for `Result<Unit>` |
| `CoreErrors` | Well-known Core error codes |

### Core/Messaging

| Type | Description |
|------|-------------|
| `IMessage` | Marker interface for all messages |
| `IMessageBus` | Pub/sub message bus: `Subscribe` (returns `int`, zero-alloc), `Unsubscribe`, `Listen` (returns `IDisposable`), `Publish` |
| `IMessageMiddleware` | Pipeline interceptor for cross-cutting concerns |
| `MessageBus` | Default zero-allocation implementation |

### Core/State

| Type | Description |
|------|-------------|
| `IStateSlice` | Marker for mutable state containers in `IWorld` |
| `IWorld` | Single source of truth for all game state |
| `World` | Default dictionary-backed implementation |

### Core/Scheduling

| Type | Description |
|------|-------------|
| `TickMode` | `StepBased` or `FixedTick` |
| `TickMessage` | Published every simulation tick |
| `IScheduler` | Controls tick dispatch |
| `IDispatcher` | Thread-safe action queue for cross-thread bridging |

### Core/Sessions

| Type | Description |
|------|-------------|
| `ISession` | Top-level game session owning world, scheduler, bus |
| `SessionConfig` | Configuration passed to `Initialize` |
| `SessionState` | Lifecycle state enum (`Created`, `Initializing`, `Initialized`, `Running`, `Paused`, `ShuttingDown`, `Disposed`) |
| Lifecycle messages | `SessionInitializedMessage`, `SessionStartedMessage`, etc. |

### Core/Module

| Type | Description |
|------|-------------|
| `IModule` | Module lifecycle contract |
| `ModuleBase` | Convenience base class with no-op defaults |
| `IServiceScope` | Singleton service locator |
| `IDIAdapter` | Adapter for external DI containers |
| `IPatternRegistry` | Registry of loaded gameplay patterns |
| `ModuleLoader` | Topological sort and pattern validation |

### Core/Annotations

| Type | Description |
|------|-------------|
| `[HotPath]` | Marks performance-critical code for analyzer enforcement |

## Determinism Notes

Core is deterministic by design: single-threaded execution, no internal randomness, deterministic subscription ordering. External threads must use `IDispatcher.Enqueue()` to safely interact with Core services.
