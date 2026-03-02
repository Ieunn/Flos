using Flos.Core.Messaging;

namespace Flos.Adapter;

/// <summary>
/// Poll-based input contract.
/// Called once per tick to drain buffered input into the message bus.
/// </summary>
public interface IInputProvider
{
    /// <summary>
    /// Push all buffered input messages into <paramref name="sink"/>.
    /// Called on the main thread at the start of each tick.
    /// </summary>
    void Drain(IInputSink sink);
}
