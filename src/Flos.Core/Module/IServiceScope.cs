using Flos.Core.Errors;

namespace Flos.Core.Module;

/// <summary>
/// Service locator that holds singleton registrations for a session.
/// </summary>
public interface IServiceScope : IDisposable
{
    /// <summary>
    /// Registers a type mapping. The implementation is instantiated lazily on first resolve.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TImpl">The concrete implementation type.</typeparam>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the scope is locked.</exception>
    void Register<TInterface, TImpl>() where TImpl : class, TInterface, new();

    /// <summary>
    /// Registers a pre-created instance.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the scope is locked.</exception>
    void RegisterInstance<T>(T instance);

    /// <summary>
    /// Registers a factory delegate. The result is cached as a singleton.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="factory">A factory that receives the scope and returns the service instance.</param>
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.ScopeAlreadyLocked"/> if the scope is locked.</exception>
    void RegisterFactory<T>(Func<IServiceScope, T> factory);

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
