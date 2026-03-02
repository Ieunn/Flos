using Flos.Core.Errors;

namespace Flos.Core.Module;

/// <summary>
/// Built-in minimal DI scope. Dictionary-backed, singleton-only.
/// No constructor injection, no decorator, no scoping.
/// </summary>
public sealed class BuiltInMinimalScope : IServiceScope
{
    private readonly Dictionary<Type, object> _instances = [];
    private readonly Dictionary<Type, Func<IServiceScope, object>> _factories = [];
    private bool _locked;
    private bool _disposed;
    private readonly HashSet<Type> _resolving = [];

    /// <inheritdoc />
    public bool IsLocked => _locked;

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the scope is locked.</exception>
    public void Register<TInterface, TImpl>() where TImpl : class, TInterface, new()
    {
        ThrowIfLocked();
        _factories[typeof(TInterface)] = _ => new TImpl();
    }

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the scope is locked.</exception>
    public void RegisterInstance<T>(T instance)
    {
        ThrowIfLocked();
        _instances[typeof(T)] = instance!;
    }

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the scope is locked.</exception>
    public void RegisterFactory<T>(Func<IServiceScope, T> factory)
    {
        ThrowIfLocked();
        _factories[typeof(T)] = scope => factory(scope)!;
    }

    /// <inheritdoc />
    public void Lock() => _locked = true;

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ServiceNotFound"/> if the service is not registered.</exception>
    public T Resolve<T>()
    {
        var key = typeof(T);

        if (_instances.TryGetValue(key, out var instance))
        {
            return (T)instance;
        }

        if (_factories.TryGetValue(key, out var factory))
        {
            if (!_resolving.Add(key))
            {
                throw new FlosException(CoreErrors.ServiceNotFound,
                    $"Circular dependency detected while resolving '{key.Name}'.");
            }

            try
            {
                var result = factory(this);
                _instances[key] = result;
                _factories.Remove(key);
                return (T)result;
            }
            finally
            {
                _resolving.Remove(key);
            }
        }

        throw new FlosException(CoreErrors.ServiceNotFound, $"Service '{key.Name}' is not registered.");
    }

    /// <inheritdoc />
    public bool IsRegistered<T>()
    {
        var key = typeof(T);
        return _instances.ContainsKey(key) || _factories.ContainsKey(key);
    }

    /// <summary>
    /// Disposes all registered instances that implement <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var instance in _instances.Values)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _instances.Clear();
        _factories.Clear();
    }

    private void ThrowIfLocked()
    {
        if (_locked)
            throw new FlosException(CoreErrors.ScopeAlreadyLocked, "Cannot register services after scope is locked.");
    }
}
