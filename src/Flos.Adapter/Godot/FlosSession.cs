using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.Sessions;

namespace Flos.Adapter.Godot;

/// <summary>
/// Node that owns and drives an <see cref="ISession"/>.
/// Add as a child node to bootstrap Flos in Godot.
/// Subclass and override <see cref="GetModules"/> to add game modules.
/// </summary>
public abstract partial class FlosSession : Node
{
    [Export] private TickMode _tickMode = TickMode.FixedTick;
    [Export] private float _fixedTimeStep = 1f / 60f;
    [Export] private bool _autoInitialize = true;
    [Export] private bool _pauseOnFocusLoss;

    private ISession? _session;
    private bool _started;

    /// <summary>The Flos session. Null until <see cref="Initialize"/> is called.</summary>
    public ISession? Session => _session;

    /// <summary>
    /// Optional scope factory. Set before <see cref="Initialize"/> to use a custom DI container.
    /// </summary>
    public IScopeFactory? ScopeFactory { get; set; }

    public override void _Ready()
    {
        if (_autoInitialize)
        {
            Initialize();
            StartSession();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_tickMode == TickMode.FixedTick && _session != null && _session.State == SessionState.Running)
        {
            _session.Scheduler.Tick((float)delta);
        }
    }

    /// <summary>
    /// Advance one tick manually. Only meaningful when <see cref="TickMode"/> is <see cref="TickMode.StepBased"/>.
    /// </summary>
    public void Step()
    {
        if (_tickMode == TickMode.StepBased && _session != null && _session.State == SessionState.Running)
        {
            _session.Scheduler.Step();
        }
    }

    public override void _Notification(int what)
    {
        if (_session == null) return;

        if (what == NotificationApplicationPaused && _session.State == SessionState.Running)
            _session.Pause();
        else if (what == NotificationApplicationResumed && _session.State == SessionState.Paused)
            _session.Resume();

        if (_pauseOnFocusLoss)
        {
            if (what == NotificationWMWindowFocusOut && _session.State == SessionState.Running)
                _session.Pause();
            else if (what == NotificationWMWindowFocusIn && _session.State == SessionState.Paused)
                _session.Resume();
        }
    }

    public override void _ExitTree()
    {
        if (_session != null)
        {
            if (_session.State == SessionState.Running || _session.State == SessionState.Paused)
                _session.Shutdown();
            _session.Dispose();
            _session = null;
        }
    }

    /// <summary>
    /// Create and initialize the session. Called automatically if <c>autoInitialize</c> is true.
    /// </summary>
    public void Initialize()
    {
        if (_session != null) return;

        _session = new Session();
        _session.Initialize(new SessionConfig
        {
            Modules = GetModules(),
            TickMode = _tickMode,
            FixedTimeStep = _fixedTimeStep,
            ScopeFactory = ScopeFactory,
        });
    }

    /// <summary>
    /// Start the session. Called automatically after <see cref="Initialize"/> if <c>autoInitialize</c> is true.
    /// </summary>
    public void StartSession()
    {
        if (_started || _session == null) return;
        _started = true;
        _session.Start();
    }

    /// <summary>
    /// Override to provide game modules. Must include <see cref="GodotAdapterModule"/> and any game-specific modules.
    /// </summary>
    protected abstract IReadOnlyList<IModule> GetModules();
}
