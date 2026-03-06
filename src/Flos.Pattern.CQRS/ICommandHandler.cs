using Flos.Core.Errors;
using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>Validates a command against a read-only state view and produces zero or more events on success.</summary>
/// <typeparam name="TCommand">The type of command this handler processes.</typeparam>
public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Processes the command. On success, appends events to <paramref name="events"/> and returns
    /// a default (successful) <see cref="ErrorCode"/>. On rejection, returns a non-default error code.
    /// </summary>
    /// <param name="command">The command to process.</param>
    /// <param name="state">Read-only view of world state.</param>
    /// <param name="events">Buffer to append produced events into. Owned by the pipeline; do not cache.</param>
    /// <returns>A default <see cref="ErrorCode"/> on success, or a non-default error code on rejection.</returns>
    ErrorCode Handle(TCommand command, IStateReader state, EventBuffer events);
}
