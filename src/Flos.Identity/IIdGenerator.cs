namespace Flos.Identity;

/// <summary>
/// Generates unique <see cref="EntityId"/> values.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Returns the next unique entity identifier.
    /// </summary>
    /// <returns>A new, unique <see cref="EntityId"/>.</returns>
    EntityId Next();

    /// <summary>
    /// Gets the current internal state for snapshot/restore support.
    /// The meaning of the value is implementation-defined.
    /// </summary>
    long GetState();

    /// <summary>
    /// Restores the generator to a previously captured state.
    /// After calling this, the next generated ID will follow from the restored state.
    /// </summary>
    /// <param name="state">A value previously obtained from <see cref="GetState"/>.</param>
    void RestoreState(long state);
}
