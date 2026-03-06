namespace Flos.Core.Module;

/// <summary>
/// Default implementation of <see cref="IPatternRegistry"/>.
/// </summary>
public sealed class PatternRegistry : IPatternRegistry
{
    private readonly HashSet<PatternId> _loaded = new HashSet<PatternId>();

    /// <inheritdoc />
    public void Register(PatternId id) => _loaded.Add(id);

    /// <inheritdoc />
    public bool IsLoaded(PatternId id) => _loaded.Contains(id);

    /// <inheritdoc />
    public IReadOnlySet<PatternId> LoadedPatterns => _loaded;
}
