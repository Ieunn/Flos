using Flos.Core.Errors;
using Flos.Core.Logging;
using Flos.Core.Messaging;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.State;

namespace Flos.Core.Sessions;

/// <summary>
/// Default implementation of <see cref="ISession"/>.
/// </summary>
public sealed class Session : ISession
{
    private SessionState _state = SessionState.Created;
    private readonly ThreadGuard _threadGuard = new("Session");
    private IServiceRegistry? _rootScope;
    private IWorld? _world;
    private IScheduler? _scheduler;
    private IMessageBus? _bus;
    private IReadOnlyList<IModule>? _sortedModules;
    private List<IModule>? _loadedModules;

    /// <inheritdoc />
    public SessionState State => _state;

    /// <inheritdoc />
    public IWorld World => _state == SessionState.Disposed
        ? throw new FlosException(CoreErrors.SessionDisposed, "Session has been disposed.")
        : _world ?? throw new FlosException(CoreErrors.SessionNotInitialized, "Session not initialized.");

    /// <inheritdoc />
    public IScheduler Scheduler => _state == SessionState.Disposed
        ? throw new FlosException(CoreErrors.SessionDisposed, "Session has been disposed.")
        : _scheduler ?? throw new FlosException(CoreErrors.SessionNotInitialized, "Session not initialized.");

    /// <inheritdoc />
    public IMessageBus MessageBus => _state == SessionState.Disposed
        ? throw new FlosException(CoreErrors.SessionDisposed, "Session has been disposed.")
        : _bus ?? throw new FlosException(CoreErrors.SessionNotInitialized, "Session not initialized.");

    /// <inheritdoc />
    public IServiceRegistry RootScope => _state == SessionState.Disposed
        ? throw new FlosException(CoreErrors.SessionDisposed, "Session has been disposed.")
        : _rootScope ?? throw new FlosException(CoreErrors.SessionNotInitialized, "Session not initialized.");

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.InitializationFailed"/> when module loading or initialization fails.</exception>
    public void Initialize(SessionConfig config)
    {
        _threadGuard.Assert();
        if (_state != SessionState.Created)
        {
            CoreLog.Warn($"Initialize() called in state {_state}. Ignored.");
            return;
        }

        if (config.TickMode == TickMode.FixedTick && config.FixedTimeStep <= 0.0)
        {
            throw new FlosException(CoreErrors.InvalidConfiguration,
                $"FixedTimeStep must be greater than 0 in FixedTick mode, but was {config.FixedTimeStep}.");
        }

        _state = SessionState.Initializing;

#if DEBUG
        FlosDebug.EnableForCurrentThread();
#endif

        var dispatcher = new Dispatcher();

        _rootScope = config.ScopeFactory?.CreateRootScope() ?? new BuiltInMinimalScope();

        _rootScope.Register<SessionConfig>(config);
        _rootScope.Register<IDispatcher>(dispatcher);

        var bus = new MessageBus();
        _bus = bus;
        _rootScope.Register<IMessageBus>(bus);

        var world = new World();
        _world = world;
        _rootScope.Register<IWorld>(world);
        _rootScope.Register<IStateReader>(world);

        var patternRegistry = new PatternRegistry();
        _rootScope.Register<IPatternRegistry>(patternRegistry);

        var scheduler = new Scheduler(config.TickMode, config.FixedTimeStep, bus, dispatcher);
        _scheduler = scheduler;
        _rootScope.Register<IScheduler>(scheduler);

        _loadedModules = new List<IModule>();
        try
        {
            _sortedModules = ModuleLoader.TopologicalSort(config.Modules);

            var loadScope = new LoadScope(_rootScope, world, bus, scheduler, dispatcher, patternRegistry, config);
            foreach (var module in _sortedModules)
            {
                module.OnLoad(loadScope);
                _loadedModules.Add(module);
            }

            ModuleLoader.ValidatePatterns(_sortedModules, patternRegistry);

            if (_rootScope.IsRegistered<ISession>())
            {
                throw new FlosException(CoreErrors.InitializationFailed,
                    "ISession must not be registered in IServiceRegistry. Modules should not hold session references.");
            }

            _rootScope.Lock();

            foreach (var module in _sortedModules)
            {
                module.OnInitialize();
            }
        }
        catch (Exception ex)
        {
            ShutdownModules(_loadedModules);
            ReleaseResources();
            _state = SessionState.Disposed;

            throw new FlosException(CoreErrors.InitializationFailed,
                $"Session initialization failed: {ex.Message}", ex);
        }

        _state = SessionState.Initialized;
        SafePublish(new SessionInitializedMessage());
    }

    /// <inheritdoc />
    public void Start()
    {
        _threadGuard.Assert();
        if (_state != SessionState.Initialized)
        {
            CoreLog.Warn($"Start() called in state {_state}. Ignored.");
            return;
        }

        for (int i = 0; i < _sortedModules!.Count; i++)
        {
            try
            {
                _sortedModules[i].OnStart();
            }
            catch (Exception ex)
            {
                CoreLog.Error($"Exception during OnStart of module '{_sortedModules[i].Id}': {ex.Message}");

                try { _bus!.Publish(new SessionShutdownMessage()); }
                catch (Exception msgEx)
                {
                    CoreLog.Warn($"Exception during SessionShutdownMessage on Start rollback: {msgEx.Message}");
                }

                ShutdownModules(_sortedModules, startInclusive: 0, endInclusive: i);
                ReleaseResources();
                _state = SessionState.Disposed;

                throw new FlosException(CoreErrors.InitializationFailed,
                    $"Session start failed: {ex.Message}", ex);
            }
        }

        _state = SessionState.Running;
        SafePublish(new SessionStartedMessage());
    }

    /// <inheritdoc />
    public void Pause()
    {
        _threadGuard.Assert();
        if (_state != SessionState.Running)
        {
            CoreLog.Warn($"Pause() called in state {_state}. Ignored.");
            return;
        }

        _state = SessionState.Paused;
        _scheduler!.SetPaused(true);

        for (int i = 0; i < _sortedModules!.Count; i++)
        {
            try
            {
                _sortedModules[i].OnPause();
            }
            catch (Exception ex)
            {
                CoreLog.Warn($"Exception during OnPause of module '{_sortedModules[i].Id}': {ex.Message}");
            }
        }

        SafePublish(new SessionPausedMessage());
    }

    /// <inheritdoc />
    public void Resume()
    {
        _threadGuard.Assert();
        if (_state != SessionState.Paused)
        {
            CoreLog.Warn($"Resume() called in state {_state}. Ignored.");
            return;
        }

        _state = SessionState.Running;
        _scheduler!.SetPaused(false);

        for (int i = 0; i < _sortedModules!.Count; i++)
        {
            try
            {
                _sortedModules[i].OnResume();
            }
            catch (Exception ex)
            {
                CoreLog.Warn($"Exception during OnResume of module '{_sortedModules[i].Id}': {ex.Message}");
            }
        }

        _scheduler!.DrainPausedBuffer();

        SafePublish(new SessionResumedMessage());
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        _threadGuard.Assert();
        if (_state is SessionState.Disposed or SessionState.Created)
        {
            CoreLog.Warn($"Shutdown() called in state {_state}. Ignored.");
            return;
        }

        _state = SessionState.ShuttingDown;
        SafePublish(new SessionShutdownMessage());

        ShutdownModules(_sortedModules);
        ReleaseResources();
        _state = SessionState.Disposed;
    }

    private void SafePublish<T>(T message) where T : IMessage
    {
        try
        {
            _bus!.Publish(message);
        }
        catch (Exception ex)
        {
            CoreLog.Warn($"Exception during {typeof(T).Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Shuts down the session if needed and releases all resources.
    /// </summary>
    public void Dispose()
    {
        _threadGuard.Assert();
        if (_state == SessionState.Disposed)
            return;

        if (_state == SessionState.Created)
        {
            ReleaseResources();
            _state = SessionState.Disposed;
            return;
        }

        Shutdown();
    }

    private static void ShutdownModules(IReadOnlyList<IModule>? modules, int startInclusive = 0, int endInclusive = -1)
    {
        if (modules is null) return;

        if (endInclusive < 0)
        {
            endInclusive = modules.Count - 1;
        }

        for (int i = endInclusive; i >= startInclusive; i--)
        {
            try
            {
                modules[i].OnShutdown();
            }
            catch (Exception ex)
            {
                CoreLog.Warn($"Exception during shutdown of module '{modules[i].Id}': {ex.Message}");
            }
        }
    }

    private void ReleaseResources()
    {
        _rootScope?.Dispose();
        _rootScope = null;
        _world = null;
        _scheduler = null;
        _bus = null;
        _sortedModules = null;
        _loadedModules = null;
    }
}
