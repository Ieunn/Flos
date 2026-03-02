using Flos.Core.State;

namespace Flos.Snapshot;

/// <summary>
/// Read-only access to a point-in-time snapshot of state slices.
/// </summary>
public interface IStateView
{
    /// <summary>
    /// Retrieves a state slice of the specified type from the snapshot.
    /// </summary>
    /// <typeparam name="T">The state slice type to retrieve.</typeparam>
    /// <returns>The state slice instance.</returns>
    /// <exception cref="Flos.Core.Errors.FlosException">Thrown with <see cref="SnapshotErrors.SliceNotFound"/> when the slice is not present.</exception>
    T Get<T>() where T : class, IStateSlice;
}
