# Flos.Snapshot

Deep-copy state snapshots and read-only state views. Used by Pattern.CQRS for handler isolation and by Flos.Testing for replay verification.

## Installation

```xml
<PackageReference Include="Flos.Snapshot" />
```

## Quick Usage

```csharp
var snapshotManager = scope.Resolve<ISnapshotManager>();

// Capture a deep-copy snapshot
IStateView snapshot = snapshotManager.Capture(session.World);

// Read state from snapshot (immutable)
var state = snapshot.Get<MyState>();

// Restore world to a previous snapshot
snapshotManager.RestoreTo(session.World, snapshot);
```

## API Overview

| Type | Description |
|------|-------------|
| `IStateView` | Read-only view of captured state slices |
| `ISnapshotManager` | Captures and restores deep-copy snapshots |
| `SnapshotManager` | Default implementation using reflection-cached delegates |
| `IDeepCloneable<T>` | Interface that state slices must implement for snapshot support |
| `SnapshotModule` | Registers `ISnapshotManager` |

## Requirements

- State slices must implement `IDeepCloneable<T>` to be snapshot-capable.
- Use `[GenerateDeepClone]` from [Flos.Generators](../Flos.Generators) for automatic implementation.

## Pattern-Mode Behavior

- **CQRS:** Snapshots are captured automatically for every command (handler receives `IStateView`, applier failures trigger rollback).
- **Standalone/ECS:** Use `ISnapshotManager` directly for manual save/load or undo.
