using Flos.Core.Errors;
using Flos.Core.Messaging;

namespace Flos.Pattern.CQRS;

/// <summary>Published on the message bus when a command fails validation or application.</summary>
/// <param name="Command">The rejected command.</param>
/// <param name="Error">The error code describing the failure reason.</param>
public readonly record struct CommandRejectedMessage(ICommand Command, ErrorCode Error) : IMessage;
