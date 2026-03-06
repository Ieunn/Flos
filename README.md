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
| [`Flos.Collections`](src/Flos.Collections/README.md) | Deterministic-iteration collections (`IOrderedMap`, `IOrderedSet`) |
| [`Flos.Identity`](src/Flos.Identity/README.md) | Shared logical entity identity (`EntityId`, `IIdGenerator`) |
| [`Flos.Snapshot`](src/Flos.Snapshot/README.md) | Deep-copy state snapshots and read-only views |
| [`Flos.Serialization`](src/Flos.Serialization/README.md) | Serialization adapter contract (no built-in implementation) |
| [`Flos.Diagnostics`](src/Flos.Diagnostics/README.md) | Tracing and profiling adapter contracts |
| [`Flos.Adapter`](src/Flos.Adapter/README.md) | Shared adapter contracts (assets, save storage, input) |
| [`Flos.Pattern.CQRS`](src/Flos.Pattern.CQRS/README.md) | CQRS + Event Sourcing pattern |
| [`Flos.Pattern.ECS`](src/Flos.Pattern.ECS/README.md) | Adapter-first ECS integration |
| [`Flos.Analyzers`](src/Flos.Analyzers/README.md) | Roslyn analyzers enforcing framework rules |
| [`Flos.Generators`](src/Flos.Generators/README.md) | Source generators (DeepClone, TypeResolver, Registration) |
| [`Flos.Adapter.Console`](src/Flos.Adapter.Console/README.md) | Console/headless adapter |
| [`Flos.Adapter.Unity`](src/Flos.Adapter.Unity/README.md) | Unity engine adapter |
| [`Flos.Adapter.Godot`](src/Flos.Adapter.Godot/README.md) | Godot engine adapter |

## Dependency Graph

```
Flos.Pattern.CQRS ──┐
Flos.Pattern.ECS ───┐│
                    ││
  Game Modules ─────┤├──► Flos.Core ◄──── Flos.Adapter.Console
                    ││                ◄──── Flos.Adapter.Unity
  Flos.Snapshot ────┘│                ◄──── Flos.Adapter.Godot
  Flos.Identity ─────┘
  Flos.Random ───────┘

  Flos.Collections ─── (zero dependencies)
  Flos.Serialization ─ (depends on Core for ErrorCode only)
  Flos.Diagnostics ─── (depends on Core for CoreLog only)
  Flos.Adapter ──────── (depends on Core)

  Lateral: Flos.Analyzers, Flos.Generators (compile-time only)
```

All arrows point toward Core. Patterns, modules, and adapters never reference each other — only Core and (optionally) contract packages.

## Quick Start

```csharp
using Flos.Core.Module;
using Flos.Core.Sessions;
using Flos.Core.Scheduling;
using Flos.Core.State;
using Flos.Core.Messaging;

// 1. Define your state — any class implementing IStateSlice
public class GameState : IStateSlice
{
    public int Score { get; set; }
}

// 2. Define a module — registers state in OnLoad, subscribes to ticks in OnInitialize
public class GameModule : ModuleBase
{
    public override string Id => "Game";

    private IWorld _world = null!;

    public override void OnLoad(ILoadScope scope)
    {
        base.OnLoad(scope);
        // IWorld is pre-registered by Session — access via scope.World
        scope.World.Register(new GameState());
    }

    public override void OnInitialize()
    {
        // Scope is now locked. Cache service references here (not per-tick).
        _world = Scope.Resolve<IWorld>();
        var bus = Scope.Resolve<IMessageBus>();
        bus.Subscribe<TickMessage>(OnTick);
    }

    private void OnTick(TickMessage tick)
    {
        _world.Get<GameState>().Score += 10;
    }
}

// 3. Create and run a session
var session = new Session();
session.Initialize(new SessionConfig
{
    Modules = [new GameModule()],
    TickMode = TickMode.StepBased,
});
session.Start();

// 4. Advance the simulation
session.Scheduler.Step(); // fires one TickMessage — Score is now 10
session.Scheduler.Step(); // Score is now 20

// 5. Read state directly
var state = session.World.Get<GameState>();
Console.WriteLine($"Score: {state.Score}"); // Score: 20

// 6. Cleanup
session.Shutdown();
session.Dispose();
```

## How It Works

Flos runs a **tick loop**. Each tick, the Scheduler drains queued cross-thread actions via `IDispatcher`, then publishes a `TickMessage` on the `IMessageBus`. Modules (and patterns) subscribe to `TickMessage` to drive their per-frame logic. All game state lives in `IWorld` as typed `IStateSlice` instances. Patterns like CQRS and ECS hook into this same tick cycle — they are just modules that subscribe with specific priorities and provide higher-level abstractions (commands/events, entity queries) on top of Core.

## Choosing Your Stack

| Goal | Packages | Notes |
|------|----------|-------|
| **Prototype / jam game** | Core | Direct state mutation, no ceremony |
| **Turn-based / card / strategy** | Core + CQRS + Snapshot + Identity | Full audit trail, undo/replay, deterministic |
| **Real-time / bullet-hell / RTS** | Core + ECS + Identity | Delegate to Arch/DefaultEcs/Flecs.NET |
| **Hybrid** | Core + CQRS + ECS | CQRS for game logic, ECS for physics/particles |
| **Deterministic multiplayer** | Core + CQRS + Snapshot + Random | Lockstep via event journal replay |

Add `Flos.Random` whenever game logic needs randomness. Add `Flos.Collections` if state slices use maps or sets. Add an adapter package (Console, Unity, Godot) to bridge engine lifecycle.

## Build & Test

```bash
# Build source projects only
dotnet build Flos.slnx

# Build everything (source + tests)
dotnet build Flos.Tests.slnx

# Run all tests
dotnet test Flos.Tests.slnx

# Run a single test project
dotnet test tests/Flos.Core.Tests/Flos.Core.Tests.csproj
```

**Solution structure:**
- `Flos.slnx` — source/shipping projects only (13 projects)
- `Flos.Tests.slnx` — source + test projects (24 projects, used for development)
- Unity and Godot adapter projects are compiled by their respective engines and excluded from both solutions

## Documentation

- **[Architecture Guide](docs/Architecture.md)** — Core concepts, patterns, module development, engine integration, error handling, performance guidelines
- **[Documentation Index](docs/index.md)** — Categorized links to all documentation
- **[Per-Package READMEs](#package-map)** — Usage, API overview, and determinism notes for each package

## License

This project is licensed under the [MIT License](LICENSE).
