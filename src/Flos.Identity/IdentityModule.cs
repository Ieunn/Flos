using Flos.Core.Module;
using Flos.Core.Sessions;

namespace Flos.Identity;

/// <summary>
/// Module that registers an <see cref="IIdGenerator"/> backed by <see cref="SequentialIdGenerator"/>.
/// </summary>
public sealed class IdentityModule : ModuleBase
{
    /// <inheritdoc />
    public override string Id => "Identity";

    /// <inheritdoc />
    public override IReadOnlyList<string> Dependencies => [];

    /// <inheritdoc />
    public override void OnLoad(IServiceScope scope)
    {
        base.OnLoad(scope);
        var config = scope.Resolve<SessionConfig>();
        long startValue = SplitMix64(unchecked((ulong)config.RandomSeed));
        var generator = new SequentialIdGenerator(startValue);
        scope.RegisterInstance<IIdGenerator>(generator);
    }

    /// <summary>
    /// SplitMix64-style hash to derive a well-distributed long from a seed.
    /// </summary>
    private static long SplitMix64(ulong seed)
    {
        unchecked
        {
            seed += 0x9E3779B97F4A7C15UL;
            seed = (seed ^ (seed >> 30)) * 0xBF58476D1CE4E5B9UL;
            seed = (seed ^ (seed >> 27)) * 0x94D049BB133111EBUL;
            seed ^= seed >> 31;
        }
        long result = (long)(seed & 0x7FFFFFFFFFFFFFFFUL);
        return result <= 0 ? 1 : result;
    }
}
