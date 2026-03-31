# Flos

**A minimalistic, multi-paradigm, engine-agnostic game logic framework in C#.**

Flos provides a pattern-neutral microkernel (Core) with pluggable pattern packages and domain modules. Games are built by composing Core + selected Pattern(s) + Modules + game-specific logic. Target: .NET 10+ / C# 14+.

## Features

- **Pattern-neutral Core** — messaging, state management, scheduling, module loading, error handling. No game-type or pattern bias.
- **Pluggable patterns** — CQRS/Event Sourcing, ECS integration, or define your own. Patterns are optional, structurally equal packages.
- **Engine-agnostic** — adapters for Unity, Godot, and Console/headless. Core has zero engine dependencies.
- **Zero-allocation hot paths** — designed for zero steady-state allocation per tick.
- **AOT-safe** — source generators for registration, deep clone, and type resolution. No runtime reflection on hot paths.

## Package Map

| Package | Description |
|---------|-------------|
| [`Flos.Core`](src/Flos.Core/README.md) | Microkernel: messaging, state, sessions, scheduling, modules, errors |
| [`Flos.Adapter`](src/Flos.Adapter/README.md) | Adapter contracts (assets, save storage, input, profiling, tracing) + Console/Unity/Godot implementations |
| [`Flos.Pattern.CQRS`](src/Flos.Pattern.CQRS/README.md) | CQRS + Event Sourcing pattern, includes snapshot/rollback support |
| [`Flos.Pattern.ECS`](src/Flos.Pattern.ECS/README.md) | Adapter-first ECS integration |
| [`Flos.Analyzers`](src/Flos.Analyzers/README.md) | Roslyn analyzers enforcing framework rules |
| [`Flos.Generators`](src/Flos.Generators/README.md) | Source generators (DeepClone, TypeResolver, Registration) |
| [`Flos.Collections`](src/Flos.Collections/README.md) | Deterministic-iteration collections (`IOrderedMap`, `IOrderedSet`) |
| [`Flos.Random`](src/Flos.Random/README.md) | Deterministic RNG (Xoshiro256**) |

## Dependency Graph

```
  Flos.Pattern ─────┐│
                    ││
  Game Modules ─────┤├──► Flos.Core ◄──── Flos.Adapter
                    ││
  Flos.Random ──────┘│
                     │
  Flos.Collections ───

  Lateral: Flos.Analyzers, Flos.Generators (compile-time only)
```

All arrows point toward Core. Patterns, modules, and adapters never reference each other — only Core and (optionally) utility packages.

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

    public override void OnLoad(IServiceRegistry scope)
    {
        base.OnLoad(scope);
        // IWorld is pre-registered by Session — resolve it to register state
        Scope.Resolve<IWorld>().Register(new GameState());
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
| **Turn-based / card / strategy** | Core + CQRS | Full audit trail, undo/replay, snapshots included |
| **Real-time / bullet-hell / RTS** | Core + ECS | Delegate to Arch/DefaultEcs/Flecs.NET |
| **Hybrid** | Core + CQRS + ECS | CQRS for game logic, ECS for physics/particles |
| **Deterministic multiplayer** | Core + CQRS + Random | Lockstep via event journal replay |

Add `Flos.Random` whenever game logic needs deterministic randomness. Add `Flos.Collections` if state slices use maps or sets. The `Flos.Adapter` package includes Console, Unity, and Godot implementations to bridge engine lifecycle.

## Build

```bash
# Build source projects only
dotnet build Flos.slnx
```

**Solution structure:**
- `Flos.slnx` — source/shipping projects only (8 packages)
- Unity and Godot adapter code lives in `src/Flos.Adapter/` subdirectories and is compiled by their respective engines

## Documentation

- **[Architecture Guide](docs/Architecture.md)** — Core concepts, patterns, module development, engine integration, error handling, performance guidelines
- **[Documentation Index](docs/index.md)** — Categorized links to all documentation
- **[Per-Package READMEs](#package-map)** — Usage, API overview, and determinism notes for each package

## License

This project is licensed under the [MIT License](LICENSE).
