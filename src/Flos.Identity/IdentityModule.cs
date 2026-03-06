using Flos.Core.Module;

namespace Flos.Identity;

/// <summary>
/// Module that registers an <see cref="IIdGenerator"/> backed by <see cref="SequentialIdGenerator"/>.
/// The generator starts from the specified start value (default 1), producing a deterministic
/// monotonically increasing sequence with no dependency on randomness.
/// </summary>
public sealed class IdentityModule : ModuleBase
{
    private readonly long _startValue;

    /// <summary>Creates an IdentityModule that starts generating IDs from 1.</summary>
    public IdentityModule() => _startValue = 1;

    /// <summary>Creates an IdentityModule with a custom starting value for the ID sequence.</summary>
    /// <param name="startValue">The starting value for the sequential ID generator. Values &lt;= 0 default to 1.</param>
    public IdentityModule(long startValue) => _startValue = startValue;

    /// <inheritdoc />
    public override string Id => "Identity";

    /// <inheritdoc />
    public override IReadOnlyList<string> Dependencies => Array.Empty<string>();

    /// <inheritdoc />
    public override void OnLoad(ILoadScope scope)
    {
        base.OnLoad(scope);
        scope.Register<IIdGenerator>(new SequentialIdGenerator(_startValue));
    }
}
