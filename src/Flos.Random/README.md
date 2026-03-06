# Flos.Random

Deterministic random number generation using Xoshiro256** with full state capture/restore for snapshots and replay.

## Installation

```xml
<PackageReference Include="Flos.Random" />
```

## Quick Usage

Add `RandomModule` to your session with an explicit seed:

```csharp
session.Initialize(new SessionConfig
{
    Modules = [new RandomModule(42), /* other modules */],
    TickMode = TickMode.StepBased,
});
```

Then resolve and use `IRandom` in any module:

```csharp
var rng = Scope.Resolve<IRandom>();
int roll = rng.Next(1, 7); // [1, 6]
float chance = rng.NextFloat(); // [0, 1)
```

## API Overview

| Type | Description |
|------|-------------|
| `IRandom` | Deterministic RNG contract: `Next`, `NextFloat`, `SetSeed`, state capture/restore |
| `Xoshiro256StarStarRandom` | Default implementation: 32-byte state (4 × `ulong`), SplitMix64 seed derivation |
| `RandomModule` | Registers `IRandom` from a constructor-provided `int seed` |

## Determinism Notes

- All game logic must use `IRandom` instead of `System.Random` (the `FLOS001` analyzer enforces this at compile time). Set `<FlosEnforceDeterminism>true</FlosEnforceDeterminism>` in your project to upgrade FLOS001 from Warning to Error.
- `GetFullState`/`RestoreFullState` enable snapshot/restore of RNG state for deterministic replay. The full state is 32 bytes (4 × `ulong`).
- Same seed always produces the same sequence.
- The seed is a required constructor parameter: `new RandomModule(seed)`. This keeps seed ownership explicit and avoids hidden coupling to Core configuration.
- Integer range methods (`Next(min, max)`) use bias-free rejection sampling — the distribution is uniform across the requested range.
