using Flos.Core.Messaging;
using Flos.Core.Scheduling;
using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Built-in <see cref="ICQRSAdapter"/> that creates the default <see cref="Pipeline"/>.
/// Registered by <see cref="CQRSPatternModule"/> when no other adapter is present.
/// </summary>
internal sealed class BuiltInCQRSAdapter : ICQRSAdapter
{
    private readonly IRollbackProvider? _rollbackProvider;
    private readonly IEventJournal _journal;
    private readonly IScheduler _scheduler;
    private readonly CQRSConfig _config;

    internal BuiltInCQRSAdapter(
        IRollbackProvider? rollbackProvider,
        IEventJournal journal,
        IScheduler scheduler,
        CQRSConfig config)
    {
        _rollbackProvider = rollbackProvider;
        _journal = journal;
        _scheduler = scheduler;
        _config = config;
    }

    public (IPipeline Pipeline, IHandlerRegistry Registry) CreatePipeline(IMessageBus bus, IWorld world)
    {
        var pipeline = new Pipeline(bus, world, _rollbackProvider, _journal, _scheduler, _config);
        return (pipeline, pipeline);
    }
}
