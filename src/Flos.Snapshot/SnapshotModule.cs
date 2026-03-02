using Flos.Core.Module;

namespace Flos.Snapshot;

/// <summary>
/// Module that registers the <see cref="ISnapshotManager"/> service.
/// </summary>
public sealed class SnapshotModule : ModuleBase
{
    /// <inheritdoc />
    public override string Id => "Snapshot";

    /// <inheritdoc />
    public override IReadOnlyList<string> Dependencies => [];

    /// <inheritdoc />
    public override void OnLoad(IServiceScope scope)
    {
        base.OnLoad(scope);
        scope.RegisterInstance<ISnapshotManager>(new SnapshotManager());
    }
}
