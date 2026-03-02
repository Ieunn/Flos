using Flos.Core.State;

namespace Flos.Snapshot;

/// <summary>
/// Creates and restores deep-copy snapshots of the world state.
/// </summary>
public interface ISnapshotManager
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
    /// </summary>
    /// <param name="world">The world to restore state into.</param>
    /// <param name="snapshot">The snapshot to restore from.</param>
    void RestoreTo(IWorld world, IStateView snapshot);
}
