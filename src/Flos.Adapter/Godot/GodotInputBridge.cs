namespace Flos.Adapter.Godot;

/// <summary>
/// Abstract base class bridging Godot Input to <see cref="IInputProvider"/>.
/// Subclasses override <see cref="ReadInput"/> to map Godot InputActions/InputEvents to Flos messages.
/// This is game-specific — the adapter provides scaffolding only.
/// </summary>
public abstract class GodotInputBridge : IInputProvider
{
    public void Drain(IInputSink sink)
    {
        ReadInput(sink);
    }

    /// <summary>
    /// Override to read Godot input and push messages into <paramref name="sink"/>.
    /// Called on the main thread once per tick.
    /// </summary>
    protected abstract void ReadInput(IInputSink sink);
}
