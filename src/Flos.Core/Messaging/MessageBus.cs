using System.Runtime.CompilerServices;
using Flos.Core.Annotations;
using Flos.Core.Errors;
using Flos.Core.Logging;

namespace Flos.Core.Messaging;

/// <summary>
/// Default implementation of <see cref="IMessageBus"/> with priority-ordered subscriptions and middleware pipeline.
/// </summary>
public sealed class MessageBus : IMessageBus, IDisposable
{
    private readonly Dictionary<Type, object> _subscriptionLists = new Dictionary<Type, object>();
    private readonly List<IMessageMiddleware> _middlewares = new List<IMessageMiddleware>();
    private readonly Dictionary<Type, object> _runnerCache = new Dictionary<Type, object>();
    private bool _hasPublished;
    private readonly ThreadGuard _threadGuard = new("MessageBus");

    /// <summary>
    /// Optional callback invoked when a handler throws during <see cref="Publish{T}"/>.
    /// If <see langword="null"/> (default), the first exception is collected and rethrown after all handlers have run.
    /// Set this to a custom callback to handle exceptions differently (e.g., log-and-continue).
    /// </summary>
    public Action<Exception>? OnHandlerException { get; set; }

    /// <summary>
    /// Registers a middleware interceptor that is invoked for every published message before it reaches subscribers.
    /// Should be called before the first <see cref="Publish{T}"/> call; late additions log a warning but are still applied.
    /// </summary>
    /// <param name="middleware">The middleware to add to the pipeline.</param>
    public void Use(IMessageMiddleware middleware)
    {
        _threadGuard.Assert();
        if (_hasPublished)
        {
            CoreLog.Warn("Middleware added after the first Publish() call. This may cause inconsistent message handling.");
        }
        _middlewares.Add(middleware);
        _runnerCache.Clear();
    }

    /// <summary>
    /// Subscribes a handler to messages of type <typeparamref name="T"/> and returns a subscription ID.
    /// Use with <see cref="Unsubscribe{T}"/> to remove. Zero-allocation.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The callback invoked when a message of type <typeparamref name="T"/> is published.</param>
    /// <param name="priority">Subscription priority; lower values execute first. Defaults to 0.</param>
    /// <returns>A long subscription ID.</returns>
    public long Subscribe<T>(Action<T> handler, int priority = 0) where T : IMessage
    {
        _threadGuard.Assert();
        var list = GetOrCreateList<T>();
        return list.AddWithId(handler, priority);
    }

    /// <summary>
    /// Removes a subscription by ID.
    /// </summary>
    /// <typeparam name="T">The message type the subscription was registered for.</typeparam>
    /// <param name="subscriptionId">The ID returned by <see cref="Subscribe{T}"/>.</param>
    public void Unsubscribe<T>(long subscriptionId) where T : IMessage
    {
        _threadGuard.Assert();
        var list = GetListOrNull<T>();
        list?.Remove(subscriptionId);
    }

    /// <summary>
    /// Subscribes a handler and returns an <see cref="IDisposable"/> token that removes the subscription when disposed.
    /// Convenience wrapper over <see cref="Subscribe{T}"/>; allocates a small token object.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The callback invoked when a message of type <typeparamref name="T"/> is published.</param>
    /// <param name="priority">Subscription priority; lower values execute first. Defaults to 0.</param>
    /// <returns>An <see cref="IDisposable"/> token that removes the subscription when disposed.</returns>
    public IDisposable Listen<T>(Action<T> handler, int priority = 0) where T : IMessage
    {
        _threadGuard.Assert();
        var list = GetOrCreateList<T>();
        return list.Add(handler, priority);
    }

    /// <summary>
    /// Publishes a message to all subscribers of <typeparamref name="T"/>, passing through any registered middleware.
    /// </summary>
    /// <typeparam name="T">The type of message to publish.</typeparam>
    /// <param name="message">The message instance to publish.</param>
    public void Publish<T>(T message) where T : IMessage
    {
        _threadGuard.Assert();
        _hasPublished = true;

        if (_middlewares.Count == 0)
        {
            DispatchDirect(message);
        }
        else
        {
            var runner = GetOrBuildRunner<T>();
            runner.Start(message);
        }
    }

    private MiddlewareRunner<T> GetOrBuildRunner<T>() where T : IMessage
    {
        var key = typeof(T);
        if (_runnerCache.TryGetValue(key, out var cached))
        {
            return Unsafe.As<MiddlewareRunner<T>>(cached);
        }

        var runner = new MiddlewareRunner<T>(this);
        _runnerCache[key] = runner;
        return runner;
    }

    [HotPath]
    private void DispatchDirect<T>(T message) where T : IMessage
    {
        var list = GetListOrNull<T>();
        list?.Dispatch(message, OnHandlerException);
    }

    private SubscriptionList<T> GetOrCreateList<T>() where T : IMessage
    {
        var key = typeof(T);
        if (_subscriptionLists.TryGetValue(key, out var obj))
        {
            return Unsafe.As<SubscriptionList<T>>(obj);
        }

        var list = new SubscriptionList<T>();
        _subscriptionLists[key] = list;
        return list;
    }

    private SubscriptionList<T>? GetListOrNull<T>() where T : IMessage
    {
        if (_subscriptionLists.TryGetValue(typeof(T), out var obj))
        {
            return Unsafe.As<SubscriptionList<T>>(obj);
        }
        return null;
    }

    /// <summary>
    /// Clears all subscriptions, middleware, and cached chains.
    /// Disposes any middleware that implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            if (_middlewares[i] is IDisposable disposable)
            {
                try { disposable.Dispose(); }
                catch (Exception ex) { CoreLog.Warn($"Middleware disposal failed: {ex.Message}"); }
            }
        }
        _subscriptionLists.Clear();
        _middlewares.Clear();
        _runnerCache.Clear();
        OnHandlerException = null;
    }

    /// <summary>
    /// Per message type subscription list with zero-allocation dispatch.
    /// Flat array, sorted by priority, version-counter for re-entrant safety.
    /// </summary>
    private sealed class SubscriptionList<T> where T : IMessage
    {
        private Entry[] _entries = new Entry[4];
        private int _count;
        private long _nextId;
        private int _dispatchDepth;

        private PendingChange[] _pending = new PendingChange[4];
        private int _pendingCount;

        public IDisposable Add(Action<T> handler, int priority)
        {
            long id = _nextId++;

            if (_dispatchDepth > 0)
            {
                AddPending(new PendingChange(PendingKind.Add, id, handler, priority));
                return new Token(this, id);
            }

            InsertSorted(handler, priority, id);
            return new Token(this, id);
        }

        public long AddWithId(Action<T> handler, int priority)
        {
            long id = _nextId++;

            if (_dispatchDepth > 0)
            {
                AddPending(new PendingChange(PendingKind.Add, id, handler, priority));
                return id;
            }

            InsertSorted(handler, priority, id);
            return id;
        }

        public void Remove(long id)
        {
            if (_dispatchDepth > 0)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (_entries[i].Id == id)
                    {
                        _entries[i].Tombstone = true;
                        return;
                    }
                }
                AddPending(new PendingChange(PendingKind.Remove, id, null!, 0));
                return;
            }

            RemoveDirect(id);
        }

        [HotPath]
        public void Dispatch(T message, Action<Exception>? exceptionHandler)
        {
            _dispatchDepth++;
            List<Exception>? exceptions = null;
            try
            {
                int count = _count;
                for (int i = 0; i < count; i++)
                {
                    if (!_entries[i].Tombstone)
                    {
                        try
                        {
                            _entries[i].Handler(message);
                        }
                        catch (Exception ex)
                        {
                            if (exceptionHandler is not null)
                            {
                                exceptionHandler(ex);
                            }
                            else
                            {
                                CoreLog.Error($"Handler threw during Publish<{typeof(T).Name}>: {ex.Message}");
                                exceptions ??= new List<Exception>();
                                exceptions.Add(ex);
                            }
                        }
                    }
                }
            }
            finally
            {
                _dispatchDepth--;
                if (_dispatchDepth == 0 && _pendingCount > 0)
                {
                    ApplyPendingChanges();
                }
            }

            if (exceptions is not null)
            {
                var inner = exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions);
                throw new FlosException(CoreErrors.HandlerException,
                    $"{exceptions.Count} handler(s) threw during Publish<{typeof(T).Name}>: {exceptions[0].Message}",
                    inner);
            }
        }

        private void InsertSorted(Action<T> handler, int priority, long id)
        {
            if (_count == _entries.Length)
            {
                Array.Resize(ref _entries, _entries.Length * 2);
            }

            int insertIdx = _count;
            for (int i = 0; i < _count; i++)
            {
                if (_entries[i].Priority > priority)
                {
                    insertIdx = i;
                    break;
                }
            }

            Array.Copy(_entries, insertIdx, _entries, insertIdx + 1, _count - insertIdx);
            _entries[insertIdx] = new Entry(handler, priority, id);
            _count++;
        }

        private void RemoveDirect(long id)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_entries[i].Id == id)
                {
                    Array.Copy(_entries, i + 1, _entries, i, _count - i - 1);
                    _count--;
                    _entries[_count] = default;
                    return;
                }
            }
        }

        private void AddPending(PendingChange change)
        {
            if (_pendingCount == _pending.Length)
            {
                Array.Resize(ref _pending, _pending.Length * 2);
            }
            _pending[_pendingCount++] = change;
        }

        private void ApplyPendingChanges()
        {
            int write = 0;
            for (int read = 0; read < _count; read++)
            {
                if (!_entries[read].Tombstone)
                {
                    if (write != read)
                        _entries[write] = _entries[read];
                    write++;
                }
            }
            for (int i = write; i < _count; i++)
                _entries[i] = default;
            _count = write;

            int pendingCount = _pendingCount;
            for (int i = 0; i < pendingCount; i++)
            {
                var p = _pending[i];
                if (p.Kind == PendingKind.Add)
                {
                    InsertSorted(p.Handler, p.Priority, p.Id);
                }
                else
                {
                    RemoveDirect(p.Id);
                }
                _pending[i] = default;
            }
            _pendingCount = 0;
        }

        private struct Entry(Action<T> handler, int priority, long id)
        {
            public readonly Action<T> Handler = handler;
            public readonly int Priority = priority;
            public readonly long Id = id;
            public bool Tombstone = false;
        }

        private enum PendingKind : byte { Add, Remove }

        private readonly struct PendingChange(PendingKind kind, long id, Action<T> handler, int priority)
        {
            public readonly PendingKind Kind = kind;
            public readonly long Id = id;
            public readonly Action<T> Handler = handler;
            public readonly int Priority = priority;
        }

        private sealed class Token(SubscriptionList<T> list, long id) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                list.Remove(id);
            }
        }
    }

    /// <summary>
    /// Stateful middleware dispatcher. Caches a single <see cref="Action{T}"/> delegate
    /// and walks the middleware list via a mutable index. Save/restore on <see cref="Start"/>
    /// guarantees correctness under reentrant same-type Publish.
    /// </summary>
    private sealed class MiddlewareRunner<T> where T : IMessage
    {
        private readonly MessageBus _bus;
        private readonly Action<T> _stepDelegate;
        private int _currentIndex;

        public MiddlewareRunner(MessageBus bus)
        {
            _bus = bus;
            _stepDelegate = Step;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start(T message)
        {
            int saved = _currentIndex;
            _currentIndex = 0;
            try
            {
                Step(message);
            }
            finally
            {
                _currentIndex = saved;
            }
        }

        private void Step(T message)
        {
            if (_currentIndex < _bus._middlewares.Count)
            {
                var mw = _bus._middlewares[_currentIndex++];
                mw.Handle(message, _stepDelegate);
            }
            else
            {
                _bus.DispatchDirect(message);
            }
        }
    }
}