using Flos.Core.Messaging;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.State;
using Flos.Snapshot;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Built-in <see cref="ICQRSAdapter"/> that creates the default <see cref="Pipeline"/>.
/// Registered by <see cref="CQRSPatternModule"/> when no other adapter is present.
/// </summary>
internal sealed class BuiltInCQRSAdapter : ICQRSAdapter
{
    private readonly ISnapshotManager _snapshotManager;
    private readonly IEventJournal _journal;
    private readonly IScheduler _scheduler;
    private readonly CQRSConfig _config;

    internal BuiltInCQRSAdapter(
        ISnapshotManager snapshotManager,
        IEventJournal journal,
        IScheduler scheduler,
        CQRSConfig config)
    {
        _snapshotManager = snapshotManager;
        _journal = journal;
        _scheduler = scheduler;
        _config = config;
    }

    public IPipeline CreatePipeline(IMessageBus bus, IWorld world)
    {
        return new Pipeline(bus, world, _snapshotManager, _journal, _scheduler, _config);
    }
}
