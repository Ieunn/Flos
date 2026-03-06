using Flos.Core.Module;

namespace Flos.Random;

/// <summary>
/// Module that registers an <see cref="IRandom"/> implementation seeded from the provided seed value.
/// </summary>
public sealed class RandomModule : ModuleBase
{
    private readonly int _seed;

    /// <summary>Creates a RandomModule with the specified seed.</summary>
    /// <param name="seed">The seed for the deterministic random number generator.</param>
    public RandomModule(int seed) => _seed = seed;

    /// <inheritdoc />
    public override string Id => "Random";

    /// <summary>
    /// Registers a seeded <see cref="Xoshiro256StarStarRandom"/> as <see cref="IRandom"/>.
    /// </summary>
    public override void OnLoad(IServiceRegistry scope)
    {
        base.OnLoad(scope);
        var rng = new Xoshiro256StarStarRandom();
        rng.SetSeed(_seed);
        Scope.Register<IRandom>(rng);
    }
}
