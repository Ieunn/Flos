namespace Flos.Core.Messaging;

/// <summary>
/// Interceptor in the message pipeline, called before subscribers receive a published message.
/// </summary>
public interface IMessageMiddleware
{
    /// <summary>
    /// Handles a message and optionally forwards it to the next stage in the pipeline.
    /// </summary>
    /// <typeparam name="T">The type of message being handled.</typeparam>
    /// <param name="message">The message instance to handle.</param>
    /// <param name="next">Delegate that invokes the next middleware or dispatches to subscribers.</param>
    void Handle<T>(T message, Action<T> next) where T : IMessage;
}
