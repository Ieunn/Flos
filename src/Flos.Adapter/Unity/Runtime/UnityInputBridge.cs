using Flos.Adapter;

namespace Flos.Adapter.Unity
{
    /// <summary>
    /// Abstract base class bridging Unity Input System to <see cref="IInputProvider"/>.
    /// Subclasses override <see cref="ReadInput"/> to map Unity InputActions to Flos messages.
    /// This is game-specific — the adapter provides scaffolding only.
    /// </summary>
    public abstract class UnityInputBridge : IInputProvider
    {
        public void Drain(IInputSink sink)
        {
            ReadInput(sink);
        }

        /// <summary>
        /// Override to read Unity input and push messages into <paramref name="sink"/>.
        /// Called on the main thread once per tick.
        /// </summary>
        protected abstract void ReadInput(IInputSink sink);
    }
}
