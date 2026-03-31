using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Provides state capture and rollback for the CQRS pipeline.
/// Implement this interface to enable <see cref="CQRSConfig.EnableRollback"/>.
/// A typical implementation wraps <see cref="ISnapshots"/>.
/// </summary>
public interface IRollbackProvider
{
    /// <summary>
    /// Captures a snapshot of the current world state before command handling.
    /// The returned <see cref="IStateReader"/> is passed to the command handler
    /// as a read-only view of the pre-command state.
    /// </summary>
    /// <param name="world">The live world state to capture.</param>
    /// <returns>A read-only snapshot of the world state.</returns>
    IStateReader Capture(IWorld world);

    /// <summary>
    /// Restores the world state from a previously captured snapshot.
    /// Called when an event applier fails and rollback is needed.
    /// </summary>
    /// <param name="world">The world to restore.</param>
    /// <param name="snapshot">The snapshot previously returned by <see cref="Capture"/>.</param>
    void RestoreTo(IWorld world, IStateReader snapshot);

    /// <summary>
    /// Returns a snapshot to the provider for potential reuse/pooling.
    /// Called by the pipeline when the snapshot is no longer needed (both success and failure paths).
    /// Implementations that pool snapshots should reclaim resources here.
    /// The default does nothing — implementations without pooling can ignore this.
    /// </summary>
    void Release(IStateReader snapshot) { }
}
