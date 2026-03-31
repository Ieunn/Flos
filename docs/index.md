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

## Standalone Packages

| Package | README |
|---------|--------|
| Flos.Random | [Deterministic RNG (Xoshiro256**)](../src/Flos.Random/README.md) |
| Flos.Collections | [Deterministic-iteration ordered collections](../src/Flos.Collections/README.md) |

## Adapters

| Package | README |
|---------|--------|
| Flos.Adapter | [Adapter contracts + Console/Unity/Godot implementations](../src/Flos.Adapter/README.md) |

## Tooling

| Package | README |
|---------|--------|
| Flos.Analyzers | [Roslyn analyzers enforcing framework rules](../src/Flos.Analyzers/README.md) |
| Flos.Generators | [Source generators (DeepClone, TypeResolver, Registration)](../src/Flos.Generators/README.md) |
