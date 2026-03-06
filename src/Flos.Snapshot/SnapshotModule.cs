using Flos.Core.Module;

namespace Flos.Snapshot;

/// <summary>
/// Module that registers the <see cref="ISnapshots"/> service.
/// </summary>
public sealed class SnapshotModule : ModuleBase
{
    /// <inheritdoc />
    public override string Id => "Snapshot";

    /// <inheritdoc />
    public override IReadOnlyList<string> Dependencies => Array.Empty<string>();

    /// <inheritdoc />
    public override void OnLoad(ILoadScope scope)
    {
        base.OnLoad(scope);
        scope.Register<ISnapshots>(new Snapshots());
    }
}
