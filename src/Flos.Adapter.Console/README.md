# Flos.Adapter.Console

Headless adapter for CLI testing and dedicated servers. Bridges `CoreLog` to `Console.Error`, reads stdin lines as `ConsoleInputMessage` via `IDispatcher`, and writes `ConsoleOutputMessage` to stdout.

## Installation

```xml
<PackageReference Include="Flos.Adapter.Console" />
```

## Quick Usage

```csharp
var session = new Session();
session.Initialize(new SessionConfig
{
    Modules = [new RandomModule(), new ConsoleAdapterModule(), /* game modules */],
    TickMode = TickMode.StepBased,
    RandomSeed = 42
});
session.Start();

// Game loop
while (running)
{
    session.Scheduler.Step();
}

session.Shutdown();
session.Dispose();
```

### Stdin / Stdout Messaging

The module reads lines from stdin on a background thread and publishes them as `ConsoleInputMessage` via `IDispatcher`. To write output, publish a `ConsoleOutputMessage`:

```csharp
bus.Subscribe<ConsoleInputMessage>(msg => { /* msg.Line */ });
bus.Publish(new ConsoleOutputMessage("Hello, world!"));
```

For testing or redirection, inject custom streams via the constructor:

```csharp
var module = new ConsoleAdapterModule(
    stdin: new StringReader("input"),
    stdout: myStdout,
    stderr: myStderr
);
```

## API Overview

| Type | Description |
|------|-------------|
| `ConsoleAdapterModule` | Module that bridges CoreLog to stderr, stdin to `ConsoleInputMessage`, and `ConsoleOutputMessage` to stdout |
| `ConsoleInputMessage` | `readonly record struct` published when a line is read from stdin |
| `ConsoleOutputMessage` | `readonly record struct` -- publish to write a line to stdout |

## Notes

- Bridges `CoreLog` output to `Console.Error` as `[Level] message` lines.
- The stdin reader thread is marked `IsBackground = true` and will not block process exit.
- Designed for headless server scenarios, automated testing, and CLI-based prototypes.
