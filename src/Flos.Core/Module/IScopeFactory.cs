namespace Flos.Core.Module;

/// <summary>
/// Factory for creating the root <see cref="IServiceRegistry"/>.
/// Implement this to replace the built-in <see cref="BuiltInMinimalScope"/>
/// with an external DI container.
/// </summary>
public interface IScopeFactory
{
    /// <summary>
    /// Creates the root service registry for a session.
    /// </summary>
    /// <returns>A new service registry instance.</returns>
    IServiceRegistry CreateRootScope();
}
