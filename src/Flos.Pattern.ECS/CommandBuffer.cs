using System.Collections.Concurrent;
using Flos.Core.Messaging;

namespace Flos.Pattern.ECS;

/// <summary>
/// Thread-safe deferred message queue. ECS worker threads call PublishAfterTick
/// to enqueue messages during a tick. The ECS module drains the buffer after
/// the adapter's Tick() completes, publishing all deferred messages on the
/// MessageBus in FIFO order.
/// </summary>
public sealed class CommandBuffer : ICommandBuffer
{
    private readonly ConcurrentQueue<DeferredMessage> _queue = new();

    private static readonly ConcurrentDictionary<Type, Action<IMessageBus, IMessage>> _publishers = new();

    /// <summary>
    /// Enqueues a message to be published after the current tick.
    /// Thread-safe: can be called from any thread.
    /// Zero closure allocation: the message is boxed as IMessage (value types incur one boxing)
    /// and a cached typed delegate handles the Publish dispatch.
    /// </summary>
    public void PublishAfterTick<T>(T message) where T : IMessage
    {
        var publisher = _publishers.GetOrAdd(typeof(T),
            static _ => CreatePublisher<T>());
        _queue.Enqueue(new DeferredMessage(message, publisher));
    }

    /// <summary>
    /// Drains all queued messages, publishing them on the given bus in FIFO order.
    /// Must be called from the main thread only.
    /// Returns the number of messages drained.
    /// </summary>
    internal int Drain(IMessageBus bus)
    {
        int count = 0;
        while (_queue.TryDequeue(out var entry))
        {
            entry.Publish(bus, entry.Message);
            count++;
        }
        return count;
    }

    private static Action<IMessageBus, IMessage> CreatePublisher<T>() where T : IMessage
    {
        return static (bus, msg) => bus.Publish((T)msg);
    }

    private readonly record struct DeferredMessage(IMessage Message, Action<IMessageBus, IMessage> Publish);
}
