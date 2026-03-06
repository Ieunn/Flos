namespace Flos.Core.Module;

/// <summary>
/// Convenience base class for <see cref="IModule"/> that provides default no-op lifecycle methods
/// and stores the service scope.
/// </summary>
public abstract class ModuleBase : IModule
{
    /// <summary>
    /// Unique module identifier.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Module dependencies. Empty by default.
    /// </summary>
    public virtual IReadOnlyList<string> Dependencies => Array.Empty<string>();

    /// <summary>
    /// Required patterns. Empty by default.
    /// </summary>
    public virtual IReadOnlyList<PatternId> RequiredPatterns => Array.Empty<PatternId>();

    /// <summary>
    /// The shared service registry, available after <see cref="OnLoad"/>.
    /// </summary>
    protected IServiceRegistry Scope { get; private set; } = null!;

    /// <summary>
    /// Stores the service registry for use in <see cref="OnInitialize"/>.
    /// Call <c>base.OnLoad(scope)</c> when overriding.
    /// </summary>
    /// <param name="scope">The service registry for registration and resolving pre-registered services.</param>
    public virtual void OnLoad(IServiceRegistry scope)
    {
        Scope = scope;
    }

    /// <summary>
    /// Called after all modules are loaded. Override to add initialization behavior.
    /// </summary>
    public virtual void OnInitialize() { }

    /// <summary>
    /// Called when the session starts. Override to add start behavior.
    /// </summary>
    public virtual void OnStart() { }

    /// <summary>
    /// Called when the session is paused. Override to add pause behavior.
    /// </summary>
    public virtual void OnPause() { }

    /// <summary>
    /// Called when the session resumes. Override to add resume behavior.
    /// </summary>
    public virtual void OnResume() { }

    /// <summary>
    /// Called during session shutdown. Override to add cleanup behavior.
    /// </summary>
    public virtual void OnShutdown() { }
}
