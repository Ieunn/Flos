# Flos.Serialization

Adapter contract for AOT-safe serialization. No built-in implementation — use bridge packages for MemoryPack, MessagePack, etc.

## Installation

```xml
<PackageReference Include="Flos.Serialization" />
```

## Quick Usage

```csharp
// Implement ISerializer with your preferred library:
public class MemoryPackSerializer : ISerializer
{
    public void Serialize<T>(T obj, IBufferWriter<byte> output) { /* ... */ }
    public T Deserialize<T>(ReadOnlySpan<byte> data) { /* ... */ }
}

// Register in a module's OnLoad:
scope.RegisterInstance<ISerializer>(new MemoryPackSerializer());
```

## API Overview

| Type | Description |
|------|-------------|
| `ISerializer` | Serialize/deserialize contract using `IBufferWriter<byte>` and `ReadOnlySpan<byte>` |

## Notes

- No built-in implementation — serialization libraries vary widely and Flos does not prescribe one.
- Use `Flos.Generators` to produce a `FlosTypeResolver` for deterministic polymorphic type IDs.
- AOT-safe by design: no `Activator.CreateInstance` or runtime reflection in the contract.
