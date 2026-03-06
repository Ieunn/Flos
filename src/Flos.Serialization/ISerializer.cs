using System.Buffers;

namespace Flos.Serialization;

/// <summary>
/// Adapter contract for AOT-safe serialization.
/// No built-in implementation — bridge adapters (e.g., MemoryPack, MessagePack)
/// provide concrete implementations.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Serializes <paramref name="obj"/> into the given buffer writer.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="output">The buffer writer to write the serialized bytes to.</param>
    void Serialize<T>(T obj, IBufferWriter<byte> output);

    /// <summary>
    /// Deserializes an object of type <typeparamref name="T"/> from the given byte span.
    /// Throws if the data is invalid or cannot be deserialized.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="data">The raw byte data to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    T Deserialize<T>(ReadOnlySpan<byte> data);
}
