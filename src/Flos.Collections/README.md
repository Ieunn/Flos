# Flos.Collections

Deterministic-iteration collections for use in `IStateSlice` fields. Ensures identical iteration order across runs for replay and networking.

## Installation

```xml
<PackageReference Include="Flos.Collections" />
```

## Quick Usage

```csharp
using Flos.Collections;

// Ordered map (replaces Dictionary)
IOrderedMap<string, int> scores = new SortedArrayMap<string, int>();
scores.Add("Alice", 10);
scores.Add("Bob", 5);

foreach (var (key, value) in scores)
{
    // Guaranteed sorted order: Alice, Bob
}

// Ordered set (replaces HashSet)
IOrderedSet<string> tags = new SortedArraySet<string>();
tags.Add("fire");
tags.Add("buff");

foreach (var tag in tags)
{
    // Guaranteed sorted order: buff, fire
}
```

## API Overview

| Type | Description |
|------|-------------|
| `IOrderedMap<TKey, TValue>` | Deterministic-iteration ordered map interface |
| `SortedArrayMap<TKey, TValue>` | Array-backed sorted map: O(log n) lookup, O(n) insert, cache-friendly. Best for small-to-medium collections. |
| `OrderedMap<TKey, TValue>` | Red-black tree backed sorted map: O(log n) insert, remove, and lookup. Better for large collections where O(n) insert becomes a bottleneck. |
| `IOrderedSet<T>` | Deterministic-iteration ordered set interface |
| `SortedArraySet<T>` | Array-backed sorted set: O(log n) lookup, O(n) insert, cache-friendly. Best for small-to-medium collections. |
| `OrderedSet<T>` | Red-black tree backed sorted set: O(log n) insert, remove, and lookup. Better for large collections. |

## Choosing an Implementation

| Implementation | Insert | Remove | Lookup | Memory | Best for |
|---------------|--------|--------|--------|--------|----------|
| `SortedArrayMap` / `SortedArraySet` | O(n) | O(n) | O(log n) | Compact, ArrayPool-backed | Small-medium collections, hot-path iteration |
| `OrderedMap` / `OrderedSet` | O(log n) | O(log n) | O(log n) | Tree nodes (SortedDictionary/SortedSet) | Large collections with frequent insert/remove |

## Performance

`SortedArrayMap` and `SortedArraySet` (array-backed):
- Use `ArrayPool` for backing storage (zero GC on growth)
- Provide struct enumerators for zero-allocation foreach
- Expose `ReadOnlySpan` accessors (`KeysSpan`/`ValuesSpan`/`ItemsSpan`) for hot-path iteration
- Implement `IDisposable` to return arrays to the pool — **call `Dispose()` when removing from state** to avoid leaking pooled arrays

`OrderedMap` and `OrderedSet` (tree-backed):
- Standard `SortedDictionary`/`SortedSet` under the hood
- No special disposal needed
- No `ReadOnlySpan` accessors

## Determinism Notes

- `Dictionary<K,V>` and `HashSet<T>` have non-deterministic iteration order. Use `IOrderedMap`/`IOrderedSet` in `IStateSlice` fields instead (the Flos analyzers warn on this at compile time via FLOS005).
- Both collections iterate in sorted order, which is deterministic across runs.
