using Flos.Core.Errors;
using Flos.Core.Logging;

namespace Flos.Core.Module;

/// <summary>
/// Built-in minimal service registry. Dictionary-backed, singleton-only.
/// No constructor injection, no decorator, no scoping.
/// Uses TypedEntry&lt;T&gt; to avoid boxing value-type services.
/// </summary>
public sealed class BuiltInMinimalScope : IServiceRegistry
{
    private readonly Dictionary<Type, IEntry> _entries = new Dictionary<Type, IEntry>();
    private readonly List<IEntry> _registrationOrder = new List<IEntry>();
    private readonly ThreadGuard _threadGuard = new("ServiceRegistry");
    private bool _locked;
    private bool _disposed;

    /// <summary>Non-generic entry contract for storage and lifecycle.</summary>
    private interface IEntry
    {
        bool IsResolving { get; set; }
        void DisposeIfNeeded();
    }

    /// <summary>
    /// Typed holder that stores T without boxing.
    /// One instance per registered service type.
    /// </summary>
    private sealed class TypedEntry<T> : IEntry
    {
        public T Value = default!;
        public Func<IServiceRegistry, T>? Factory;
        public bool HasInstance;
        public bool IsResolving { get; set; }

        public void DisposeIfNeeded()
        {
            if (HasInstance && Value is IDisposable disposable)
                disposable.Dispose();
        }
    }

    /// <inheritdoc />
    public bool IsLocked => _locked;

    /// <inheritdoc />
    public void Register<TInterface, TImpl>() where TImpl : class, TInterface, new()
    {
        _threadGuard.Assert();
        ThrowIfLocked();
        var key = typeof(TInterface);
        ThrowIfDuplicate(key);
        var entry = new TypedEntry<TInterface> { Factory = _ => new TImpl() };
        _entries[key] = entry;
        _registrationOrder.Add(entry);
    }

    /// <inheritdoc />
    public void Register<T>(T instance)
    {
        _threadGuard.Assert();
        ThrowIfLocked();
        ArgumentNullException.ThrowIfNull(instance);
        var key = typeof(T);
        ThrowIfDuplicate(key);
        var entry = new TypedEntry<T> { Value = instance, HasInstance = true };
        _entries[key] = entry;
        _registrationOrder.Add(entry);
    }

    /// <inheritdoc />
    public void Register<T>(Func<IServiceRegistry, T> factory)
    {
        _threadGuard.Assert();
        ThrowIfLocked();
        var key = typeof(T);
        ThrowIfDuplicate(key);
        var entry = new TypedEntry<T> { Factory = factory };
        _entries[key] = entry;
        _registrationOrder.Add(entry);
    }

    /// <inheritdoc />
    public bool TryRegister<TInterface, TImpl>() where TImpl : class, TInterface, new()
    {
        _threadGuard.Assert();
        ThrowIfLocked();
        var key = typeof(TInterface);
        if (_entries.ContainsKey(key)) return false;
        var entry = new TypedEntry<TInterface> { Factory = _ => new TImpl() };
        _entries[key] = entry;
        _registrationOrder.Add(entry);
        return true;
    }

    /// <inheritdoc />
    public bool TryRegister<T>(T instance)
    {
        _threadGuard.Assert();
        ThrowIfLocked();
        ArgumentNullException.ThrowIfNull(instance);
        var key = typeof(T);
        if (_entries.ContainsKey(key)) return false;
        var entry = new TypedEntry<T> { Value = instance, HasInstance = true };
        _entries[key] = entry;
        _registrationOrder.Add(entry);
        return true;
    }

    /// <inheritdoc />
    public bool TryRegister<T>(Func<IServiceRegistry, T> factory)
    {
        _threadGuard.Assert();
        ThrowIfLocked();
        var key = typeof(T);
        if (_entries.ContainsKey(key)) return false;
        var entry = new TypedEntry<T> { Factory = factory };
        _entries[key] = entry;
        _registrationOrder.Add(entry);
        return true;
    }

    /// <inheritdoc />
    public void Lock()
    {
        _threadGuard.Assert();
        _locked = true;
    }

    /// <inheritdoc />
    public T Resolve<T>()
    {
        _threadGuard.Assert();
        var key = typeof(T);

        if (!_entries.TryGetValue(key, out var entryObj))
            throw new FlosException(CoreErrors.ServiceNotFound, $"Service '{key.Name}' is not registered.");

        var entry = (TypedEntry<T>)entryObj;

        if (entry.HasInstance)
            return entry.Value;

        if (entry.Factory is null)
            throw new FlosException(CoreErrors.ServiceNotFound, $"Service '{key.Name}' is not registered.");

        if (entry.IsResolving)
            throw new FlosException(CoreErrors.CircularDependency,
                $"Circular dependency detected while resolving '{key.Name}'.");

        entry.IsResolving = true;
        try
        {
            var result = entry.Factory(this);
            entry.Value = result;
            entry.HasInstance = true;
            entry.Factory = null; // promote to instance, release factory
            return result;
        }
        finally
        {
            entry.IsResolving = false;
        }
    }

    /// <inheritdoc />
    public bool TryResolve<T>(out T? value)
    {
        _threadGuard.Assert();
        var key = typeof(T);

        if (!_entries.TryGetValue(key, out var entryObj))
        {
            value = default;
            return false;
        }

        var entry = (TypedEntry<T>)entryObj;

        if (entry.HasInstance)
        {
            value = entry.Value;
            return true;
        }

        if (entry.Factory is null || entry.IsResolving)
        {
            value = default;
            return false;
        }

        entry.IsResolving = true;
        try
        {
            var result = entry.Factory(this);
            entry.Value = result;
            entry.HasInstance = true;
            entry.Factory = null;
            value = result;
            return true;
        }
        catch (Exception ex)
        {
            CoreLog.Warn($"Factory for '{key.Name}' threw during TryResolve: {ex.Message}");
            value = default;
            return false;
        }
        finally
        {
            entry.IsResolving = false;
        }
    }

    /// <inheritdoc />
    public bool IsRegistered<T>()
    {
        _threadGuard.Assert();
        return _entries.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Disposes all registered instances that implement <see cref="IDisposable"/>,
    /// in reverse registration order.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        for (int i = _registrationOrder.Count - 1; i >= 0; i--)
        {
            try { _registrationOrder[i].DisposeIfNeeded(); }
            catch ( Exception ex ) { CoreLog.Warn($"Service disposal failed: {ex.Message}"); }
        }
        _entries.Clear();
        _registrationOrder.Clear();
    }

    private void ThrowIfLocked()
    {
        if (_locked)
            throw new FlosException(CoreErrors.ScopeAlreadyLocked, "Cannot register services after registry is locked.");
    }

    private void ThrowIfDuplicate(Type key)
    {
        if (_entries.ContainsKey(key))
            throw new FlosException(CoreErrors.DuplicateRegistration, $"Service '{key.Name}' is already registered.");
    }
}