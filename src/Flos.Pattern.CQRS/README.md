# Flos.Pattern.CQRS

CQRS + Event Sourcing pattern for discrete, auditable state transitions. Ideal for turn-based, card, strategy, and board games.

## Installation

```xml
<PackageReference Include="Flos.Pattern.CQRS" />
```

## Quick Usage

```csharp
// Define a command and event
public readonly record struct DrawCardCommand(
    CommandSource Source, EntityId? TargetEntityId, int Count) : ICommand;

public readonly record struct CardDrawnEvent(EntityId PlayerId, string CardId) : IEvent;

// Implement a handler (validates against read-only state, returns events)
public class DrawCardHandler : ICommandHandler<DrawCardCommand>
{
    public Result<IReadOnlyList<IEvent>> Handle(DrawCardCommand cmd, IStateView state)
    {
        var deck = state.Get<DeckState>();
        if (deck.Cards.Count < cmd.Count)
            return Result<IReadOnlyList<IEvent>>.Fail(GameErrors.InsufficientCards);

        var events = new List<IEvent>();
        for (int i = 0; i < cmd.Count; i++)
            events.Add(new CardDrawnEvent(cmd.TargetEntityId!.Value, deck.Cards[i]));
        return Result<IReadOnlyList<IEvent>>.Ok(events);
    }
}

// Implement an applier (mutates state)
public class CardDrawnApplier : IEventApplier<CardDrawnEvent, DeckState>
{
    public void Apply(DeckState state, CardDrawnEvent evt) => state.Cards.RemoveAt(0);
}
```

## API Overview

| Type | Description |
|------|-------------|
| `ICommand` | Command marker interface (extends `IMessage`) |
| `IEvent` | Domain event marker |
| `ICommandHandler<T>` | Validates commands against read-only state, produces events |
| `IEventApplier<TEvent, TState>` | Mutates state in response to events |
| `IPipeline` | Sends commands through the CQRS pipeline |
| `IHandlerRegistry` | Registers handlers and appliers |
| `IEventJournal` | Append-only event log indexed by tick |
| `CQRSConfig` | Runtime config: `FaultMode`, `EnableJournal` |
| `ApplierFaultMode` | `Strict` (default), `Tolerant`, `Fatal` |
| `CommandSource` | Identifies command origin (System or Player) |

## Pipeline Execution

1. Route command to handler (fail: `UnknownCommand`)
2. Capture read-only snapshot
3. Handler validates and returns events (fail: publish `CommandRejectedMessage`)
4. Apply events to world state
5. Journal events (if enabled)
6. Publish events on MessageBus

## Determinism Notes

- Handlers receive `IStateView` (read-only snapshot) — cannot accidentally mutate state.
- Applier exceptions trigger automatic state rollback (configurable via `ApplierFaultMode`).
- Event journal enables deterministic replay.
