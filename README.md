# Flos

**A minimalistic, multi-paradigm, engine-agnostic game logic framework in C#.**

Flos provides a pattern-neutral microkernel (Core) with pluggable pattern packages and domain modules. Games are built by composing Core + selected Pattern(s) + Modules + game-specific logic. Target: .NET 10+ / C# 14+.

## Features

- **Pattern-neutral Core** — messaging, state management, scheduling, module loading, error handling. No game-type or pattern bias.
- **Pluggable patterns** — CQRS/Event Sourcing, ECS integration, or define your own. Patterns are optional, structurally equal packages.
- **Deterministic execution** — same inputs + same seed = identical state. Enforced by compile-time analyzers.
- **Engine-agnostic** — adapters for Unity, Godot, and Console/headless. Core has zero engine dependencies.
- **Zero-allocation hot paths** — designed for zero steady-state allocation per tick.
- **AOT-safe** — source generators for registration, deep clone, and type resolution. No runtime reflection on hot paths.

## Package Map

| Package | Description |
|---------|-------------|
| [`Flos.Core`](src/Flos.Core/README.md) | Microkernel: messaging, state, sessions, scheduling, modules, errors |
| [`Flos.Random`](src/Flos.Random/README.md) | Deterministic RNG (Xoshiro256**) |
| [`Flos.Collections`](src/Flos.Collections/README.md) | Deterministic-iteration collections (`IOrderedMap`, `SortedArrayMap`) |
| [`Flos.Identity`](src/Flos.Identity/README.md) | Shared logical entity identity (`EntityId`, `IIdGenerator`) |
| [`Flos.Snapshot`](src/Flos.Snapshot/README.md) | Deep-copy state snapshots and read-only views |
| [`Flos.Serialization`](src/Flos.Serialization/README.md) | Serialization adapter contract (no built-in implementation) |
| [`Flos.Diagnostics`](src/Flos.Diagnostics/README.md) | Tracing and profiling adapter contracts |
| [`Flos.Pattern.CQRS`](src/Flos.Pattern.CQRS/README.md) | CQRS + Event Sourcing pattern |
| [`Flos.Pattern.ECS`](src/Flos.Pattern.ECS/README.md) | Adapter-first ECS integration |
| [`Flos.Testing`](src/Flos.Testing/README.md) | Test harness, replay verification, fake RNG |
| [`Flos.Analyzers`](src/Flos.Analyzers/README.md) | Roslyn analyzers enforcing framework rules |
| [`Flos.Generators`](src/Flos.Generators/README.md) | Source generators (DeepClone, TypeResolver, Registration) |
| [`Flos.Adapter.Console`](src/Flos.Adapter.Console/README.md) | Console/headless adapter |
| [`Flos.Adapter.Unity`](src/Flos.Adapter.Unity/README.md) | Unity engine adapter |
| [`Flos.Adapter.Godot`](src/Flos.Adapter.Godot/README.md) | Godot engine adapter |

## Quick Start

```csharp
using Flos.Core.Module;
using Flos.Core.Sessions;
using Flos.Core.Scheduling;
using Flos.Core.State;
using Flos.Random;

// Define state
public class GameState : IStateSlice
{
    public int Score { get; set; }
}

// Define module
public class GameModule : ModuleBase
{
    public override string Id => "Game";
    public override IReadOnlyList<string> Dependencies => ["Random"];

    public override void OnLoad(IServiceScope scope)
    {
        base.OnLoad(scope);
        scope.Resolve<IWorld>().Register(new GameState());
    }
}

// Create session
var session = new Session();
session.Initialize(new SessionConfig
{
    Modules = [new RandomModule(), new GameModule()],
    TickMode = TickMode.StepBased,
    RandomSeed = 42
});
session.Start();

// Use it
var state = session.World.Get<GameState>();
state.Score += 100;
session.Scheduler.Step();

session.Shutdown();
session.Dispose();
```

## Documentation

- **[Architecture Guide](docs/Architecture.md)** — Core concepts, patterns, module development, engine integration, error handling, performance guidelines
- **[Per-Package READMEs](#package-map)** — Usage, API overview, and determinism notes for each package

## License

This project is licensed under the [MIT License](LICENSE).
