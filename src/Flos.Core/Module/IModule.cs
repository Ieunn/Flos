namespace Flos.Core.Module;

/// <summary>
/// Defines the contract for a game module with a deterministic lifecycle.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Unique module identifier (e.g. "Random", "CQRS").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// IDs of modules that must be loaded before this one.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Patterns that must be registered before this module initializes.
    /// </summary>
    IReadOnlyList<PatternId> RequiredPatterns { get; }

    /// <summary>
    /// Called first in dependency order. Register services into the shared scope.
    /// </summary>
    /// <param name="scope">The shared service scope for the session.</param>
    void OnLoad(IServiceScope scope);

    /// <summary>
    /// Called after all modules are loaded and the scope is locked. Resolve cross-module services here.
    /// </summary>
    void OnInitialize();

    /// <summary>
    /// Called when the session transitions to Running.
    /// </summary>
    void OnStart();

    /// <summary>
    /// Called when the session is paused.
    /// </summary>
    void OnPause();

    /// <summary>
    /// Called when the session resumes from pause.
    /// </summary>
    void OnResume();

    /// <summary>
    /// Called in reverse dependency order during session shutdown.
    /// </summary>
    void OnShutdown();
}
