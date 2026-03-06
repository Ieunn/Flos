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

    public override string Id => "ECS";
    public override IReadOnlyList<string> Dependencies => Array.Empty<string>();

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

    public override void OnLoad(IServiceRegistry scope)
    {
        base.OnLoad(scope);

        Scope.Resolve<IPatternRegistry>().Register(ECSPattern.Id);

        Scope.Register<ICommandBuffer>(_commandBuffer);
        Scope.Register<IECSAdapter>(_adapter);
    }

    public override void OnInitialize()
    {
        var world = Scope.Resolve<IWorld>();
        _adapter.CreateWorld(world);

        _bus = Scope.Resolve<IMessageBus>();
        _tickSub = _bus.Listen<TickMessage>(OnTick, _tickPriority);
    }

    public override void OnShutdown()
    {
        _tickSub?.Dispose();
        _tickSub = null;
        _adapter.Shutdown();
    }

    private void OnTick(TickMessage tick)
    {
        try
        {
            _adapter.Tick(tick.DeltaTime);
        }
        finally
        {
            _commandBuffer.Drain(_bus!);
        }
    }
}
