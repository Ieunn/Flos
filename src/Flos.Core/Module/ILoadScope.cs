using Flos.Core.Messaging;
using Flos.Core.Scheduling;
using Flos.Core.Sessions;
using Flos.Core.State;

namespace Flos.Core.Module;

/// <summary>
/// Narrowed view of the service registry available during <see cref="IModule.OnLoad"/>.
/// Exposes registration methods and pre-registered infrastructure, but hides
/// <c>Resolve&lt;T&gt;()</c> to prevent ordering-dependent service resolution.
/// </summary>
public interface ILoadScope
{
    /// <summary>
    /// The underlying registry. Should not be directly accessed except for module initialization.
    /// </summary>
    IServiceRegistry Registry { get; }

    /// <summary>Pre-registered world state container.</summary>
    IWorld World { get; }

    /// <summary>Pre-registered message bus.</summary>
    IMessageBus Bus { get; }

    /// <summary>Pre-registered scheduler.</summary>
    IScheduler Scheduler { get; }

    /// <summary>Pre-registered dispatcher for cross-thread access.</summary>
    IDispatcher Dispatcher { get; }

    /// <summary>Pre-registered pattern registry.</summary>
    IPatternRegistry Patterns { get; }

    /// <summary>Session configuration.</summary>
    SessionConfig Config { get; }

    /// <inheritdoc cref="IServiceRegistry.Register{TInterface, TImpl}"/>
    void Register<TInterface, TImpl>() where TImpl : class, TInterface, new();

    /// <inheritdoc cref="IServiceRegistry.Register{T}(T)"/>
    void Register<T>(T instance);

    /// <inheritdoc cref="IServiceRegistry.Register{T}(Func{IServiceRegistry, T})"/>
    void Register<T>(Func<IServiceRegistry, T> factory);

    /// <inheritdoc cref="IServiceRegistry.TryRegister{T}(T)"/>
    bool TryRegister<T>(T instance);

    /// <inheritdoc cref="IServiceRegistry.TryRegister{TInterface, TImpl}"/>
    bool TryRegister<TInterface, TImpl>() where TImpl : class, TInterface, new();

    /// <inheritdoc cref="IServiceRegistry.TryRegister{T}(Func{IServiceRegistry, T})"/>
    bool TryRegister<T>(Func<IServiceRegistry, T> factory);

    /// <inheritdoc cref="IServiceRegistry.IsRegistered{T}"/>
    bool IsRegistered<T>();
}
