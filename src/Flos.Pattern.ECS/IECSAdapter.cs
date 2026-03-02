using Flos.Core.State;

namespace Flos.Pattern.ECS;

/// <summary>
/// Adapter interface for integrating an external ECS framework with Flos.
/// Implementations create and own the ECS world, hook into session lifecycle,
/// and delegate tick processing to the ECS framework's internal system scheduler.
/// Concrete ECS API access (entity creation, component access, queries, system
/// registration, parallel scheduling) goes through the adapter's native API.
/// </summary>
public interface IECSAdapter
{
    /// <summary>
    /// Creates the ECS world and registers it as an IStateSlice with the given IWorld.
    /// The adapter calls world.Register with its concrete state slice type.
    /// Called during module OnLoad.
    /// </summary>
    void CreateWorld(IWorld world);

    /// <summary>
    /// Drives the adapter's internal system scheduler for one tick.
    /// Called from the ECS module's TickMessage handler.
    /// </summary>
    void Tick(float deltaTime);

    /// <summary>
    /// Shuts down the ECS world and releases resources.
    /// Called during module OnShutdown.
    /// </summary>
    void Shutdown();
}
