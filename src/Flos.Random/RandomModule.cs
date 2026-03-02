using Flos.Core.Module;
using Flos.Core.Sessions;

namespace Flos.Random;

/// <summary>
/// Module that registers an <see cref="IRandom"/> implementation seeded from <see cref="SessionConfig.RandomSeed"/>.
/// </summary>
public sealed class RandomModule : ModuleBase
{
    /// <inheritdoc />
    public override string Id => "Random";

    /// <summary>
    /// Registers a seeded <see cref="Xoshiro256StarStarRandom"/> as <see cref="IRandom"/>.
    /// </summary>
    public override void OnLoad(IServiceScope scope)
    {
        base.OnLoad(scope);
        var config = scope.Resolve<SessionConfig>();
        var rng = new Xoshiro256StarStarRandom();
        rng.SetSeed(config.RandomSeed);
        scope.RegisterInstance<IRandom>(rng);
    }
}
