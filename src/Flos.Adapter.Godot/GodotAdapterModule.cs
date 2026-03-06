using Flos.Core.Logging;
using Flos.Core.Messaging;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Diagnostics;

namespace Flos.Adapter.Godot;

/// <summary>
/// Godot adapter module. Wires Godot engine bridges into the Flos session.
/// Follows the same pattern as <c>ConsoleAdapterModule</c> and <c>UnityAdapterModule</c>.
/// </summary>
public sealed class GodotAdapterModule : ModuleBase
{
    public override string Id => "Adapter.Godot";

    private readonly GodotSaveBridge _saveBridge = new();
    private readonly GodotAssetBridge _assetBridge = new();
    private readonly GodotProfilerBridge _profilerBridge = new();
    private IMessageBus? _bus;
    private int _tickSubscriptionId;

    public override void OnLoad(ILoadScope scope)
    {
        base.OnLoad(scope);

        CoreLog.Handler = GodotLogBridge.Handler;

        scope.Register<IProfiler>(_profilerBridge);
        scope.Register<ISaveStorage>(_saveBridge);
        scope.Register<IAssetProvider>(_assetBridge);
    }

    public override void OnInitialize()
    {
        var dispatcher = Scope.Resolve<IDispatcher>();

        _saveBridge.Initialize(dispatcher);
        _assetBridge.Initialize(dispatcher);
    }

    public override void OnStart()
    {
        _bus = Scope.Resolve<IMessageBus>();
        _tickSubscriptionId = _bus.Subscribe<TickMessage>(OnTick);
    }

    public override void OnShutdown()
    {
        _bus?.Unsubscribe<TickMessage>(_tickSubscriptionId);
        _bus = null;
        CoreLog.Handler = null;
    }

    private void OnTick(TickMessage _)
    {
        _assetBridge.Poll();
    }
}
