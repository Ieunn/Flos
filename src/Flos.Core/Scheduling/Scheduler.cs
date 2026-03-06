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
    private readonly double _fixedTimeStep;
    private double _accumulator;
    private bool _isTicking;
    private bool _isPaused;
    private int _pausedStepBuffer;
    private double _pausedTimeBuffer;
    private readonly ThreadGuard _threadGuard = new("Scheduler");

    /// <inheritdoc />
    public TickMode Mode { get; }

    /// <inheritdoc />
    public long CurrentTick { get; private set; }

    /// <inheritdoc />
    public double ElapsedTime => Mode == TickMode.FixedTick
        ? CurrentTick * _fixedTimeStep
        : 0.0;

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
    public Scheduler(TickMode mode, double fixedTimeStep, IMessageBus bus, IDispatcher dispatcher)
    {
        if (mode == TickMode.FixedTick && fixedTimeStep <= 0.0)
            throw new FlosException(CoreErrors.InvalidConfiguration,
                "fixedTimeStep must be greater than 0 in FixedTick mode.");

        Mode = mode;
        _fixedTimeStep = fixedTimeStep;
        _bus = bus;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    /// <exception cref="FlosException">Thrown when a reentrant tick is detected (<see cref="CoreErrors.ReentrantTick"/>).</exception>
    public void Step()
    {
        _threadGuard.Assert();
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
    public void Tick(double deltaTime)
    {
        _threadGuard.Assert();
        if (Mode != TickMode.FixedTick)
        {
            CoreLog.Warn("Tick() called in StepBased mode. Ignored.");
            return;
        }

        if (double.IsNaN(deltaTime) || double.IsInfinity(deltaTime))
        {
            CoreLog.Warn($"Tick() received invalid deltaTime ({deltaTime}). Ignored.");
            return;
        }

        if (deltaTime < 0.0)
        {
            CoreLog.Warn($"Tick() received negative deltaTime ({deltaTime}). Clamped to 0.");
            deltaTime = 0.0;
        }

        if (_isPaused)
        {
            _pausedTimeBuffer += deltaTime;
            return;
        }

        ConsumeTime(deltaTime);
    }

    /// <inheritdoc />
    public void SetPaused(bool paused)
    {
        _threadGuard.Assert();
        _isPaused = paused;
    }

    /// <inheritdoc />
    /// <returns>The number of ticks actually fired.</returns>
    public int DrainPausedBuffer()
    {
        _threadGuard.Assert();
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
            double buffered = _pausedTimeBuffer;
            fired = ConsumeTime(buffered);
            _pausedTimeBuffer = 0.0;
        }

        return fired;
    }

    /// <summary>
    /// Inject <paramref name="deltaTime"/> to accumulator to trigger as more ticks as possible,
    /// throw unnecessary ticks when catch up is restricted.
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <returns>Actual fired ticks.</returns>
    private int ConsumeTime(double deltaTime)
    {
        _accumulator += deltaTime;
        int fired = 0;

        while (_accumulator >= _fixedTimeStep && fired < MaxCatchUpTicks)
        {
            _accumulator -= _fixedTimeStep;
            FireTick();
            fired++;
        }

        if (_accumulator >= _fixedTimeStep)
        {
            double excessTicks = _accumulator / _fixedTimeStep;
            if (excessTicks <= long.MaxValue)
            {
                CoreLog.Warn(
                    $"Scheduler catch-up clamped. Discarding {(long)excessTicks} excess ticks.");
            }
            else
            {
                CoreLog.Warn("Scheduler catch-up clamped. Discarding a large number of excess ticks.");
            }

            _accumulator %= _fixedTimeStep;
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
            double dt = Mode == TickMode.FixedTick ? _fixedTimeStep : 0.0;

            _bus.Publish(new TickMessage(CurrentTick, dt));
        }
        finally
        {
            _isTicking = false;
        }
    }
}
