# Flos.Testing

Test harness and replay verification for Flos game logic. Provides a fluent API for setting up sessions, executing commands, and asserting state.

## Installation

```xml
<PackageReference Include="Flos.Testing" />
```

## Quick Usage

```csharp
new GameTestHarness()
    .WithModules(new RandomModule(), new SnapshotModule(),
                 new CQRSPatternModule(), new CardModule())
    .WithSeed(42)
    .Build()
    .Execute(new DrawCardCommand(CommandSource.System, null, 3))
    .Tick()
    .AssertEventEmitted<CardDrawnEvent>(e => e.CardId == "Ace")
    .AssertState<DeckState>(s => s.Cards.Count == 47, "Deck should have 47 cards")
    .AssertDeterministic(5); // replay 5 times, verify identical state
```

## API Overview

| Type | Description |
|------|-------------|
| `GameTestHarness` | Fluent test builder: `WithModules`, `WithSeed`, `Build`, `Execute`, `Tick`, `Assert*`. Call `Build()` between configuration and usage. Module instances are reused across replays. |
| `ReplayVerifier` | Replays a command sequence N times and asserts identical final state using `Equals` + `ToString()` fallback |
| `FakeRandom` | Controllable `IRandom` implementation for tests |
| `EventCaptureModule` | Module that records all published events for assertion |
| `DeterminismException` | Thrown when replay produces non-identical state |
| `TestAssertionException` | Thrown when a test assertion fails |

## Determinism Notes

- `AssertDeterministic(N)` replays the entire command sequence N times with the same seed and verifies that the final world state matches using `Equals` (with `ToString()` fallback for diagnostics). State slices should override `Equals` for reliable determinism verification.
- `FakeRandom` allows injecting specific random sequences for edge-case testing.
