# Flos.Generators

Source generators for compile-time code generation. Eliminates runtime reflection for AOT-safe operation.

## Installation

```xml
<PackageReference Include="Flos.Generators" />
```

## Generators

### DeepClone Generator

Generates `IDeepCloneable<T>.DeepClone()` implementations for `IStateSlice` types annotated with `[GenerateDeepClone]`.

```csharp
[GenerateDeepClone]
public class GameState : IStateSlice, IDeepCloneable<GameState>
{
    public int Score { get; set; }
    public IOrderedMap<string, int> Inventory { get; set; }
    // DeepClone() is auto-generated
}
```

### TypeResolver Generator

Produces a `FlosTypeResolver` mapping deterministic integer Type IDs to concrete types for polymorphic serialization. Hash collisions are detected at compile time (FLOSGEN002).

### Registration Generator

Auto-generates handler and applier registration code, eliminating manual `registry.Register(...)` boilerplate.

## Diagnostics

| ID | Severity | Description |
|----|----------|-------------|
| FLOSGEN001 | Error | Non-cloneable reference field in `IStateSlice` |
| FLOSGEN002 | Error | TypeResolver hash collision between two types |
| FLOSGEN003 | Warning | `[GenerateDeepClone]` on type that doesn't implement `IStateSlice` |
