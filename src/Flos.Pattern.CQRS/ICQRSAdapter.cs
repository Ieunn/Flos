using Flos.Core.Messaging;
using Flos.Core.State;

namespace Flos.Pattern.CQRS;

/// <summary>
/// Adapter interface for CQRS pipeline implementations.
/// The built-in implementation and third-party implementations both go through this interface.
/// <see cref="CQRSPatternModule"/> resolves <c>ICQRSAdapter</c> during <c>OnLoad</c>
/// and delegates pipeline creation to it.
/// </summary>
public interface ICQRSAdapter
{
    /// <summary>
    /// Creates the CQRS pipeline. The module registers the returned pipeline as
    /// <see cref="IPipeline"/> and the returned registry as <see cref="IHandlerRegistry"/>.
    /// </summary>
    /// <param name="bus">The session's message bus.</param>
    /// <param name="world">The session's world state.</param>
    /// <returns>A tuple of the pipeline and handler registry. Both may be the same object.</returns>
    (IPipeline Pipeline, IHandlerRegistry Registry) CreatePipeline(IMessageBus bus, IWorld world);
}
