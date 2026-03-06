namespace Flos.Core.State;

/// <summary>
/// Read-only view of the world's state slices.
/// Both <see cref="IWorld"/> (live state) and snapshot views implement this interface,
/// enabling code to accept either without forcing a deep-clone.
/// </summary>
public interface IStateReader
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
    /// Attempts to retrieve the state slice of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The slice type.</typeparam>
    /// <param name="value">The slice if found, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the slice was found; otherwise, <see langword="false"/>.</returns>
    bool TryGet<T>(out T? value) where T : class, IStateSlice;

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
    /// Types of all registered slices, in registration order.
    /// </summary>
    IReadOnlyList<Type> RegisteredTypes { get; }
}
