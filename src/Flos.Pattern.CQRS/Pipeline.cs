using Flos.Core.Annotations;
using Flos.Core.Errors;
using Flos.Core.Logging;
using Flos.Core.Messaging;
using Flos.Core.Scheduling;
using Flos.Core.State;
using Flos.Snapshot;

namespace Flos.Pattern.CQRS;

internal sealed class Pipeline : IPipeline, IHandlerRegistry
{
    private readonly IMessageBus _bus;
    private readonly IWorld _world;
    private readonly ISnapshotManager _snapshotManager;
    private readonly IEventJournal _journal;
    private readonly IScheduler _scheduler;
    private readonly CQRSConfig _config;

    private readonly Dictionary<Type, Func<ICommand, IStateView, Result<IReadOnlyList<IEvent>>>> _handlers = [];
    private readonly Dictionary<Type, List<Action<IEvent, IWorld>>> _appliers = [];
    private readonly Dictionary<Type, Action<IMessageBus, IEvent>> _publishers = [];

    /// <summary>
    /// Number of applier faults absorbed in <see cref="ApplierFaultMode.Tolerant"/> mode.
    /// Always 0 in Strict mode (which throws immediately).
    /// </summary>
    internal int TolerantFaultCount { get; private set; }

    internal Pipeline(
        IMessageBus bus,
        IWorld world,
        ISnapshotManager snapshotManager,
        IEventJournal journal,
        IScheduler scheduler,
        CQRSConfig config)
    {
        _bus = bus;
        _world = world;
        _snapshotManager = snapshotManager;
        _journal = journal;
        _scheduler = scheduler;
        _config = config;
    }

    public void Register<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand
    {
        var key = typeof(TCommand);
        if (_handlers.ContainsKey(key))
        {
            CoreLog.Warn($"Handler for command '{key.Name}' is being overwritten.");
        }
        _handlers[key] = (command, stateView) => handler.Handle((TCommand)command, stateView);
    }

    public void Register<TEvent, TState>(IEventApplier<TEvent, TState> applier)
        where TEvent : IEvent
        where TState : class, IStateSlice
    {
        var eventType = typeof(TEvent);
        if (!_appliers.TryGetValue(eventType, out var list))
        {
            list = [];
            _appliers[eventType] = list;
        }

        list.Add((evt, world) => applier.Apply(world.Get<TState>(), (TEvent)evt));

        _publishers.TryAdd(eventType, static (bus, evt) => bus.Publish((TEvent)evt));
    }

    [HotPath]
    public Result<IReadOnlyList<IEvent>> Send(ICommand command)
    {
        var commandType = command.GetType();

        if (!_handlers.TryGetValue(commandType, out var handler))
        {
            return Result<IReadOnlyList<IEvent>>.Fail(CQRSErrors.UnknownCommand);
        }

        var snapshot = _snapshotManager.Capture(_world);

        var result = handler(command, snapshot);

        if (!result.IsSuccess)
        {
            _bus.Publish(new CommandRejectedMessage(command, result.Error));
            return Result<IReadOnlyList<IEvent>>.Fail(result.Error);
        }

        var events = result.Value;

        try
        {
            ApplyEvents(events);
        }
        catch (Exception ex)
        {
            return HandleApplierFault(snapshot, command, ex);
        }

        if (_config.EnableJournal)
        {
            var tick = _scheduler.CurrentTick;
            for (int i = 0; i < events.Count; i++)
            {
                _journal.Append(tick, events[i]);
            }
        }

        for (int i = 0; i < events.Count; i++)
        {
            PublishEvent(events[i]);
        }

        return Result<IReadOnlyList<IEvent>>.Ok(events);
    }

    [HotPath]
    private void ApplyEvents(IReadOnlyList<IEvent> events)
    {
        for (int i = 0; i < events.Count; i++)
        {
            var evt = events[i];
            var eventType = evt.GetType();

            if (!_appliers.TryGetValue(eventType, out var applierList))
                continue;

            for (int j = 0; j < applierList.Count; j++)
            {
                applierList[j](evt, _world);
            }
        }
    }

    private Result<IReadOnlyList<IEvent>> HandleApplierFault(
        IStateView preApplySnapshot, ICommand command, Exception ex)
    {
        switch (_config.FaultMode)
        {
            case ApplierFaultMode.Strict:
                _snapshotManager.RestoreTo(_world, preApplySnapshot);
                _bus.Publish(new CommandRejectedMessage(command, CQRSErrors.ApplierFailed));
                return Result<IReadOnlyList<IEvent>>.Fail(CQRSErrors.ApplierFailed);

            case ApplierFaultMode.Tolerant:
                TolerantFaultCount++;
                _snapshotManager.RestoreTo(_world, preApplySnapshot);
                _bus.Publish(new CommandRejectedMessage(command, CQRSErrors.ApplierFailed));
                return Result<IReadOnlyList<IEvent>>.Fail(CQRSErrors.ApplierFailed);

            case ApplierFaultMode.Fatal:
            default:
                throw new FlosException(CQRSErrors.ApplierFailed,
                    $"Applier threw in Fatal mode: {ex.Message}");
        }
    }

    [HotPath]
    private void PublishEvent(IEvent evt)
    {
        var eventType = evt.GetType();
        if (_publishers.TryGetValue(eventType, out var publisher))
        {
            publisher(_bus, evt);
        }
        else
        {
            _bus.Publish(evt);
        }
    }
}
