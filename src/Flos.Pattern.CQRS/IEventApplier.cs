using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>Mutates a state slice in response to a domain event.</summary>
/// <typeparam name="TEvent">The type of domain event this applier handles.</typeparam>
/// <typeparam name="TState">The type of state slice to mutate.</typeparam>
public interface IEventApplier<TEvent, TState>
    where TEvent : IEvent
    where TState : class, IStateSlice
{
    /// <summary>Applies the event to the state slice.</summary>
    /// <param name="state">The state slice to mutate.</param>
    /// <param name="evt">The domain event to apply.</param>
    void Apply(TState state, TEvent evt);
}
