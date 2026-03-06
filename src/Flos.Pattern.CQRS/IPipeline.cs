using Flos.Core.Errors;

namespace Flos.Pattern.CQRS;

/// <summary>The CQRS command pipeline. Validates commands, applies events, and journals results.</summary>
public interface IPipeline
{
    /// <summary>
    /// Number of subscriber exceptions swallowed during the last <see cref="Send"/> call's
    /// event publication phase. Reset to 0 at the start of each Send.
    /// Non-zero indicates that some subscribers did not receive events despite successful state mutation.
    /// </summary>
    int PublishFaultCount { get; }

    /// <summary>Sends a command through the pipeline.</summary>
    /// <param name="command">The command to send.</param>
    /// <returns>
    /// Ok with produced events, or Fail with an ErrorCode. On failure, publishes <see cref="CommandRejectedMessage"/>.
    /// <para><b>Important:</b> The returned <see cref="EventBuffer"/> is owned by the pipeline and is
    /// invalidated on the next <c>Send</c> call. Do not store the reference across multiple sends.
    /// Read the events or copy the data you need before calling <c>Send</c> again.</para>
    /// </returns>
    Result<EventBuffer> Send(ICommand command);

    /// <summary>
    /// Sends a command through the pipeline without boxing.
    /// Prefer this overload when the concrete command type is known at the call site.
    /// </summary>
    /// <typeparam name="TCommand">The concrete command type.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <returns>
    /// Ok with produced events, or Fail with an ErrorCode. On failure, publishes <see cref="CommandRejectedMessage"/>.
    /// <para><b>Important:</b> The returned <see cref="EventBuffer"/> is owned by the pipeline and is
    /// invalidated on the next <c>Send</c> call. Do not store the reference across multiple sends.
    /// Read the events or copy the data you need before calling <c>Send</c> again.</para>
    /// </returns>
    Result<EventBuffer> Send<TCommand>(TCommand command) where TCommand : ICommand;
}
