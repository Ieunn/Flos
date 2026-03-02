namespace Flos.Snapshot;

/// <summary>
/// Implemented by IStateSlice types that participate in snapshots.
/// In Phase 2 implementations are hand-written; Flos.Generators will auto-generate later.
/// </summary>
public interface IDeepCloneable<out T>
{
    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    /// <returns>A new instance that is a deep clone of this object.</returns>
    T DeepClone();
}
