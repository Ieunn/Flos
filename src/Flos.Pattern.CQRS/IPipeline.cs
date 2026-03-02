using Flos.Core.Errors;

namespace Flos.Pattern.CQRS;

/// <summary>The CQRS command pipeline. Validates commands, applies events, and journals results.</summary>
public interface IPipeline
{
    /// <summary>Sends a command through the pipeline.</summary>
    /// <param name="command">The command to send.</param>
    /// <returns>Ok with produced events, or Fail with an ErrorCode. On failure, publishes <see cref="CommandRejectedMessage"/>.</returns>
    Result<IReadOnlyList<IEvent>> Send(ICommand command);
}
