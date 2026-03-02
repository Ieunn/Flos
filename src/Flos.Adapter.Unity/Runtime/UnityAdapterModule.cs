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

        public override void OnLoad(IServiceScope scope)
        {
            base.OnLoad(scope);

            CoreLog.Handler = UnityLogBridge.Handler;

            scope.RegisterInstance<IProfiler>(_profilerBridge);
            scope.RegisterInstance<ISaveStorage>(_saveBridge);
        }

        public override void OnInitialize()
        {
            var dispatcher = Scope.Resolve<IDispatcher>();

            _saveBridge.Initialize(dispatcher);
        }

        public override void OnShutdown()
        {
            CoreLog.Handler = null;
        }
    }
}
