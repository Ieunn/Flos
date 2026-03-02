using Flos.Core.Messaging;

namespace Flos.Adapter;

/// <summary>
/// Default <see cref="IInputSink"/> that publishes messages directly to an <see cref="IMessageBus"/>.
/// </summary>
public sealed class MessageBusInputSink(IMessageBus bus) : IInputSink
{
    public void Push<T>(T message) where T : IMessage
    {
        bus.Publish(message);
    }
}
