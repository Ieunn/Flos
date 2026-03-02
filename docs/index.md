# Flos Framework

**A minimalistic, multi-paradigm, engine-agnostic game logic framework in C#.**

Flos provides a pattern-neutral microkernel (Core) with pluggable pattern packages and domain modules. Games are built by composing Core + selected Pattern(s) + Modules + game-specific logic.

## Getting Started

See the [Architecture Guide](Architecture.md) for a walkthrough of core concepts, pattern selection, module development, and engine integration.

## API Reference

Browse the [API Reference](api/) for detailed documentation of all public types and members.

## Building the Docs

```bash
# Install docfx (if not already installed)
dotnet tool install -g docfx

# Build documentation
docfx build docs/docfx.json

# Serve locally
docfx serve docs/_site
```
