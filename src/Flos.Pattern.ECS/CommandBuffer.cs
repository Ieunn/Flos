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
    private readonly ConcurrentQueue<IBufferedMessage> _queue = new();

    /// <summary>
    /// Enqueues a message to be published after the current tick.
    /// Thread-safe: can be called from any thread.
    /// Uses pooled holders to avoid per-call closure allocations.
    /// </summary>
    public void PublishAfterTick<T>(T message) where T : IMessage
    {
        _queue.Enqueue(BufferedMessage<T>.Rent(message));
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
            entry.PublishAndReturn(bus);
            count++;
        }
        return count;
    }
}

/// <summary>
/// Type-erased buffered message for zero-allocation deferred publish.
/// </summary>
internal interface IBufferedMessage
{
    void PublishAndReturn(IMessageBus bus);
}

/// <summary>
/// Pooled typed holder that publishes via the generic <c>bus.Publish&lt;T&gt;</c> path (no boxing).
/// Thread-safe pool: uses a <see cref="ConcurrentStack{T}"/> since Rent/Return may happen on different threads.
/// </summary>
internal sealed class BufferedMessage<T> : IBufferedMessage where T : IMessage
{
    private const int MaxPoolSize = 64;

    private static readonly ConcurrentStack<BufferedMessage<T>> Pool = new();

    private T _message = default!;

    internal static BufferedMessage<T> Rent(T message)
    {
        if (Pool.TryPop(out var holder))
        {
            holder._message = message;
            return holder;
        }
        return new BufferedMessage<T> { _message = message };
    }

    public void PublishAndReturn(IMessageBus bus)
    {
        var msg = _message;
        _message = default!;
        bus.Publish(msg);
        if (Pool.Count < MaxPoolSize)
            Pool.Push(this);
    }
}
