namespace Flos.Core.State;

/// <summary>
/// Single source of truth for all game state.
/// Stores named <see cref="IStateSlice"/> instances keyed by type.
/// Extends <see cref="IStateReader"/> with mutation operations.
/// </summary>
public interface IWorld : IStateReader
{
    /// <summary>
    /// Registers an initial state slice.
    /// If a slice of the same type is already registered, it is overwritten and a warning is logged.
    /// </summary>
    /// <typeparam name="T">The slice type.</typeparam>
    /// <param name="initialState">The initial state slice instance to register.</param>
    void Register<T>(T initialState) where T : class, IStateSlice;

    /// <summary>
    /// Removes a previously registered state slice.
    /// If the slice implements <see cref="System.IDisposable"/>, it is disposed.
    /// </summary>
    /// <typeparam name="T">The slice type to remove.</typeparam>
    void Unregister<T>() where T : class, IStateSlice;

    /// <summary>
    /// Replaces a previously registered slice (used by snapshot restore).
    /// </summary>
    /// <param name="type">The type key of the slice to replace.</param>
    /// <param name="slice">The new slice instance.</param>
    /// <exception cref="Flos.Core.Errors.FlosException">
    /// Thrown with <see cref="Flos.Core.Errors.CoreErrors.SliceNotFound"/> if no slice of that type is registered.
    /// </exception>
    void SetSlice(Type type, IStateSlice slice);
}
