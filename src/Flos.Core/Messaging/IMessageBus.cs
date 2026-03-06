namespace Flos.Core.Messaging;

/// <summary>
/// Central pub/sub message bus used to publish and subscribe to messages across the framework.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to all subscribers of <typeparamref name="T"/>, passing through any registered middleware.
    /// </summary>
    /// <typeparam name="T">The type of message to publish.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    void Publish<T>(T message) where T : IMessage;

    /// <summary>
    /// Subscribes a handler to messages of type <typeparamref name="T"/> and returns a subscription ID.
    /// Use with <see cref="Unsubscribe{T}"/> to remove. Zero-allocation.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The callback invoked when a message of type <typeparamref name="T"/> is published.</param>
    /// <param name="priority">Subscription priority; lower values execute first. Defaults to 0.</param>
    /// <returns>A long subscription ID that can be passed to <see cref="Unsubscribe{T}"/>.</returns>
    long Subscribe<T>(Action<T> handler, int priority = 0) where T : IMessage;

    /// <summary>
    /// Removes a subscription by its ID.
    /// </summary>
    /// <typeparam name="T">The message type the subscription was registered for.</typeparam>
    /// <param name="subscriptionId">The ID returned by <see cref="Subscribe{T}"/>.</param>
    void Unsubscribe<T>(long subscriptionId) where T : IMessage;

    /// <summary>
    /// Subscribes a handler and returns an <see cref="IDisposable"/> token that removes the subscription when disposed.
    /// Convenience wrapper over <see cref="Subscribe{T}"/>; allocates a small token object.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The callback invoked when a message of type <typeparamref name="T"/> is published.</param>
    /// <param name="priority">Subscription priority; lower values execute first. Defaults to 0.</param>
    /// <returns>An <see cref="IDisposable"/> token that removes the subscription when disposed.</returns>
    IDisposable Listen<T>(Action<T> handler, int priority = 0) where T : IMessage;

    /// <summary>
    /// Registers a middleware interceptor that is invoked for every published message before it reaches subscribers.
    /// Must be called before the first <see cref="Publish{T}"/> call.
    /// </summary>
    /// <param name="middleware">The middleware to add to the pipeline.</param>
    void Use(IMessageMiddleware middleware);
}
