using Flos.Core.Messaging;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>Module that bootstraps the CQRS pattern. Registers the pipeline, handler registry, event journal, and CQRS middleware.</summary>
public sealed class CQRSPatternModule : ModuleBase
{
    /// <inheritdoc />
    public override string Id => "CQRS";

    /// <inheritdoc />
    public override IReadOnlyList<string> Dependencies => Array.Empty<string>();

    private IPipeline? _pipeline;
    private IHandlerRegistry? _registry;

    /// <inheritdoc />
    public override void OnLoad(ILoadScope scope)
    {
        base.OnLoad(scope);

        scope.Patterns.Register(CQRSPattern.Id);

        scope.TryRegister(new CQRSConfig());

        scope.Register<IEventJournal>(s =>
        {
            var config = s.Resolve<CQRSConfig>();
            return new EventJournal { MaxEntries = config.MaxJournalEntries };
        });

        scope.TryRegister<ICQRSAdapter>(s =>
        {
            s.TryResolve<IRollbackProvider>(out var rollbackProvider);
            var config = s.Resolve<CQRSConfig>();
            var journal = s.Resolve<IEventJournal>();
            var scheduler = s.Resolve<IScheduler>();
            return new BuiltInCQRSAdapter(rollbackProvider, journal, scheduler, config);
        });

        scope.Register<IPipeline>(s =>
        {
            EnsurePipelineCreated(s);
            return _pipeline!;
        });

        scope.Register<IHandlerRegistry>(s =>
        {
            EnsurePipelineCreated(s);
            return _registry!;
        });

        // Register middleware early — the bus is already an instance in the scope at this point.
        // The DeferredCQRSMiddleware delays pipeline resolution until the first command is received,
        // ensuring the scope is locked and all factories are resolvable.
        scope.Bus.Use(new DeferredCQRSMiddleware(Scope));
    }

    private void EnsurePipelineCreated(IServiceRegistry scope)
    {
        if (_pipeline is not null) return;

        var adapter = scope.Resolve<ICQRSAdapter>();
        var bus = scope.Resolve<IMessageBus>();
        var world = scope.Resolve<IWorld>();
        var result = adapter.CreatePipeline(bus, world);
        _pipeline = result.Pipeline;
        _registry = result.Registry;
    }
}
