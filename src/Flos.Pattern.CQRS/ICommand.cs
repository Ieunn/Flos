using Flos.Core.Messaging;

namespace Flos.Pattern.CQRS;

/// <summary>Marker interface for commands processed by the CQRS <see cref="IPipeline"/>.</summary>
public interface ICommand : IMessage;

