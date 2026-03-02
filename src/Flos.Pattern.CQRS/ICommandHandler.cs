using Flos.Core.Errors;
using Flos.Snapshot;

namespace Flos.Pattern.CQRS;

/// <summary>Validates a command against a read-only state view and produces zero or more events on success.</summary>
/// <typeparam name="TCommand">The type of command this handler processes.</typeparam>
public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    /// <summary>Processes the command and returns a list of events or a failure.</summary>
    /// <param name="command">The command to process.</param>
    /// <param name="state">Read-only snapshot of world state.</param>
    /// <returns>Ok with events on success, or Fail with an ErrorCode on rejection.</returns>
    Result<IReadOnlyList<IEvent>> Handle(TCommand command, IStateView state);
}
