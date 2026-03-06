using System.Runtime.CompilerServices;
using Flos.Core.Errors;
using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Typed dispatcher that holds the handler without boxing.
/// Serves both <c>Send(ICommand)</c> and <c>Send&lt;T&gt;</c> paths.
/// </summary>
public sealed class CommandDispatcher<TCommand> : ICommandDispatcher
    where TCommand : ICommand
{
    internal ICommandHandler<TCommand> Handler = default!;

    /// <summary>
    /// Non-generic path: one unbox of the command, then fully typed.
    /// No closure allocation, no Func indirection.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ErrorCode Execute(ICommand command, IStateReader reader, EventBuffer events)
        => Handler.Handle((TCommand)command, reader, events);

    /// <summary>
    /// Generic path: zero boxing, zero unboxing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ErrorCode ExecuteTyped(TCommand command, IStateReader reader, EventBuffer events)
        => Handler.Handle(command, reader, events);
}