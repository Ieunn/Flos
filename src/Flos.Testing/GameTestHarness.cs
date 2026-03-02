using Flos.Core.Errors;
using Flos.Core.Module;
using Flos.Core.Scheduling;
using Flos.Core.Sessions;
using Flos.Core.State;
using Flos.Pattern.CQRS;
using Flos.Random;
using Flos.Identity;
using Flos.Snapshot;

namespace Flos.Testing;

/// <summary>
/// Fluent API for integration-testing game logic.
/// Each method returns <c>this</c> for chaining.
/// </summary>
public sealed class GameTestHarness : IDisposable
{
    private IModule[] _modules = [];
    private int _seed;
    private TickMode _tickMode = TickMode.StepBased;
    private Session? _session;
    private IPipeline? _pipeline;
    private EventCaptureModule? _captureModule;
    private readonly List<RecordedCommand> _recordedCommands = [];
    private Result<IReadOnlyList<IEvent>>? _lastSendResult;

    /// <summary>
    /// Configures the modules for the session. The harness automatically includes
    /// RandomModule, IdentityModule, SnapshotModule, and CQRSPatternModule.
    /// Pass only your game modules here. Module instances are reused across replays
    /// (each session re-initializes them via <see cref="IModule.OnLoad"/>).
    /// </summary>
    public GameTestHarness WithModules(params IModule[] modules)
    {
        _modules = modules;
        return this;
    }

    /// <summary>
    /// Sets the random seed for deterministic execution.
    /// </summary>
    public GameTestHarness WithSeed(int seed)
    {
        _seed = seed;
        return this;
    }

    /// <summary>
    /// Sets the tick mode. Default is <see cref="TickMode.StepBased"/>.
    /// </summary>
    public GameTestHarness WithTickMode(TickMode mode)
    {
        _tickMode = mode;
        return this;
    }

    /// <summary>
    /// Creates and starts the session with all configured modules.
    /// Must be called before Execute, Tick, or any Assert methods.
    /// </summary>
    public GameTestHarness Build()
    {
        _captureModule = new EventCaptureModule();

        var allModules = new List<IModule>
        {
            _captureModule,
            new RandomModule(),
            new IdentityModule(),
            new SnapshotModule(),
            new CQRSPatternModule()
        };
        allModules.AddRange(_modules);

        _session = new Session();
        _session.Initialize(new SessionConfig
        {
            Modules = allModules,
            TickMode = _tickMode,
            RandomSeed = _seed
        });
        _session.Start();

        _pipeline = _session.RootScope.Resolve<IPipeline>();

        return this;
    }

    /// <summary>
    /// Sends a command through the CQRS pipeline.
    /// </summary>
    public GameTestHarness Execute(ICommand command)
    {
        EnsureStarted();

        _recordedCommands.Add(new RecordedCommand(_session!.Scheduler.CurrentTick, command));
        _lastSendResult = _pipeline!.Send(command);
        return this;
    }

    /// <summary>
    /// Advances the scheduler by <paramref name="count"/> ticks (StepBased mode).
    /// </summary>
    public GameTestHarness Tick(int count = 1)
    {
        EnsureStarted();
        for (int i = 0; i < count; i++)
        {
            _session!.Scheduler.Step();
        }
        return this;
    }

    /// <summary>
    /// Asserts that the current state of slice <typeparamref name="T"/> satisfies the predicate.
    /// </summary>
    public GameTestHarness AssertState<T>(Func<T, bool> predicate, string? msg = null)
        where T : class, IStateSlice
    {
        EnsureStarted();
        var slice = _session!.World.Get<T>();
        if (!predicate(slice))
        {
            throw new TestAssertionException(
                msg ?? $"State assertion failed for '{typeof(T).Name}'. Current state: {slice}");
        }
        return this;
    }

    /// <summary>
    /// Asserts that at least one event of type <typeparamref name="T"/> was emitted
    /// (optionally matching a predicate).
    /// </summary>
    public GameTestHarness AssertEventEmitted<T>(Func<T, bool>? predicate = null) where T : IEvent
    {
        EnsureStarted();
        var matching = _captureModule!.CapturedEvents.OfType<T>();
        if (predicate is not null)
            matching = matching.Where(e => predicate(e));

        if (!matching.Any())
        {
            throw new TestAssertionException(
                $"Expected event '{typeof(T).Name}' to be emitted, but it was not found.");
        }
        return this;
    }

    /// <summary>
    /// Asserts that no event of type <typeparamref name="T"/> was emitted.
    /// </summary>
    public GameTestHarness AssertNoEventEmitted<T>() where T : IEvent
    {
        EnsureStarted();
        if (_captureModule!.CapturedEvents.OfType<T>().Any())
        {
            throw new TestAssertionException(
                $"Expected no event '{typeof(T).Name}', but one was found.");
        }
        return this;
    }

    /// <summary>
    /// Asserts that the last command was rejected with the specified error code.
    /// </summary>
    public GameTestHarness AssertRejected(ErrorCode error)
    {
        EnsureStarted();

        if (_lastSendResult is null || _lastSendResult.Value.IsSuccess)
        {
            throw new TestAssertionException(
                $"Expected command rejection with error {error}, but no rejection occurred.");
        }

        if (_lastSendResult.Value.Error != error)
        {
            throw new TestAssertionException(
                $"Expected rejection error {error}, " +
                $"but got {_lastSendResult.Value.Error}.");
        }
        return this;
    }

    /// <summary>
    /// Replays the entire command sequence <paramref name="replayCount"/> times with the same seed,
    /// asserting identical final state. Uses deep comparison via snapshot.
    /// </summary>
    public GameTestHarness AssertDeterministic(int replayCount)
    {
        EnsureStarted();

        var referenceState = ReplayVerifier.CaptureWorldState(_session!.World);

        for (int i = 0; i < replayCount; i++)
        {
            var config = new SessionConfig
            {
                Modules = BuildModuleList(),
                TickMode = _tickMode,
                RandomSeed = _seed
            };

            var replayState = RunSingleReplay(config);
            ReplayVerifier.CompareSnapshots(referenceState, replayState, i + 1);
        }

        return this;
    }

    /// <summary>
    /// Returns all captured events since the session started.
    /// </summary>
    public IReadOnlyList<IEvent> GetEmittedEvents()
    {
        EnsureStarted();
        return _captureModule!.CapturedEvents;
    }

    /// <summary>
    /// Returns the current state of slice <typeparamref name="T"/> for manual inspection.
    /// </summary>
    public T GetState<T>() where T : class, IStateSlice
    {
        EnsureStarted();
        return _session!.World.Get<T>();
    }

    public void Dispose()
    {
        _session?.Dispose();
    }

    private void EnsureStarted()
    {
        if (_session is null)
            throw new InvalidOperationException("GameTestHarness: call Build() before using the harness.");
    }

    private IReadOnlyList<IModule> BuildModuleList()
    {
        var allModules = new List<IModule>
        {
            new EventCaptureModule(),
            new RandomModule(),
            new IdentityModule(),
            new SnapshotModule(),
            new CQRSPatternModule()
        };
        allModules.AddRange(_modules);
        return allModules;
    }

    private Dictionary<Type, IStateSlice> RunSingleReplay(SessionConfig config)
    {
        using var session = new Session();
        session.Initialize(config);
        session.Start();

        var pipeline = session.RootScope.Resolve<IPipeline>();
        long currentTick = 0;

        for (int i = 0; i < _recordedCommands.Count; i++)
        {
            var recorded = _recordedCommands[i];
            while (currentTick < recorded.Tick)
            {
                session.Scheduler.Step();
                currentTick++;
            }
            pipeline.Send(recorded.Command);
        }

        return ReplayVerifier.CaptureWorldState(session.World);
    }
}
