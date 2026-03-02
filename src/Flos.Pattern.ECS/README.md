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
    public void Tick(float deltaTime) { /* run ECS systems */ }
    public void Shutdown() { /* cleanup */ }
}

// Register ECSPatternModule with your adapter:
var ecsModule = new ECSPatternModule(new ArchAdapter());
// Add ecsModule to your session's module list
```

## API Overview

| Type | Description |
|------|-------------|
| `IECSAdapter` | Adapter contract: `CreateWorld`, `Tick`, `Shutdown` |
| `ICommandBuffer` | Deferred message queue for safe cross-thread publishing |
| `CommandBuffer` | Default `ICommandBuffer` implementation |
| `IEntityIdBridge` | Maps `EntityId` to ECS-native entity identifiers |
| `EntityIdBridge` | Default bidirectional bridge implementation |
| `ECSPatternModule` | Registers pattern, subscribes to ticks, drains command buffer |
| `ECSPattern` | Pattern identifier (`"ECS"`) |

## Design Philosophy

Pattern.ECS is intentionally **thin**. It does not define a universal System/Query/Component API — those differ fundamentally across ECS frameworks. Instead, it provides:
- **Startup protocol:** `IECSAdapter` creates and registers the ECS world
- **Tick integration:** Subscribes to `TickMessage`, delegates to adapter
- **Identity bridge:** Links `EntityId` to ECS-native entities
- **Safe messaging:** `ICommandBuffer` for ECS systems to enqueue messages outside the main thread
