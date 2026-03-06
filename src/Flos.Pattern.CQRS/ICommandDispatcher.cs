using Flos.Core.Errors;
using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Non-generic interface for type-erased dispatch.
/// One instance per command type, stored in <see cref="_dispatchers"/>.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Executes the handler with a boxed command.
    /// Single unbox inside — unavoidable when caller uses <c>Send(ICommand)</c>.
    /// </summary>
    ErrorCode Execute(ICommand command, IStateReader reader, EventBuffer events);
}