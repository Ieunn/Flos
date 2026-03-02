using Flos.Core.Messaging;

namespace Flos.Adapter;

/// <summary>
/// Zero-allocation callback sink for <see cref="IInputProvider.Drain"/>.
/// Implementations typically publish directly to <see cref="IMessageBus"/>.
/// </summary>
public interface IInputSink
{
    void Push<T>(T message) where T : IMessage;
}
