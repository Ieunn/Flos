using System.Collections.Concurrent;
using Flos.Core.Errors;
using Flos.Core.Logging;

namespace Flos.Core.Scheduling;

/// <summary>
/// Default implementation of <see cref="IDispatcher"/>. Uses a double-buffered
/// <see cref="ConcurrentQueue{T}"/> for lock-free enqueue from worker threads.
/// </summary>
public sealed class Dispatcher : IDispatcher
{
    private ConcurrentQueue<Action> _incoming = new();
    private ConcurrentQueue<Action> _draining = new();

    /// <summary>
    /// ThreadGuard for consumer. Only used in DrainAll.
    /// </summary>
    private readonly ThreadGuard _threadGuard = new("Dispatcher");

    public Action<Exception>? OnActionException { get; set; }

    public void Enqueue(Action action) => _incoming.Enqueue(action);

    public void DrainAll()
    {
        _threadGuard.Assert();

        var toDrain = Interlocked.Exchange(
            ref _incoming,
            Volatile.Read(ref _draining));

        List<Exception>? exceptions = null;
        while (toDrain.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (OnActionException is not null)
                {
                    OnActionException(ex);
                }
                else
                {
                    CoreLog.Error($"Dispatcher action threw: {ex.Message}");
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }
        }

        Volatile.Write(ref _draining, toDrain);

        if (exceptions is not null)
        {
            var inner = exceptions.Count == 1
                ? exceptions[0]
                : new AggregateException(exceptions);
            throw new FlosException(CoreErrors.HandlerException,
                $"{exceptions.Count} dispatched action(s) threw: {exceptions[0].Message}",
                inner);
        }
    }
}