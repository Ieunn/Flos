using Flos.Core.Messaging;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.State;

namespace Flos.Pattern.ECS;

/// <summary>
/// Module that integrates an ECS adapter with the Flos session lifecycle.
/// Registers ECSPattern.Id, subscribes to TickMessage to drive the adapter,
/// and drains the CommandBuffer after each tick.
/// </summary>
public sealed class ECSPatternModule : ModuleBase
{
    private readonly IECSAdapter _adapter;
    private readonly CommandBuffer _commandBuffer;
    private readonly int _tickPriority;

    private IMessageBus? _bus;
    private IDisposable? _tickSub;
    private bool _paused;

    public override string Id => "ECS";
    public override IReadOnlyList<string> Dependencies => [];

    /// <summary>
    /// Creates the ECS pattern module.
    /// </summary>
    /// <param name="adapter">The ECS framework adapter.</param>
    /// <param name="commandBuffer">Optional shared command buffer. If null, a new one is created.</param>
    /// <param name="tickPriority">Priority for TickMessage subscription. Lower = earlier. Default 100.</param>
    public ECSPatternModule(IECSAdapter adapter, CommandBuffer? commandBuffer = null, int tickPriority = 100)
    {
        _adapter = adapter;
        _commandBuffer = commandBuffer ?? new CommandBuffer();
        _tickPriority = tickPriority;
    }

    public override void OnLoad(IServiceScope scope)
    {
        base.OnLoad(scope);

        var patternRegistry = scope.Resolve<IPatternRegistry>();
        patternRegistry.Register(ECSPattern.Id);

        scope.RegisterInstance<ICommandBuffer>(_commandBuffer);
        scope.RegisterInstance<IECSAdapter>(_adapter);

        var world = scope.Resolve<IWorld>();
        _adapter.CreateWorld(world);

        _bus = scope.Resolve<IMessageBus>();
    }

    public override void OnInitialize()
    {
        _tickSub = _bus!.Listen<TickMessage>(OnTick, _tickPriority);
    }

    public override void OnPause()
    {
        _paused = true;
    }

    public override void OnResume()
    {
        _paused = false;
    }

    public override void OnShutdown()
    {
        _tickSub?.Dispose();
        _tickSub = null;
        _adapter.Shutdown();
    }

    private void OnTick(TickMessage tick)
    {
        if (_paused) return;

        _adapter.Tick(tick.DeltaTime);

        _commandBuffer.Drain(_bus!);
    }
}
