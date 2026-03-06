# Flos.Pattern.ECS

Adapter-first ECS integration for mass-entity simulation. Provides lifecycle hooks, tick integration, and identity bridging — delegates actual ECS operations to a framework-specific adapter.

## Installation

```xml
<PackageReference Include="Flos.Pattern.ECS" />
```

## Quick Usage

```csharp
// Implement IECSAdapter for your ECS framework (e.g., Arch, DefaultEcs):
public class ArchAdapter : IECSAdapter
{
    public void CreateWorld(IWorld world) { /* register ECS state slice */ }
    public void Tick(double deltaTime) { /* run ECS systems */ }
    public void Shutdown() { /* cleanup */ }
}

// Register ECSPatternModule with your adapter:
var ecsModule = new ECSPatternModule(new ArchAdapter());
// Add ecsModule to your session's module list
```

## Module Setup

`ECSPatternModule` is the entry point. It accepts three constructor parameters:

```csharp
new ECSPatternModule(
    adapter: new ArchAdapter(),       // required: your IECSAdapter implementation
    commandBuffer: null,              // optional: shared CommandBuffer (creates one if null)
    tickPriority: 100                 // optional: TickMessage subscription priority (default 100)
)
```

The `tickPriority` parameter controls when ECS ticks relative to other subscribers. Lower values execute first. If you have a CQRS pattern handling game logic at default priority (0), the ECS tick at priority 100 runs after game commands are processed.

## CommandBuffer

`ICommandBuffer` provides a thread-safe way for ECS systems running on worker threads to publish messages back to the main-thread `IMessageBus`. Messages are drained after the adapter's `Tick()` completes.

```csharp
// In your ECS system (may run on a worker thread):
public class DamageSystem
{
    private readonly ICommandBuffer _buffer;

    public DamageSystem(ICommandBuffer buffer) => _buffer = buffer;

    public void Execute(/* ECS query results */)
    {
        // Thread-safe: enqueue for main-thread publish after tick
        _buffer.PublishAfterTick(new EntityDestroyedMessage(entityId));
    }
}
```

The `ECSPatternModule` drains the buffer after each `adapter.Tick()` call (in a `finally` block, so messages are drained even if the tick throws).

## EntityIdBridge

`EntityIdBridge<TId, TEntity>` provides bidirectional mapping between Flos logical IDs (e.g., `EntityId`) and ECS-native entity identifiers. Thread-safe via internal locking.

```csharp
var bridge = new EntityIdBridge<EntityId, ArchEntity>();

// Link a Flos entity to an ECS entity
bridge.Link(flosId, archEntity);

// Look up in either direction
if (bridge.TryGetEntity(flosId, out var ecsEntity)) { /* ... */ }
if (bridge.TryGetFlosId(archEntity, out var id)) { /* ... */ }

// Remove mapping
bridge.Unlink(flosId);
```

Register the bridge as a service in your adapter module's `OnLoad` for cross-module access:

```csharp
scope.Register<IEntityIdBridge<EntityId, ArchEntity>>(bridge);
```

## API Overview

| Type | Description |
|------|-------------|
| `IECSAdapter` | Adapter contract: `CreateWorld(IWorld)`, `Tick(double deltaTime)`, `Shutdown()` |
| `ICommandBuffer` | Deferred message queue for safe cross-thread publishing: `PublishAfterTick<T>(T message)` |
| `CommandBuffer` | Default `ICommandBuffer` implementation (thread-safe) |
| `IEntityIdBridge<TId, TEntity>` | Maps logical IDs to ECS-native entity identifiers: `Link`, `Unlink`, `TryGetEntity`, `TryGetFlosId` |
| `EntityIdBridge<TId, TEntity>` | Default bidirectional bridge implementation (thread-safe via locking) |
| `ECSPatternModule` | Registers pattern, subscribes to ticks, drains command buffer. Constructor: `(IECSAdapter adapter, CommandBuffer? commandBuffer = null, int tickPriority = 100)` |
| `ECSPattern` | Pattern identifier (`"ECS"`) |

## Design Philosophy

Pattern.ECS is intentionally **thin**. It does not define a universal System/Query/Component API — those differ fundamentally across ECS frameworks. Instead, it provides:
- **Startup protocol:** `IECSAdapter.CreateWorld` is called during `OnInitialize` — the adapter creates and registers the ECS world as an `IStateSlice`
- **Tick integration:** Subscribes to `TickMessage` at configurable priority, delegates to `IECSAdapter.Tick(double deltaTime)`
- **Identity bridge:** Links logical IDs (e.g., `EntityId`) to ECS-native entities via `IEntityIdBridge`
- **Safe messaging:** `ICommandBuffer` for ECS systems to enqueue messages from worker threads
