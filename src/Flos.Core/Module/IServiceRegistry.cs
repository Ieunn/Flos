using Flos.Core.Errors;

namespace Flos.Core.Module;

/// <summary>
/// Service registry that holds singleton registrations for a session.
/// Supports two-phase lifecycle: register services, then lock and resolve.
/// </summary>
public interface IServiceRegistry : IDisposable
{
    /// <summary>
    /// Registers a type mapping. The implementation is instantiated lazily on first resolve.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TImpl">The concrete implementation type.</typeparam>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the registry is locked.</exception>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.DuplicateRegistration"/> if the service is already registered.</exception>
    void Register<TInterface, TImpl>() where TImpl : class, TInterface, new();

    /// <summary>
    /// Registers a pre-created instance.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the registry is locked.</exception>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.DuplicateRegistration"/> if the service is already registered.</exception>
    void Register<T>(T instance);

    /// <summary>
    /// Registers a factory delegate. The result is cached as a singleton.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="factory">A factory that receives the registry and returns the service instance.</param>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the registry is locked.</exception>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.DuplicateRegistration"/> if the service is already registered.</exception>
    void Register<T>(Func<IServiceRegistry, T> factory);

    /// <summary>
    /// Registers a pre-created instance only if no registration for <typeparamref name="T"/> exists.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <returns><see langword="true"/> if the registration was added; <see langword="false"/> if a registration already existed.</returns>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the registry is locked.</exception>
    bool TryRegister<T>(T instance);

    /// <summary>
    /// Registers a type mapping only if no registration for <typeparamref name="TInterface"/> exists.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TImpl">The concrete implementation type.</typeparam>
    /// <returns><see langword="true"/> if the registration was added; <see langword="false"/> if a registration already existed.</returns>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the registry is locked.</exception>
    bool TryRegister<TInterface, TImpl>() where TImpl : class, TInterface, new();

    /// <summary>
    /// Registers a factory delegate only if no registration for <typeparamref name="T"/> exists.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="factory">A factory that receives the registry and returns the service instance.</param>
    /// <returns><see langword="true"/> if the registration was added; <see langword="false"/> if a registration already existed.</returns>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the registry is locked.</exception>
    bool TryRegister<T>(Func<IServiceRegistry, T> factory);

    /// <summary>
    /// Prevents further registrations. Called after all modules' <see cref="IModule.OnLoad"/> completes.
    /// </summary>
    void Lock();

    /// <summary>
    /// Resolves a service. Returns the cached instance or invokes the registered factory.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ServiceNotFound"/> if the service is not registered.</exception>
    T Resolve<T>();

    /// <summary>
    /// Attempts to resolve a service. Returns <see langword="false"/> if not registered.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <param name="value">The resolved service instance, or <see langword="default"/> if not found.</param>
    /// <returns><see langword="true"/> if the service was resolved; otherwise, <see langword="false"/>.</returns>
    bool TryResolve<T>(out T? value);

    /// <summary>
    /// Returns <see langword="true"/> if a service of type <typeparamref name="T"/> is registered.
    /// </summary>
    /// <typeparam name="T">The service type to check.</typeparam>
    /// <returns><see langword="true"/> if the service is registered; otherwise, <see langword="false"/>.</returns>
    bool IsRegistered<T>();

    /// <summary>
    /// <see langword="true"/> after <see cref="Lock"/> has been called.
    /// </summary>
    bool IsLocked { get; }
}
