using Flos.Core.Errors;

namespace Flos.Snapshot;

/// <summary>
/// Error codes for Flos.Snapshot (Category 300 = Modules).
/// </summary>
public static class SnapshotErrors
{
    /// <summary>
    /// FLOS-300-0001. A requested state slice type was not found in the snapshot.
    /// </summary>
    public static readonly ErrorCode SliceNotFound = new(300, 1);

    /// <summary>
    /// FLOS-300-0002. A state slice does not implement <see cref="IDeepCloneable{T}"/>.
    /// </summary>
    public static readonly ErrorCode NotCloneable = new(300, 2);
}
