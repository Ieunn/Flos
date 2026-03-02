namespace Flos.Core.Module;

/// <summary>
/// Adapter interface for external dependency injection containers.
/// Implement this to replace the built-in <see cref="BuiltInMinimalScope"/>.
/// </summary>
public interface IDIAdapter
{
    /// <summary>
    /// Creates the root service scope for a session.
    /// </summary>
    /// <returns>A new service scope instance.</returns>
    IServiceScope CreateRootScope();
}
