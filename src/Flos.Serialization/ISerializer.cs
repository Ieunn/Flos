using System.Buffers;

namespace Flos.Serialization;

/// <summary>
/// Adapter contract for AOT-safe serialization.
/// No built-in implementation — bridge adapters (e.g., MemoryPack, MessagePack)
/// provide concrete implementations.
/// </summary>
public interface ISerializer
{
    void Serialize<T>(T obj, IBufferWriter<byte> output);
    T Deserialize<T>(ReadOnlySpan<byte> data);
}
