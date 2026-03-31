using System.Runtime.CompilerServices;
using Flos.Core.Annotations;
using Flos.Core.Errors;
using Flos.Core.Logging;
using Flos.Core.Messaging;
using Flos.Core.Scheduling;
using Flos.Core.State;

namespace Flos.Pattern.CQRS;

internal sealed class Pipeline : IPipeline, IHandlerRegistry, IApplierDispatch
{
    private readonly IMessageBus _bus;
    private readonly IWorld _world;
    private readonly IRollbackProvider? _rollbackProvider;
    private readonly IEventJournal _journal;
    private readonly IScheduler _scheduler;
    private readonly CQRSConfig _config;
    private readonly EventBuffer _eventBuffer = new();
    private readonly ReadOnlyWorldView _handlerView = new();
    private bool _isSending;
    private readonly Queue<ICommand> _deferredCommands = new();
    private int _deferralDepth;

    private readonly Dictionary<Type, ICommandDispatcher> _dispatchers = new();
    private readonly Dictionary<Type, object> _appliers = new();

    /// <summary>
    /// Number of applier faults absorbed in <see cref="ApplierFaultMode.Tolerant"/> mode.
    /// Always 0 in Strict mode (which throws immediately).
    /// </summary>
    internal int TolerantFaultCount { get; private set; }

    /// <summary>
    /// Number of subscriber exceptions swallowed during the last <see cref="Send"/> call's
    /// event publication phase. Reset to 0 at the start of each Send.
    /// Non-zero indicates that some subscribers did not receive events despite successful state mutation.
    /// </summary>
    public int PublishFaultCount { get; private set; }

    internal Pipeline(
        IMessageBus bus,
        IWorld world,
        IRollbackProvider? rollbackProvider,
        IEventJournal journal,
        IScheduler scheduler,
        CQRSConfig config)
    {
        if (config.EnableRollback && rollbackProvider is null)
        {
            throw new FlosException(CQRSErrors.InvalidConfiguration,
                "EnableRollback is true but no IRollbackProvider was registered.");
        }

        _bus = bus;
        _world = world;
        _rollbackProvider = rollbackProvider;
        _journal = journal;
        _scheduler = scheduler;
        _config = config;
    }

    public void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand
    {
        var key = typeof(TCommand);

        if (_dispatchers.TryGetValue(key, out var existing))
        {
            CoreLog.Warn($"Handler for command '{key.Name}' is being overwritten.");
            ((CommandDispatcher<TCommand>)existing).Handler = handler;
            return;
        }

        _dispatchers[key] = new CommandDispatcher<TCommand> { Handler = handler };
    }

    public void Register<TEvent, TState>(IEventApplier<TEvent, TState> applier)
        where TEvent : IEvent
        where TState : class, IStateSlice
    {
        var eventType = typeof(TEvent);
        if (!_appliers.TryGetValue(eventType, out var listObj))
        {
            var list = new ApplierList<TEvent>();
            _appliers[eventType] = list;
            listObj = list;
        }

        ((ApplierList<TEvent>)listObj).Add(applier);
    }

    /// <inheritdoc />
    [HotPath]
    public Result<EventBuffer> Send(ICommand command)
    {
        if (TryEnqueueIfReentrant(command))
            return Result<EventBuffer>.Ok(_eventBuffer);

        if (!_dispatchers.TryGetValue(command.GetType(), out var dispatcher))
            return Result<EventBuffer>.Fail(CQRSErrors.UnknownCommand);

        _isSending = true;
        PublishFaultCount = 0;
        IStateReader? rollbackSnapshot = null;
        Result<EventBuffer> result;
        try
        {
            rollbackSnapshot = CaptureRollbackIfEnabled();
            _handlerView.Bind(_world, _config.FaultMode);
            _eventBuffer.Reset();
            var error = dispatcher.Execute(command, _handlerView, _eventBuffer);

            result = FinishSend(command, ref rollbackSnapshot, error);
        }
        finally
        {
            EndSend(rollbackSnapshot);
        }

        DrainDeferredCommands();

        return result;
    }

    /// <inheritdoc />
    [HotPath]
    public Result<EventBuffer> Send<TCommand>(TCommand command) where TCommand : ICommand
    {
        if (TryEnqueueIfReentrant(command))
            return Result<EventBuffer>.Ok(_eventBuffer);

        if (!_dispatchers.TryGetValue(typeof(TCommand), out var dispatcherObj))
            return Result<EventBuffer>.Fail(CQRSErrors.UnknownCommand);

        _isSending = true;
        PublishFaultCount = 0;
        IStateReader? rollbackSnapshot = null;
        Result<EventBuffer> result;
        try
        {
            var dispatcher = (CommandDispatcher<TCommand>)dispatcherObj;

            rollbackSnapshot = CaptureRollbackIfEnabled();
            _handlerView.Bind(_world, _config.FaultMode);
            _eventBuffer.Reset();
            var error = dispatcher.ExecuteTyped(command, _handlerView, _eventBuffer);

            result = FinishSend(command, ref rollbackSnapshot, error);
        }
        finally
        {
            EndSend(rollbackSnapshot);
        }

        DrainDeferredCommands();

        return result;
    }

    /// <summary>
    /// If currently inside a Send, enqueue the command for deferred execution after the
    /// outermost Send completes. Returns true if deferred (caller should return early).
    /// </summary>
    private bool TryEnqueueIfReentrant(ICommand command)
    {
        if (!_isSending)
            return false;

        if (_config.MaxDeferralDepth == 0)
        {
            throw new FlosException(CQRSErrors.ReentrantSend,
                "Reentrant Pipeline.Send detected. Deferred send is disabled (MaxDeferralDepth=0).");
        }

        if (_deferredCommands.Count >= _config.MaxDeferralDepth)
        {
            throw new FlosException(CQRSErrors.DeferralDepthExceeded,
                $"Deferred command queue exceeded MaxDeferralDepth ({_config.MaxDeferralDepth}). "
                + "Possible infinite command loop.");
        }

        _deferredCommands.Enqueue(command);
        return true;
    }

    /// <summary>
    /// Processes all deferred commands in FIFO order after the outermost Send completes.
    /// Each deferred command may itself queue further commands; those are drained in the same loop
    /// (breadth-first execution order).
    /// </summary>
    private void DrainDeferredCommands()
    {
        while (_deferredCommands.TryDequeue(out var deferred))
        {
            _deferralDepth++;
            try
            {
                if (!_dispatchers.TryGetValue(deferred.GetType(), out var dispatcher))
                {
                    CoreLog.Warn($"Deferred command '{deferred.GetType().Name}' has no handler. Skipping.");
                    continue;
                }

                _isSending = true;
                PublishFaultCount = 0;
                IStateReader? rollbackSnapshot = null;
                try
                {
                    rollbackSnapshot = CaptureRollbackIfEnabled();
                    _handlerView.Bind(_world, _config.FaultMode);
                    _eventBuffer.Reset();
                    var error = dispatcher.Execute(deferred, _handlerView, _eventBuffer);

                    var result = FinishSend(deferred, ref rollbackSnapshot, error);
                    if (!result.IsSuccess)
                    {
                        CoreLog.Warn($"Deferred command '{deferred.GetType().Name}' failed: {result.Error}");
                    }
                }
                finally
                {
                    EndSend(rollbackSnapshot);
                }
            }
            finally
            {
                _deferralDepth--;
            }
        }
    }

    private IStateReader? CaptureRollbackIfEnabled()
        => _config.EnableRollback ? _rollbackProvider?.Capture(_world) : null;

    [HotPath]
    private Result<EventBuffer> FinishSend(ICommand command, ref IStateReader? rollbackSnapshot, ErrorCode handleError)
    {
        if (handleError != default)
        {
            _bus.Publish(new CommandRejectedMessage(command, handleError));
            return Result<EventBuffer>.Fail(handleError);
        }
        return CompleteCommand(_eventBuffer, ref rollbackSnapshot, command);
    }

    private void EndSend(IStateReader? rollbackSnapshot)
    {
        _handlerView.Reset();
        ReleaseSnapshot(rollbackSnapshot);
        _isSending = false;
    }

    [HotPath]
    private Result<EventBuffer> CompleteCommand(
        EventBuffer events, ref IStateReader? rollbackSnapshot, ICommand command)
    {
        try
        {
            ApplyEvents(events);
        }
        catch (Exception ex)
        {
            return HandleApplierFault(ref rollbackSnapshot, command, ex);
        }

        var tick = _scheduler.CurrentTick;
        for (int i = 0; i < events.Count; i++)
        {
            try
            {
                events.GetSlot(i).Publish(_bus);
            }
            catch (Exception ex)
            {
                PublishFaultCount++;
                CoreLog.Error(
                    $"Subscriber threw during event '{events.GetSlot(i).EventType.Name}': "
                    + $"{ex.Message}. State was mutated but this subscriber did not process the event.");
            }
        }

        if (_config.EnableJournal)
        {
            for (int i = 0; i < events.Count; i++)
            {
                _journal.Append(tick, events.GetSlot(i));
            }
        }

        return Result<EventBuffer>.Ok(events);
    }

    [HotPath]
    private void ApplyEvents(EventBuffer events)
    {
        for (int i = 0; i < events.Count; i++)
        {
            ref readonly var slot = ref events.GetSlot(i);
            if (_config.FaultMode == ApplierFaultMode.Tolerant)
            {
                try
                {
                    slot.Apply(this, _world);
                }
                catch (Exception ex)
                {
                    CoreLog.Error($"Applier threw for event '{slot.EventType.Name}': {ex.Message}. Skipping (Tolerant mode).");
                    TolerantFaultCount++;
                }
            }
            else
            {
                slot.Apply(this, _world);
            }
        }
    }

    [HotPath]
    void IApplierDispatch.Apply<T>(T evt, IWorld world)
    {
        if (!_appliers.TryGetValue(typeof(T), out var listObj))
            return;

        ((ApplierList<T>)listObj).ApplyAll(evt, world);
    }

    private Result<EventBuffer> HandleApplierFault(
        ref IStateReader? preApplySnapshot, ICommand command, Exception ex)
    {
        CoreLog.Error($"Applier threw for command '{command.GetType().Name}': {ex}");

        switch (_config.FaultMode)
        {
            case ApplierFaultMode.Strict:
                RestoreAndRelease(ref preApplySnapshot);
                _bus.Publish(new CommandRejectedMessage(command, CQRSErrors.ApplierFailed));
                return Result<EventBuffer>.Fail(CQRSErrors.ApplierFailed);

            case ApplierFaultMode.Fatal:
            default:
                RestoreAndRelease(ref preApplySnapshot);
                throw new FlosException(CQRSErrors.ApplierFailed,
                    $"Applier threw in Fatal mode: {ex.Message}", ex);
        }
    }

    private void RestoreAndRelease(ref IStateReader? snapshot)
    {
        if (snapshot is null) return;
        _rollbackProvider!.RestoreTo(_world, snapshot);
        _rollbackProvider.Release(snapshot);
        snapshot = null;
    }

    private void ReleaseSnapshot(IStateReader? snapshot)
    {
        if (snapshot is not null)
            _rollbackProvider?.Release(snapshot);
    }

    private interface IApplierEntry<TEvent> where TEvent : IEvent
    {
        void Apply(TEvent evt, IWorld world);
    }

    private sealed class ApplierEntry<TEvent, TState> : IApplierEntry<TEvent>
        where TEvent : IEvent
        where TState : class, IStateSlice
    {
        private readonly IEventApplier<TEvent, TState> _applier;

        public ApplierEntry(IEventApplier<TEvent, TState> applier)
            => _applier = applier;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Apply(TEvent evt, IWorld world)
            => _applier.Apply(world.Get<TState>(), evt);
    }

    /// <summary>
    /// Typed applier list that dispatches events to appliers without boxing.
    /// One per event type. Each entry captures TState at registration time.
    /// </summary>
    private sealed class ApplierList<TEvent> where TEvent : IEvent
    {
        private IApplierEntry<TEvent>[] _entries = new IApplierEntry<TEvent>[2];
        private int _count;

        internal void Add<TState>(IEventApplier<TEvent, TState> applier)
            where TState : class, IStateSlice
        {
            if (_count == _entries.Length)
                Array.Resize(ref _entries, _entries.Length * 2);
            _entries[_count++] = new ApplierEntry<TEvent, TState>(applier);
        }

        [HotPath]
        internal void ApplyAll(TEvent evt, IWorld world)
        {
            for (int i = 0; i < _count; i++)
            {
                _entries[i].Apply(evt, world);
            }
        }
    }
}