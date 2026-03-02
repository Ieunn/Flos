using System.Collections.Concurrent;

namespace Flos.Core.Scheduling;

/// <summary>
/// Default implementation of <see cref="IDispatcher"/>. Uses a double-buffered
/// <see cref="ConcurrentQueue{T}"/> for lock-free enqueue from worker threads.
/// </summary>
public sealed class Dispatcher : IDispatcher
{
    private ConcurrentQueue<Action> _incoming = new();
    private ConcurrentQueue<Action> _draining = new();

    /// <inheritdoc />
    public void Enqueue(Action action) => _incoming.Enqueue(action);

    /// <inheritdoc />
    public void DrainAll()
    {
        var toDrain = Interlocked.Exchange(ref _incoming, _draining);

        while (toDrain.TryDequeue(out var action))
        {
            action();
        }

        _draining = toDrain;
    }
}
