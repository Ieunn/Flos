using Flos.Core.Messaging;

namespace Flos.Pattern.ECS;

/// <summary>
/// Deferred action queue for ECS systems to safely enqueue MessageBus publishes
/// outside the main thread. Messages are drained after the tick completes,
/// before the next tick's IDispatcher.DrainAll().
/// </summary>
public interface ICommandBuffer
{
    /// <summary>
    /// Enqueues a message to be published on the MessageBus after the current tick completes.
    /// Thread-safe: can be called from ECS worker threads.
    /// </summary>
    void PublishAfterTick<T>(T message) where T : IMessage;
}
