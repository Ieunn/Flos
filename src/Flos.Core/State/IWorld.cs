namespace Flos.Core.State;

/// <summary>
/// Single source of truth for all game state.
/// Stores named <see cref="IStateSlice"/> instances keyed by type.
/// </summary>
public interface IWorld
{
    /// <summary>
    /// Retrieves the state slice of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The slice type.</typeparam>
    /// <returns>The registered slice instance.</returns>
    /// <exception cref="Flos.Core.Errors.FlosException">
    /// Thrown with <see cref="Flos.Core.Errors.CoreErrors.SliceNotFound"/> when no slice of that type is registered.
    /// </exception>
    T Get<T>() where T : class, IStateSlice;

    /// <summary>
    /// Registers an initial state slice.
    /// If a slice of the same type is already registered, it is overwritten and a warning is logged.
    /// </summary>
    /// <typeparam name="T">The slice type.</typeparam>
    /// <param name="initialState">The initial state slice instance to register.</param>
    void Register<T>(T initialState) where T : class, IStateSlice;

    /// <summary>
    /// Retrieves a state slice by its runtime type key.
    /// </summary>
    /// <param name="type">The type key of the slice.</param>
    /// <returns>The registered slice instance.</returns>
    /// <exception cref="Flos.Core.Errors.FlosException">
    /// Thrown with <see cref="Flos.Core.Errors.CoreErrors.SliceNotFound"/> when no slice of that type is registered.
    /// </exception>
    IStateSlice GetSlice(Type type);

    /// <summary>
    /// Replaces a previously registered slice (used by snapshot restore).
    /// </summary>
    /// <param name="type">The type key of the slice to replace.</param>
    /// <param name="slice">The new slice instance.</param>
    /// <exception cref="Flos.Core.Errors.FlosException">
    /// Thrown with <see cref="Flos.Core.Errors.CoreErrors.SliceNotFound"/> if no slice of that type is registered.
    /// </exception>
    void SetSlice(Type type, IStateSlice slice);

    /// <summary>
    /// Types of all registered slices, in registration order.
    /// </summary>
    IReadOnlyList<Type> RegisteredTypes { get; }
}
