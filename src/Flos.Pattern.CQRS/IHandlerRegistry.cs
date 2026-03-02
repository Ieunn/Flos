namespace Flos.Pattern.CQRS;

/// <summary>Registration point for command handlers and event appliers.</summary>
public interface IHandlerRegistry
{
    /// <summary>Registers a command handler for the given command type.</summary>
    /// <typeparam name="TCommand">The command type the handler processes.</typeparam>
    /// <param name="handler">The command handler to register.</param>
    void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand;

    /// <summary>Registers an event applier that mutates state in response to events.</summary>
    /// <typeparam name="TEvent">The event type the applier handles.</typeparam>
    /// <typeparam name="TState">The state slice type the applier mutates.</typeparam>
    /// <param name="applier">The event applier to register.</param>
    void Register<TEvent, TState>(IEventApplier<TEvent, TState> applier)
        where TEvent : IEvent
        where TState : class, Flos.Core.State.IStateSlice;
}
