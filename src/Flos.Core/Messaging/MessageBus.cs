using System.Diagnostics;
using System.Runtime.CompilerServices;
using Flos.Core.Annotations;
using Flos.Core.Errors;
using Flos.Core.Logging;

namespace Flos.Core.Messaging;

/// <summary>
/// Default implementation of <see cref="IMessageBus"/> with priority-ordered subscriptions and middleware pipeline.
/// </summary>
public sealed class MessageBus : IMessageBus
{
    private readonly Dictionary<Type, object> _subscriptionLists = [];
    private readonly List<IMessageMiddleware> _middlewares = [];
    private readonly Dictionary<Type, Delegate> _chainCache = [];
    private bool _hasPublished;
    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;

    [Conditional("DEBUG")]
    private void AssertMainThread([CallerMemberName] string? caller = null)
    {
        Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId,
            $"MessageBus.{caller}() called from thread {Environment.CurrentManagedThreadId}, " +
            $"expected main thread {_ownerThreadId}. Use IDispatcher.Enqueue() for cross-thread access.");
    }

    /// <summary>
    /// Registers a middleware interceptor that is invoked for every published message before it reaches subscribers.
    /// Should be called before the first <see cref="Publish{T}"/> call; late additions log a warning but are still applied.
    /// </summary>
    /// <param name="middleware">The middleware to add to the pipeline.</param>
    public void Use(IMessageMiddleware middleware)
    {
        if (_hasPublished)
        {
            Logging.CoreLog.Warn("Middleware added after the first Publish() call. This may cause inconsistent message handling.");
        }
        _middlewares.Add(middleware);
        _chainCache.Clear();
    }

    /// <summary>
    /// Subscribes a handler to messages of type <typeparamref name="T"/> and returns a subscription ID.
    /// Use with <see cref="Unsubscribe{T}"/> to remove. Zero-allocation.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The callback invoked when a message of type <typeparamref name="T"/> is published.</param>
    /// <param name="priority">Subscription priority; lower values execute first. Defaults to 0.</param>
    /// <returns>An integer subscription ID.</returns>
    public int Subscribe<T>(Action<T> handler, int priority = 0) where T : IMessage
    {
        AssertMainThread();
        var list = GetOrCreateList<T>();
        return list.AddWithId(handler, priority);
    }

    /// <summary>
    /// Removes a subscription by ID.
    /// </summary>
    /// <typeparam name="T">The message type the subscription was registered for.</typeparam>
    /// <param name="subscriptionId">The ID returned by <see cref="Subscribe{T}"/>.</param>
    public void Unsubscribe<T>(int subscriptionId) where T : IMessage
    {
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
        AssertMainThread();
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
        AssertMainThread();
        _hasPublished = true;

        if (_middlewares.Count == 0)
        {
            DispatchDirect(message);
        }
        else
        {
            var chain = GetOrBuildChain<T>();
            chain(message);
        }
    }

    private Action<T> GetOrBuildChain<T>() where T : IMessage
    {
        var key = typeof(T);
        if (_chainCache.TryGetValue(key, out var cached))
        {
            return Unsafe.As<Action<T>>(cached);
        }

        Action<T> chain = DispatchDirect;
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var prev = chain;
            chain = msg => mw.Handle(msg, prev);
        }

        _chainCache[key] = chain;
        return chain;
    }

    [HotPath]
    private void DispatchDirect<T>(T message) where T : IMessage
    {
        var list = GetListOrNull<T>();
        list?.Dispatch(message);
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
    /// Per message type subscription list with zero-allocation dispatch.
    /// Flat array, sorted by priority, version-counter for re-entrant safety.
    /// </summary>
    private sealed class SubscriptionList<T> where T : IMessage
    {
        private Entry[] _entries = new Entry[4];
        private int _count;
        private int _nextId;
        private int _dispatchDepth;

        private PendingChange[] _pending = new PendingChange[4];
        private int _pendingCount;

        public IDisposable Add(Action<T> handler, int priority)
        {
            int id = _nextId++;

            if (_dispatchDepth > 0)
            {
                AddPending(new PendingChange(PendingKind.Add, id, handler, priority));
                return new Token(this, id);
            }

            InsertSorted(handler, priority, id);
            return new Token(this, id);
        }

        public int AddWithId(Action<T> handler, int priority)
        {
            int id = _nextId++;

            if (_dispatchDepth > 0)
            {
                AddPending(new PendingChange(PendingKind.Add, id, handler, priority));
                return id;
            }

            InsertSorted(handler, priority, id);
            return id;
        }

        public void Remove(int id)
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
        public void Dispatch(T message)
        {
            _dispatchDepth++;
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
                            CoreLog.Error($"Handler threw during Publish<{typeof(T).Name}>: {ex.Message}");
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
        }

        private void InsertSorted(Action<T> handler, int priority, int id)
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

        private void RemoveDirect(int id)
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

        private struct Entry(Action<T> handler, int priority, int id)
        {
            public readonly Action<T> Handler = handler;
            public readonly int Priority = priority;
            public readonly int Id = id;
            public bool Tombstone = false;
        }

        private enum PendingKind : byte { Add, Remove }

        private readonly struct PendingChange(PendingKind kind, int id, Action<T> handler, int priority)
        {
            public readonly PendingKind Kind = kind;
            public readonly int Id = id;
            public readonly Action<T> Handler = handler;
            public readonly int Priority = priority;
        }

        private sealed class Token(SubscriptionList<T> list, int id) : IDisposable
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
}
