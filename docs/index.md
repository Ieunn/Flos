# Flos Framework Documentation

**A minimalistic, multi-paradigm, engine-agnostic game logic framework in C#.**

Flos provides a pattern-neutral microkernel (Core) with pluggable pattern packages and domain modules. Games are built by composing Core + selected Pattern(s) + Modules + game-specific logic.

## Guides

- **[Architecture Guide](Architecture.md)** — Core concepts, session lifecycle, state management, module development, pattern selection, engine integration, error handling, and performance guidelines.

## Core

| Package | README |
|---------|--------|
| Flos.Core | [Microkernel: messaging, state, sessions, scheduling, modules, errors](../src/Flos.Core/README.md) |

## Patterns

| Package | README |
|---------|--------|
| Flos.Pattern.CQRS | [CQRS + Event Sourcing pattern](../src/Flos.Pattern.CQRS/README.md) |
| Flos.Pattern.ECS | [Adapter-first ECS integration](../src/Flos.Pattern.ECS/README.md) |

## Domain Modules

| Package | README |
|---------|--------|
| Flos.Random | [Deterministic RNG (Xoshiro256**)](../src/Flos.Random/README.md) |
| Flos.Collections | [Deterministic-iteration ordered collections](../src/Flos.Collections/README.md) |
| Flos.Identity | [Shared logical entity identity](../src/Flos.Identity/README.md) |
| Flos.Snapshot | [Deep-copy state snapshots and read-only views](../src/Flos.Snapshot/README.md) |
| Flos.Serialization | [Serialization adapter contract](../src/Flos.Serialization/README.md) |
| Flos.Diagnostics | [Tracing and profiling adapter contracts](../src/Flos.Diagnostics/README.md) |

## Adapters

| Package | README |
|---------|--------|
| Flos.Adapter | [Shared adapter contracts (assets, save storage, input)](../src/Flos.Adapter/README.md) |
| Flos.Adapter.Console | [Console/headless adapter](../src/Flos.Adapter.Console/README.md) |
| Flos.Adapter.Unity | [Unity engine adapter](../src/Flos.Adapter.Unity/README.md) |
| Flos.Adapter.Godot | [Godot engine adapter](../src/Flos.Adapter.Godot/README.md) |

## Tooling

| Package | README |
|---------|--------|
| Flos.Analyzers | [Roslyn analyzers enforcing framework rules](../src/Flos.Analyzers/README.md) |
| Flos.Generators | [Source generators (DeepClone, TypeResolver, Registration)](../src/Flos.Generators/README.md) |
