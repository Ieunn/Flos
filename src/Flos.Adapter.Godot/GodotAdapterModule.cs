using Flos.Core.Logging;
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

    public override void OnLoad(IServiceScope scope)
    {
        base.OnLoad(scope);

        CoreLog.Handler = GodotLogBridge.Handler;

        scope.RegisterInstance<IProfiler>(_profilerBridge);
        scope.RegisterInstance<ISaveStorage>(_saveBridge);
        scope.RegisterInstance<IAssetProvider>(_assetBridge);
    }

    public override void OnInitialize()
    {
        var dispatcher = Scope.Resolve<IDispatcher>();

        _saveBridge.Initialize(dispatcher);
        _assetBridge.Initialize(dispatcher);
    }

    public override void OnShutdown()
    {
        CoreLog.Handler = null;
    }
}
