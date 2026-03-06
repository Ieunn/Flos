using System.Runtime.CompilerServices;
using Flos.Core.Annotations;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Reusable, array-backed event buffer with zero-boxing generic Add.
/// Handlers append events via <see cref="Add{T}"/> instead of allocating a new list per command.
/// The pipeline owns and resets the buffer between commands. Single-threaded use only.
/// </summary>
public sealed class EventBuffer
{
    private EventSlot[] _slots;
    private int _count;
#if DEBUG
    private int _version;
#endif

    internal EventBuffer(int initialCapacity = 4)
    {
        _slots = new EventSlot[initialCapacity];
    }

    /// <summary>Number of events in the buffer.</summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

#if DEBUG
    /// <summary>Version counter incremented on each Reset. Used for use-after-reset detection in DEBUG.</summary>
    internal int Version => _version;
#endif

    /// <summary>Appends a typed event to the buffer. Zero-boxing for struct events.</summary>
    [HotPath]
    public void Add<T>(T evt) where T : IEvent
    {
        if (_count == _slots.Length)
            Grow();

        _slots[_count++] = EventSlot.Create(evt);
    }

    /// <summary>Resets the buffer for reuse. Returns pooled holders. Does not shrink the backing array.</summary>
    [HotPath]
    internal void Reset()
    {
        for (int i = 0; i < _count; i++)
            _slots[i].Return();
        Array.Clear(_slots, 0, _count);
        _count = 0;
#if DEBUG
        _version++;
#endif
    }

    /// <summary>Gets the event slot at the specified index (for pipeline internal use).</summary>
    internal ref readonly EventSlot GetSlot(int index) => ref _slots[index];

    private void Grow()
    {
        Array.Resize(ref _slots, _slots.Length * 2);
    }

    /// <summary>
    /// A type-erased event slot that holds a typed event without boxing.
    /// Uses a pooled holder object to avoid per-Add allocations in steady state.
    /// Provides typed Publish and Apply callbacks that avoid interface dispatch.
    /// </summary>
    public readonly struct EventSlot
    {
        /// <summary>The runtime type of the event.</summary>
        internal readonly Type EventType;

        /// <summary>Holder object that contains the typed event and its operations.</summary>
        private readonly IEventHolder _holder;

        private EventSlot(Type eventType, IEventHolder holder)
        {
            EventType = eventType;
            _holder = holder;
        }

        internal static EventSlot Create<T>(T evt) where T : IEvent
        {
            return new EventSlot(typeof(T), TypedHolder<T>.Rent(evt));
        }

        /// <summary>Publishes the event on the bus using the typed Publish&lt;T&gt; path (no boxing).</summary>
        internal void Publish(Core.Messaging.IMessageBus bus) => _holder.Publish(bus);

        /// <summary>Applies the event through a typed dispatch interface (no boxing).</summary>
        internal void Apply(IApplierDispatch dispatch, Core.State.IWorld world)
            => _holder.Apply(dispatch, world);

        /// <summary>Creates a journal entry holding the event without boxing.</summary>
        internal JournalEntry ToJournalEntry(long tick)
            => new JournalEntry(tick, EventType, _holder.CreateJournalHolder());

        /// <summary>Returns the holder to its type-specific pool.</summary>
        internal void Return() => _holder.Return();
    }

    private interface IEventHolder
    {
        void Publish(Core.Messaging.IMessageBus bus);
        void Apply(IApplierDispatch dispatch, Core.State.IWorld world);
        IJournalEventHolder CreateJournalHolder();
        void Return();
    }

    private sealed class TypedHolder<T> : IEventHolder where T : IEvent
    {
        private const int MaxPoolSize = 64;

        [ThreadStatic]
        private static Stack<TypedHolder<T>>? _pool;

        private static Stack<TypedHolder<T>> Pool => _pool ??= new Stack<TypedHolder<T>>(8);

        private T _event;

        private TypedHolder(T evt) => _event = evt;

        internal static TypedHolder<T> Rent(T evt)
        {
            if (Pool.TryPop(out var holder))
            {
                holder._event = evt;
                return holder;
            }
            return new TypedHolder<T>(evt);
        }

        public void Publish(Core.Messaging.IMessageBus bus) => bus.Publish(_event);

        public void Apply(IApplierDispatch dispatch, Core.State.IWorld world)
            => dispatch.Apply(_event, world);

        public IJournalEventHolder CreateJournalHolder()
            => new JournalEventHolder<T>(_event);

        public void Return()
        {
            _event = default!;
            if (Pool.Count < MaxPoolSize)
                Pool.Push(this);
        }
    }
}

/// <summary>
/// Internal dispatch interface for zero-boxing event applier dispatch.
/// The pipeline implements this to route typed events to typed applier lists.
/// </summary>
internal interface IApplierDispatch
{
    void Apply<T>(T evt, Core.State.IWorld world) where T : IEvent;
}
