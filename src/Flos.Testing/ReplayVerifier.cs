using Flos.Core.Sessions;
using Flos.Core.State;
using Flos.Pattern.CQRS;
using Flos.Snapshot;

namespace Flos.Testing;

/// <summary>
/// A recorded command with the tick at which it should be executed.
/// </summary>
public readonly record struct RecordedCommand(long Tick, ICommand Command);

/// <summary>
/// Replays a recorded command sequence N times and asserts identical final state each time.
/// Can be used standalone outside GameTestHarness.
/// </summary>
public sealed class ReplayVerifier
{
    /// <summary>
    /// Replays the given command sequence <paramref name="replayCount"/> times
    /// using the provided session config, asserting identical final state on each replay.
    /// </summary>
    /// <param name="commands">Recorded commands with their tick timings.</param>
    /// <param name="config">Session configuration (must include all required modules).</param>
    /// <param name="replayCount">Number of replay iterations to run.</param>
    /// <exception cref="DeterminismException">Thrown if any replay produces different final state.</exception>
    public void Verify(IReadOnlyList<RecordedCommand> commands, SessionConfig config, int replayCount)
    {
        if (replayCount < 1)
            throw new ArgumentException("Replay count must be at least 1.", nameof(replayCount));

        var referenceSnapshot = RunReplay(commands, config);

        for (int i = 1; i < replayCount; i++)
        {
            var snapshot = RunReplay(commands, config);
            CompareSnapshots(referenceSnapshot, snapshot, i + 1);
        }
    }

    private static Dictionary<Type, IStateSlice> RunReplay(
        IReadOnlyList<RecordedCommand> commands, SessionConfig config)
    {
        using var session = new Session();
        session.Initialize(config);
        session.Start();

        var pipeline = session.RootScope.Resolve<IPipeline>();

        long currentTick = 0;

        for (int i = 0; i < commands.Count; i++)
        {
            var recorded = commands[i];

            while (currentTick < recorded.Tick)
            {
                session.Scheduler.Step();
                currentTick++;
            }

            pipeline.Send(recorded.Command);
        }

        return CaptureWorldState(session.World);
    }

    internal static Dictionary<Type, IStateSlice> CaptureWorldState(IWorld world)
    {
        var result = new Dictionary<Type, IStateSlice>();
        foreach (var type in world.RegisteredTypes)
        {
            var slice = world.GetSlice(type);
            if (slice is IDeepCloneable<IStateSlice> cloneable)
            {
                result[type] = cloneable.DeepClone();
            }
            else
            {
                result[type] = slice;
            }
        }
        return result;
    }

    internal static void CompareSnapshots(
        Dictionary<Type, IStateSlice> reference,
        Dictionary<Type, IStateSlice> other,
        int replayNumber)
    {
        if (reference.Count != other.Count)
        {
            throw new DeterminismException(
                $"Replay #{replayNumber}: Different number of state slices. " +
                $"Expected {reference.Count}, got {other.Count}.");
        }

        foreach (var (type, refSlice) in reference)
        {
            if (!other.TryGetValue(type, out var otherSlice))
            {
                throw new DeterminismException(
                    $"Replay #{replayNumber}: Missing state slice '{type.Name}'.");
            }

            if (!SlicesEqual(refSlice, otherSlice, type))
            {
                throw new DeterminismException(
                    $"Replay #{replayNumber}: State slice '{type.Name}' differs. " +
                    $"Reference: {refSlice}, Actual: {otherSlice}.");
            }
        }
    }

    private static bool SlicesEqual(IStateSlice a, IStateSlice b, Type type)
    {
        if (Equals(a, b))
            return true;

        return string.Equals(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }
}
