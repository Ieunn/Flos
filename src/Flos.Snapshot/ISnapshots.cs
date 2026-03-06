using Flos.Core.State;

namespace Flos.Snapshot;

/// <summary>
/// Creates and restores deep-copy snapshots of the world state.
/// </summary>
public interface ISnapshots
{
    /// <summary>
    /// Registers a state slice type for snapshot capture and restore.
    /// Must be called at module load time for every slice type that participates in snapshots.
    /// This avoids runtime reflection and is AOT-safe.
    /// </summary>
    /// <typeparam name="T">The state slice type. Must implement <see cref="IDeepCloneable{T}"/>.</typeparam>
    void RegisterSlice<T>() where T : class, IStateSlice, IDeepCloneable<T>;

    /// <summary>
    /// Deep-clones all registered state slices and returns an immutable snapshot.
    /// </summary>
    /// <param name="world">The world whose state slices will be captured.</param>
    /// <returns>An immutable <see cref="IStateView"/> containing the cloned slices.</returns>
    IStateView Capture(IWorld world);

    /// <summary>
    /// Restores world state from a snapshot by deep-cloning each slice back.
    /// The snapshot remains valid after this call (it is not consumed).
    /// </summary>
    /// <param name="world">The world to restore state into.</param>
    /// <param name="snapshot">The snapshot to restore from.</param>
    void RestoreTo(IWorld world, IStateView snapshot);

    /// <summary>
    /// Restores world state by moving slices directly from the snapshot without cloning.
    /// The snapshot is consumed and returned to the pool — it must not be accessed after this call.
    /// Use this when the snapshot will be discarded immediately (e.g., CQRS rollback).
    /// </summary>
    /// <param name="world">The world to restore state into.</param>
    /// <param name="snapshot">The snapshot to consume. Invalid after this call.</param>
    void RestoreAndConsume(IWorld world, IStateView snapshot);

    /// <summary>
    /// Returns a snapshot to the internal pool for reuse.
    /// Call this when the snapshot is no longer needed to avoid allocations on subsequent captures.
    /// After returning, the snapshot must not be accessed.
    /// </summary>
    void Return(IStateView snapshot);
}
