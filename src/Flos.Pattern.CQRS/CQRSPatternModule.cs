using Flos.Core.Messaging;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.State;
using Flos.Snapshot;

namespace Flos.Pattern.CQRS;

/// <summary>Module that bootstraps the CQRS pattern. Registers the pipeline, handler registry, event journal, and CQRS middleware.</summary>
public sealed class CQRSPatternModule : ModuleBase
{
    private IMessageBus? _bus;
    private IPipeline? _pipeline;

    /// <inheritdoc />
    public override string Id => "CQRS";

    /// <inheritdoc />
    public override IReadOnlyList<string> Dependencies => ["Snapshot"];

    /// <inheritdoc />
    public override void OnLoad(IServiceScope scope)
    {
        base.OnLoad(scope);

        var patternRegistry = scope.Resolve<IPatternRegistry>();
        patternRegistry.Register(CQRSPattern.Id);

        var config = new CQRSConfig();
        scope.RegisterInstance(config);

        var journal = new EventJournal();
        scope.RegisterInstance<IEventJournal>(journal);

        if (!scope.IsRegistered<ICQRSAdapter>())
        {
            scope.RegisterFactory<ICQRSAdapter>(s =>
            {
                var snapshotManager = s.Resolve<ISnapshotManager>();
                var scheduler = s.Resolve<IScheduler>();
                return new BuiltInCQRSAdapter(snapshotManager, journal, scheduler, config);
            });
        }

        scope.RegisterFactory<IPipeline>(s =>
        {
            var adapter = s.Resolve<ICQRSAdapter>();
            var bus = s.Resolve<IMessageBus>();
            var world = s.Resolve<IWorld>();
            return adapter.CreatePipeline(bus, world);
        });

        scope.RegisterFactory<IHandlerRegistry>(s =>
        {
            var pipeline = s.Resolve<IPipeline>();
            if (pipeline is IHandlerRegistry registry)
                return registry;
            throw new InvalidOperationException("The CQRS pipeline does not implement IHandlerRegistry.");
        });
    }

    /// <inheritdoc />
    public override void OnInitialize()
    {
        var scope = Scope!;
        _bus = scope.Resolve<IMessageBus>();
        _pipeline = scope.Resolve<IPipeline>();

        _bus.Use(new CQRSMiddleware(_pipeline));
    }
}
