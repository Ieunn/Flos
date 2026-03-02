# Flos.Random

Deterministic random number generation using Xoshiro256** with full state capture/restore for snapshots and replay.

## Installation

```xml
<PackageReference Include="Flos.Random" />
```

## Quick Usage

```csharp
// In a module's OnInitialize, resolve the RNG:
var rng = Scope.Resolve<IRandom>();
int roll = rng.Next(1, 7); // [1, 6]
float chance = rng.NextFloat(); // [0, 1)
```

Register `RandomModule` in your session config and set `RandomSeed` for deterministic sequences.

## API Overview

| Type | Description |
|------|-------------|
| `IRandom` | Deterministic RNG contract (Next, NextFloat, SetSeed, state capture/restore) |
| `Xoshiro256StarStarRandom` | Default implementation: 32 bytes state, SplitMix64 seed derivation |
| `RandomModule` | Registers `IRandom` seeded from `SessionConfig.RandomSeed` |

## Determinism Notes

- All game logic must use `IRandom` instead of `System.Random` (the Flos analyzers enforce this at compile time).
- `GetFullState`/`RestoreFullState` enable snapshot/restore of RNG state for deterministic replay.
- Same seed always produces the same sequence.
