# Flos.Collections

Deterministic-iteration collections for use in `IStateSlice` fields. Ensures identical iteration order across runs for replay and networking.

## Installation

```xml
<PackageReference Include="Flos.Collections" />
```

## Quick Usage

```csharp
using Flos.Collections;

IOrderedMap<string, int> scores = new SortedArrayMap<string, int>();
scores.Add("Alice", 10);
scores.Add("Bob", 5);

foreach (var (key, value) in scores)
{
    // Guaranteed sorted order: Alice, Bob
}
```

## API Overview

| Type | Description |
|------|-------------|
| `IOrderedMap<TKey, TValue>` | Deterministic-iteration ordered map interface |
| `SortedArrayMap<TKey, TValue>` | Array-backed sorted map: O(log n) lookup, O(n) insert, cache-friendly |

## Determinism Notes

- `Dictionary<K,V>` and `HashSet<T>` have non-deterministic iteration order. Use `IOrderedMap` in `IStateSlice` fields instead (the Flos analyzers warn on this at compile time).
- `SortedArrayMap` iterates in key-sorted order, which is deterministic across runs.
