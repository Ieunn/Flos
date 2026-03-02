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
    private IServiceScope? _rootScope;
    private IWorld? _world;
    private IScheduler? _scheduler;
    private IMessageBus? _bus;
    private IReadOnlyList<IModule>? _sortedModules;
    private List<IModule>? _loadedModules;

    /// <inheritdoc />
    public SessionState State => _state;

    /// <inheritdoc />
    public IWorld World => _world ?? throw new InvalidOperationException("Session not initialized.");

    /// <inheritdoc />
    public IScheduler Scheduler => _scheduler ?? throw new InvalidOperationException("Session not initialized.");

    /// <inheritdoc />
    public IMessageBus MessageBus => _bus ?? throw new InvalidOperationException("Session not initialized.");

    /// <inheritdoc />
    public IServiceScope RootScope => _rootScope ?? throw new InvalidOperationException("Session not initialized.");

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown with <see cref="CoreErrors.InitializationFailed"/> when module loading or initialization fails.</exception>
    public void Initialize(SessionConfig config)
    {
        if (_state != SessionState.Created)
        {
            if (CoreLog.Handler is not null)
                CoreLog.Warn($"Initialize() called in state {_state}. Ignored.");
            return;
        }

        _state = SessionState.Initializing;

        var dispatcher = new Dispatcher();

        _rootScope = config.DIAdapter?.CreateRootScope() ?? new BuiltInMinimalScope();

        _rootScope.RegisterInstance<SessionConfig>(config);
        _rootScope.RegisterInstance<IDispatcher>(dispatcher);

        var bus = new MessageBus();
        _bus = bus;
        _rootScope.RegisterInstance<IMessageBus>(bus);

        var world = new World();
        _world = world;
        _rootScope.RegisterInstance<IWorld>(world);

        var patternRegistry = new PatternRegistry();
        _rootScope.RegisterInstance<IPatternRegistry>(patternRegistry);

        var scheduler = new Scheduler(config.TickMode, config.FixedTimeStep, bus, dispatcher);
        _scheduler = scheduler;
        _rootScope.RegisterInstance<IScheduler>(scheduler);

        _loadedModules = [];
        try
        {
            _sortedModules = ModuleLoader.TopologicalSort(config.Modules);

            foreach (var module in _sortedModules)
            {
                module.OnLoad(_rootScope);
                _loadedModules.Add(module);
            }

            ModuleLoader.ValidatePatterns(_sortedModules, patternRegistry);

            if (_rootScope.IsRegistered<ISession>())
            {
                throw new FlosException(CoreErrors.InitializationFailed,
                    "ISession must not be registered in IServiceScope. Modules should not hold session references.");
            }

            _rootScope.Lock();

            foreach (var module in _sortedModules)
            {
                module.OnInitialize();
            }
        }
        catch (Exception ex)
        {
            for (int i = _loadedModules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _loadedModules[i].OnShutdown();
                }
                catch (Exception shutdownEx)
                {
                    if (CoreLog.Handler is not null)
                        CoreLog.Warn($"Exception during rollback shutdown of module '{_loadedModules[i].Id}': {shutdownEx.Message}");
                }
            }

            _rootScope.Dispose();
            _rootScope = null;
            _world = null;
            _scheduler = null;
            _bus = null;
            _sortedModules = null;
            _loadedModules = null;
            _state = SessionState.Created;

            throw new FlosException(CoreErrors.InitializationFailed,
                $"Session initialization failed: {ex.Message}");
        }

        _state = SessionState.Initialized;
        _bus.Publish(new SessionInitializedMessage());
    }

    /// <inheritdoc />
    public void Start()
    {
        if (_state != SessionState.Initialized)
        {
            if (CoreLog.Handler is not null)
                CoreLog.Warn($"Start() called in state {_state}. Ignored.");
            return;
        }

        foreach (var module in _sortedModules!)
        {
            module.OnStart();
        }

        _state = SessionState.Running;
        _bus!.Publish(new SessionStartedMessage());
    }

    /// <inheritdoc />
    public void Pause()
    {
        if (_state != SessionState.Running)
        {
            if (CoreLog.Handler is not null)
                CoreLog.Warn($"Pause() called in state {_state}. Ignored.");
            return;
        }

        _state = SessionState.Paused;
        _scheduler!.SetPaused(true);

        foreach (var module in _sortedModules!)
        {
            module.OnPause();
        }

        _bus!.Publish(new SessionPausedMessage());
    }

    /// <inheritdoc />
    public void Resume()
    {
        if (_state != SessionState.Paused)
        {
            if (CoreLog.Handler is not null)
                CoreLog.Warn($"Resume() called in state {_state}. Ignored.");
            return;
        }

        _state = SessionState.Running;
        _scheduler!.SetPaused(false);

        foreach (var module in _sortedModules!)
        {
            module.OnResume();
        }

        _scheduler!.DrainPausedBuffer();

        _bus!.Publish(new SessionResumedMessage());
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        if (_state is SessionState.ShuttingDown or SessionState.Disposed or SessionState.Created)
        {
            if (CoreLog.Handler is not null)
                CoreLog.Warn($"Shutdown() called in state {_state}. Ignored.");
            return;
        }

        _state = SessionState.ShuttingDown;

        if (_sortedModules is not null)
        {
            for (int i = _sortedModules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _sortedModules[i].OnShutdown();
                }
                catch (Exception ex)
                {
                    if (CoreLog.Handler is not null)
                        CoreLog.Warn($"Exception during shutdown of module '{_sortedModules[i].Id}': {ex.Message}");
                }
            }
        }

        _bus!.Publish(new SessionShutdownMessage());
    }

    /// <summary>
    /// Shuts down the session if needed and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_state == SessionState.Disposed)
            return;

        if (_state is not (SessionState.ShuttingDown or SessionState.Created))
        {
            Shutdown();
        }

        _rootScope?.Dispose();
        _rootScope = null;
        _world = null;
        _scheduler = null;
        _bus = null;
        _sortedModules = null;
        _loadedModules = null;
        _state = SessionState.Disposed;
    }
}
