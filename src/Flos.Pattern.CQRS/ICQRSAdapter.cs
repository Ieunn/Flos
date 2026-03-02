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
    /// <see cref="IPipeline"/>. If the pipeline also implements <see cref="IHandlerRegistry"/>,
    /// it is additionally registered as <c>IHandlerRegistry</c>.
    /// </summary>
    IPipeline CreatePipeline(IMessageBus bus, IWorld world);
}
