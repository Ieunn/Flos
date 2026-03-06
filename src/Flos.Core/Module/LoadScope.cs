using Flos.Core.Messaging;
using Flos.Core.Scheduling;
using Flos.Core.Sessions;
using Flos.Core.State;

namespace Flos.Core.Module;

/// <summary>
/// Default implementation of <see cref="ILoadScope"/> that wraps an <see cref="IServiceRegistry"/>
/// and pre-registered infrastructure instances.
/// </summary>
public sealed class LoadScope : ILoadScope
{
    private readonly IServiceRegistry _registry;

    public LoadScope(
        IServiceRegistry registry,
        IWorld world,
        IMessageBus bus,
        IScheduler scheduler,
        IDispatcher dispatcher,
        IPatternRegistry patterns,
        SessionConfig config)
    {
        _registry = registry;
        World = world;
        Bus = bus;
        Scheduler = scheduler;
        Dispatcher = dispatcher;
        Patterns = patterns;
        Config = config;
    }

    /// <inheritdoc />
    public IWorld World { get; }

    /// <inheritdoc />
    public IMessageBus Bus { get; }

    /// <inheritdoc />
    public IScheduler Scheduler { get; }

    /// <inheritdoc />
    public IDispatcher Dispatcher { get; }

    /// <inheritdoc />
    public IPatternRegistry Patterns { get; }

    /// <inheritdoc />
    public SessionConfig Config { get; }

    /// <inheritdoc />
    public void Register<TInterface, TImpl>() where TImpl : class, TInterface, new()
        => _registry.Register<TInterface, TImpl>();

    /// <inheritdoc />
    public void Register<T>(T instance) => _registry.Register(instance);

    /// <inheritdoc />
    public void Register<T>(Func<IServiceRegistry, T> factory) => _registry.Register(factory);

    /// <inheritdoc />
    public bool TryRegister<T>(T instance) => _registry.TryRegister(instance);

    /// <inheritdoc />
    public bool TryRegister<TInterface, TImpl>() where TImpl : class, TInterface, new()
        => _registry.TryRegister<TInterface, TImpl>();

    /// <inheritdoc />
    public bool TryRegister<T>(Func<IServiceRegistry, T> factory) => _registry.TryRegister(factory);

    /// <inheritdoc />
    public bool IsRegistered<T>() => _registry.IsRegistered<T>();

    /// <inheritdoc />
    public IServiceRegistry Registry => _registry;
}
