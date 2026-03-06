# Flos.Snapshot

Deep-copy state snapshots and read-only state views. Used by Pattern.CQRS for handler isolation and rollback.

## Installation

```xml
<PackageReference Include="Flos.Snapshot" />
```

## Quick Usage

```csharp
var snapshotManager = scope.Resolve<ISnapshots>();

// Register slice types during OnLoad (AOT-safe — no runtime reflection)
snapshotManager.RegisterSlice<MyState>();

// Capture a deep-copy snapshot
IStateView snapshot = snapshotManager.Capture(session.World);

// Read state from snapshot (immutable)
var state = snapshot.Get<MyState>();

// Restore world to a previous snapshot (snapshot remains valid)
snapshotManager.RestoreTo(session.World, snapshot);

// Or restore and consume in one step (more efficient, snapshot is invalidated)
snapshotManager.RestoreAndConsume(session.World, snapshot);

// Return snapshot to pool when done (optional, reduces allocations)
snapshotManager.Return(snapshot);
```

## API Overview

| Type | Description |
|------|-------------|
| `IStateView` | Read-only view of captured state slices (extends `IStateReader`) |
| `ISnapshots` | Captures and restores deep-copy snapshots: `RegisterSlice<T>`, `Capture`, `RestoreTo`, `RestoreAndConsume`, `Return` |
| `Snapshots` | Default AOT-safe implementation using typed delegates (no runtime reflection). Pools `StateView` objects internally |
| `IDeepCloneable<T>` | Interface that state slices must implement for snapshot support (defined in `Flos.Core.State`) |
| `SnapshotModule` | Registers `ISnapshots` (no dependencies) |
| `SnapshotErrors` | Error codes: `SliceNotFound` (FLOS-300-0001), `NotCloneable` (FLOS-300-0002) |

## Requirements

- State slices must implement `IDeepCloneable<T>` to be snapshot-capable.
- Use `[DeepClone]` from [Flos.Generators](../Flos.Generators) for automatic implementation.

## Pooling

`Snapshots` internally pools `StateView` objects. Call `Return(snapshot)` when a snapshot is no longer needed to recycle it for the next `Capture()`. This is particularly important on the CQRS rollback hot path where snapshots are captured and discarded every command.

## Unregistered Slices

State slice types that are not registered with `RegisterSlice<T>()` are silently skipped during `Capture` and `RestoreTo`. This means only slices you explicitly opt in will be snapshot-capable. Ensure all relevant slices are registered during module `OnInitialize` (resolve `ISnapshots` via `Scope` after the registry is locked).

## Pattern-Mode Behavior

- **CQRS:** Snapshots are captured automatically for every command (handler receives `IStateReader`, applier failures trigger rollback). The pipeline calls `IRollbackProvider.Release` to return snapshots to the pool.
- **Standalone/ECS:** Use `ISnapshots` directly for manual save/load or undo.
