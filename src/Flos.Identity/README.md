# Flos.Identity

Shared logical identity for game objects across module boundaries. Provides a compact 8-byte `EntityId` and deterministic ID generation.

## Installation

```xml
<PackageReference Include="Flos.Identity" />
```

## Quick Usage

```csharp
// In a module's OnInitialize, resolve the ID generator:
var idGen = Scope.Resolve<IIdGenerator>();
EntityId playerId = idGen.Next();
```

Register `IdentityModule` in your session config (depends on `RandomModule`).

## API Overview

| Type | Description |
|------|-------------|
| `EntityId` | 8-byte value type identifier (`long`-backed, O(1) equality) |
| `IIdGenerator` | Generates unique `EntityId` values |
| `SequentialIdGenerator` | Monotonically increasing counter seeded from `IRandom` |
| `IdentityModule` | Registers `IIdGenerator` (depends on `"Random"`) |

## Determinism Notes

- IDs are deterministic: same seed produces the same ID sequence.
- `EntityId.None` (value 0) is the sentinel for "no entity".
- This is a **logical** identifier, not an ECS entity. In ECS mode, use `IEntityIdBridge` to map between `EntityId` and ECS-native entities.
