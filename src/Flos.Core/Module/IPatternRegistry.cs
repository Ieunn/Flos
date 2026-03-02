namespace Flos.Core.Module;

/// <summary>
/// Identifies a gameplay pattern (e.g. CQRS, ECS) by name.
/// </summary>
/// <param name="Name">The unique name of the pattern.</param>
public readonly record struct PatternId(string Name);

/// <summary>
/// Registry of loaded gameplay patterns, used during module validation.
/// </summary>
public interface IPatternRegistry
{
    /// <summary>
    /// Marks a pattern as loaded.
    /// </summary>
    /// <param name="id">The pattern identifier to register.</param>
    void Register(PatternId id);

    /// <summary>
    /// Returns <see langword="true"/> if the pattern has been registered.
    /// </summary>
    /// <param name="id">The pattern identifier to check.</param>
    /// <returns><see langword="true"/> if the pattern is loaded; otherwise, <see langword="false"/>.</returns>
    bool IsLoaded(PatternId id);

    /// <summary>
    /// All currently loaded pattern identifiers.
    /// </summary>
    IReadOnlySet<PatternId> LoadedPatterns { get; }
}
