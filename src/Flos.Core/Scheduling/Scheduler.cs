using System.Diagnostics;
using System.Runtime.CompilerServices;
using Flos.Core.Errors;
using Flos.Core.Logging;
using Flos.Core.Messaging;

namespace Flos.Core.Scheduling;

/// <summary>
/// Default implementation of <see cref="IScheduler"/>. Publishes <see cref="TickMessage"/>
/// through the <see cref="Messaging.IMessageBus"/>.
/// </summary>
public sealed class Scheduler : IScheduler
{
    private readonly IMessageBus _bus;
    private readonly IDispatcher _dispatcher;
    private readonly float _fixedTimeStep;
    private float _accumulator;
    private bool _isTicking;
    private bool _isPaused;
    private int _pausedStepBuffer;
    private float _pausedTimeBuffer;
    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;

    [Conditional("DEBUG")]
    private void AssertMainThread([CallerMemberName] string? caller = null)
    {
        Debug.Assert(Environment.CurrentManagedThreadId == _ownerThreadId,
            $"Scheduler.{caller}() called from thread {Environment.CurrentManagedThreadId}, " +
            $"expected main thread {_ownerThreadId}. Use IDispatcher.Enqueue() for cross-thread access.");
    }

    /// <inheritdoc />
    public TickMode Mode { get; }

    /// <inheritdoc />
    public long CurrentTick { get; private set; }

    /// <inheritdoc />
    public float ElapsedTime { get; private set; }

    /// <inheritdoc />
    public int MaxCatchUpTicks
    {
        get => _maxCatchUpTicks;
        set => _maxCatchUpTicks = Math.Max(1, value);
    }
    private int _maxCatchUpTicks = 5;

    /// <inheritdoc />
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Initializes a new <see cref="Scheduler"/>.
    /// </summary>
    /// <param name="mode">The tick mode this scheduler operates in.</param>
    /// <param name="fixedTimeStep">Seconds per tick in <see cref="TickMode.FixedTick"/> mode.</param>
    /// <param name="bus">The message bus used to publish <see cref="TickMessage"/>.</param>
    /// <param name="dispatcher">The dispatcher drained at the start of each tick.</param>
    public Scheduler(TickMode mode, float fixedTimeStep, IMessageBus bus, IDispatcher dispatcher)
    {
        Mode = mode;
        _fixedTimeStep = fixedTimeStep;
        _bus = bus;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown when a reentrant tick is detected (<see cref="CoreErrors.ReentrantTick"/>).</exception>
    public void Step()
    {
        AssertMainThread();
        if (Mode != TickMode.StepBased)
        {
            CoreLog.Warn("Step() called in FixedTick mode. Ignored.");
            return;
        }

        if (_isPaused)
        {
            _pausedStepBuffer++;
            return;
        }

        FireTick();
    }

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown when a reentrant tick is detected (<see cref="CoreErrors.ReentrantTick"/>).</exception>
    public void Tick(float deltaTime)
    {
        AssertMainThread();
        if (Mode != TickMode.FixedTick)
        {
            CoreLog.Warn("Tick() called in StepBased mode. Ignored.");
            return;
        }

        if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
        {
            if (CoreLog.Handler is not null)
                CoreLog.Warn($"Tick() received invalid deltaTime ({deltaTime}). Ignored.");
            return;
        }

        if (deltaTime < 0f)
        {
            if (CoreLog.Handler is not null)
                CoreLog.Warn($"Tick() received negative deltaTime ({deltaTime}). Clamped to 0.");
            deltaTime = 0f;
        }

        if (_isPaused)
        {
            _pausedTimeBuffer += deltaTime;
            return;
        }

        _accumulator += deltaTime;
        int fired = 0;

        while (_accumulator >= _fixedTimeStep && fired < MaxCatchUpTicks)
        {
            FireTick();
            _accumulator -= _fixedTimeStep;
            fired++;
        }

        if (_accumulator >= _fixedTimeStep)
        {
            if (CoreLog.Handler is not null)
                CoreLog.Warn($"Scheduler catch-up clamped. Discarding {_accumulator / _fixedTimeStep:F1} excess ticks.");
            _accumulator = 0f;
        }
    }

    /// <inheritdoc />
    public void SetPaused(bool paused)
    {
        _isPaused = paused;
    }

    /// <inheritdoc />
    /// <returns>The number of ticks actually fired.</returns>
    public int DrainPausedBuffer()
    {
        int fired = 0;

        if (Mode == TickMode.StepBased)
        {
            int toReplay = Math.Min(_pausedStepBuffer, MaxCatchUpTicks);
            for (int i = 0; i < toReplay; i++)
            {
                FireTick();
                fired++;
            }
            _pausedStepBuffer = 0;
        }
        else
        {
            _accumulator += _pausedTimeBuffer;
            _pausedTimeBuffer = 0f;

            while (_accumulator >= _fixedTimeStep && fired < MaxCatchUpTicks)
            {
                FireTick();
                _accumulator -= _fixedTimeStep;
                fired++;
            }

            if (_accumulator >= _fixedTimeStep)
            {
                if (CoreLog.Handler is not null)
                    CoreLog.Warn($"Scheduler resume catch-up clamped. Discarding excess time.");
                _accumulator = 0f;
            }
        }

        return fired;
    }

    private void FireTick()
    {
        if (_isTicking)
        {
            throw new FlosException(CoreErrors.ReentrantTick, "Reentrant tick detected. Cannot call Step()/Tick() from within a TickMessage handler.");
        }

        _isTicking = true;
        try
        {
            _dispatcher.DrainAll();

            CurrentTick++;
            float dt = Mode == TickMode.FixedTick ? _fixedTimeStep : 0f;
            ElapsedTime += dt;

            _bus.Publish(new TickMessage(CurrentTick, dt));
        }
        finally
        {
            _isTicking = false;
        }
    }
}
