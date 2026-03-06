using Flos.Adapter;
using Flos.Core.Logging;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Diagnostics;

namespace Flos.Adapter.Unity
{
    /// <summary>
    /// Unity adapter module. Wires Unity engine bridges into the Flos session.
    /// Follows the same pattern as <c>ConsoleAdapterModule</c>.
    /// </summary>
    public sealed class UnityAdapterModule : ModuleBase
    {
        public override string Id => "Adapter.Unity";

        private readonly UnitySaveBridge _saveBridge = new UnitySaveBridge();
        private readonly UnityProfilerBridge _profilerBridge = new UnityProfilerBridge();
        private readonly UnityAssetBridge _assetBridge = new UnityAssetBridge();

        public override void OnLoad(ILoadScope scope)
        {
            base.OnLoad(scope);

            CoreLog.Handler = UnityLogBridge.Handler;

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

        public override void OnShutdown()
        {
            CoreLog.Handler = null;
        }
    }
}
