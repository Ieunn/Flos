using System.Diagnostics;
using System.Runtime.CompilerServices;
using Flos.Core.Errors;
using Flos.Core.Logging;

namespace Flos.Core.State;

/// <summary>
/// Default implementation of <see cref="IWorld"/>.
/// List-backed state container with deterministic iteration.
/// Typical slice count is &lt; 20 — linear search is cache-friendly and sufficient.
/// </summary>
public sealed class World : IWorld
{
    private readonly List<Type> _types = [];
    private readonly List<IStateSlice> _slices = [];
    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;

    [Conditional("DEBUG")]
    private void AssertMainThread([CallerMemberName] string? caller = null)
    {
        Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId,
            $"World.{caller}() called from thread {Environment.CurrentManagedThreadId}, " +
            $"expected main thread {_ownerThreadId}. Use IDispatcher.Enqueue() for cross-thread access.");
    }

    /// <summary>
    /// Retrieves the state slice of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The slice type.</typeparam>
    /// <returns>The registered slice instance.</returns>
    /// <exception cref="FlosException">
    /// Thrown with <see cref="CoreErrors.SliceNotFound"/> when no slice of that type is registered.
    /// </exception>
    public T Get<T>() where T : class, IStateSlice
    {
        AssertMainThread();
        var target = typeof(T);
        for (int i = 0; i < _types.Count; i++)
        {
            if (_types[i] == target)
                return (T)_slices[i];
        }
        throw new FlosException(CoreErrors.SliceNotFound, $"State slice '{typeof(T).Name}' is not registered.");
    }

    /// <summary>
    /// Registers an initial state slice.
    /// If a slice of the same type is already registered, it is overwritten and a warning is logged.
    /// </summary>
    /// <typeparam name="T">The slice type.</typeparam>
    /// <param name="initialState">The initial state slice instance to register.</param>
    public void Register<T>(T initialState) where T : class, IStateSlice
    {
        AssertMainThread();
        var type = typeof(T);
        for (int i = 0; i < _types.Count; i++)
        {
            if (_types[i] == type)
            {
                CoreLog.Warn($"State slice '{type.Name}' is already registered. Overwriting.");
                _slices[i] = initialState;
                return;
            }
        }
        _types.Add(type);
        _slices.Add(initialState);
    }

    /// <summary>
    /// Retrieves a state slice by its runtime type key.
    /// </summary>
    /// <param name="type">The type key of the slice.</param>
    /// <returns>The registered slice instance.</returns>
    /// <exception cref="FlosException">
    /// Thrown with <see cref="CoreErrors.SliceNotFound"/> when no slice of that type is registered.
    /// </exception>
    public IStateSlice GetSlice(Type type)
    {
        AssertMainThread();
        for (int i = 0; i < _types.Count; i++)
        {
            if (_types[i] == type)
                return _slices[i];
        }
        throw new FlosException(CoreErrors.SliceNotFound, $"State slice '{type.Name}' is not registered.");
    }

    /// <summary>
    /// Replaces a previously registered slice (used by snapshot restore).
    /// </summary>
    /// <param name="type">The type key of the slice to replace.</param>
    /// <param name="slice">The new slice instance.</param>
    /// <exception cref="FlosException">
    /// Thrown with <see cref="CoreErrors.SliceNotFound"/> if no slice of that type is registered.
    /// </exception>
    public void SetSlice(Type type, IStateSlice slice)
    {
        AssertMainThread();
        for (int i = 0; i < _types.Count; i++)
        {
            if (_types[i] == type)
            {
                _slices[i] = slice;
                return;
            }
        }
        throw new FlosException(CoreErrors.SliceNotFound, $"State slice '{type.Name}' is not registered. Use Register<T> first.");
    }

    /// <summary>
    /// Types of all registered slices, in registration order.
    /// </summary>
    public IReadOnlyList<Type> RegisteredTypes => _types;
}
