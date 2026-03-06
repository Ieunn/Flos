# Flos.Identity

Shared logical identity for game objects across module boundaries. Provides a compact 8-byte `EntityId` and deterministic sequential ID generation with snapshot support.

## Installation

```xml
<PackageReference Include="Flos.Identity" />
```

## Quick Usage

Add `IdentityModule` to your session:

```csharp
session.Initialize(new SessionConfig
{
    Modules = [new IdentityModule(), /* other modules */],
    TickMode = TickMode.StepBased,
});
```

Then resolve and use `IIdGenerator` in any module:

```csharp
var idGen = Scope.Resolve<IIdGenerator>();
EntityId playerId = idGen.Next(); // 1, 2, 3, ...
```

Optionally start from a custom value: `new IdentityModule(startValue: 1000)`.

## API Overview

| Type | Description |
|------|-------------|
| `EntityId` | 8-byte value type identifier (`long`-backed, `IComparable<EntityId>`, `IEquatable<EntityId>`) |
| `IIdGenerator` | Generates unique `EntityId` values; supports `GetState()`/`RestoreState()` for snapshots |
| `SequentialIdGenerator` | Monotonically increasing counter starting from a configurable value. Uses checked arithmetic to prevent overflow. |
| `IdentityModule` | Registers `IIdGenerator` (no dependencies) |

## Snapshot Support

`IIdGenerator` provides `GetState()` and `RestoreState(long)` for snapshot integration. After restoring world state from a snapshot, also restore the generator state to prevent ID collisions:

```csharp
// Capture
long idState = idGen.GetState();

// Restore
idGen.RestoreState(idState);
```

## Determinism Notes

- IDs are deterministic: same start value produces the same ID sequence.
- `EntityId.None` (value 0) is the sentinel for "no entity". `SequentialIdGenerator` will never produce `EntityId.None`.
- `SequentialIdGenerator` uses checked arithmetic and throws `OverflowException` if the counter exceeds `long.MaxValue`.
- `EntityId` implements `IComparable<EntityId>`, making it usable as a key in `IOrderedMap<EntityId, T>`.
- This is a **logical** identifier, not an ECS entity. In ECS mode, use `IEntityIdBridge` to map between `EntityId` and ECS-native entities.
