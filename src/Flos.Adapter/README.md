# Flos.Adapter

Shared adapter contracts for engine-agnostic asset loading, save storage, and input handling. Engine-specific adapters (Unity, Godot, Console) implement these interfaces.

## Installation

```xml
<PackageReference Include="Flos.Adapter" />
```

## API Overview

| Type | Description |
|------|-------------|
| `IAssetProvider` | Async-via-callback asset loading (`Load<T>`, `Release`). Completion callbacks dispatched to main thread via `IDispatcher`. |
| `ISaveStorage` | Async-via-callback persistence (`Save`, `Load`, `Delete`, `Exists`). Completion callbacks dispatched to main thread via `IDispatcher`. |
| `IInputProvider` | Poll-based input: `Drain(IInputSink)` called once per tick to push buffered input into the message bus |
| `IInputSink` | Zero-allocation callback sink for `IInputProvider.Drain`. `Push<T>(T message)` publishes messages. |
| `MessageBusInputSink` | Default `IInputSink` that publishes directly to `IMessageBus` |
| `AdapterErrors` | Error codes (category 400): `AssetNotFound`, `AssetLoadFailed`, `SaveFailed`, `LoadFailed`, `DeleteFailed`, `SlotNotFound` |

## Design

All async operations use callbacks instead of `async/await` to maintain determinism in game logic. Completion callbacks are dispatched to the main thread via `IDispatcher.Enqueue()`, ensuring thread safety without exposing async patterns to handlers or systems.

## Notes

- Engine adapters implement these contracts with platform-native APIs (e.g., Unity Addressables, Godot ResourceLoader).
- `IInputProvider` is polled at the start of each tick by the adapter module.
- `MessageBusInputSink` is the standard sink — custom sinks can filter or transform input before publishing.
