using Flos.Core.Messaging;

namespace Flos.Pattern.CQRS;

/// <summary>Marker interface for domain events produced by command handlers.</summary>
public interface IEvent : IMessage { }
