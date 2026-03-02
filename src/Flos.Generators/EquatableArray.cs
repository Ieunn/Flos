using System;
using System.Collections.Generic;

namespace Flos.Generators;

/// <summary>
/// IEquatable wrapper for arrays, required for incremental generator pipeline caching.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    public readonly T[] Items;

    public EquatableArray(T[] items)
    {
        Items = items ?? Array.Empty<T>();
    }

    public bool Equals(EquatableArray<T> other)
    {
        if (Items.Length != other.Items.Length)
            return false;
        for (int i = 0; i < Items.Length; i++)
        {
            if (!Items[i].Equals(other.Items[i]))
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < Items.Length; i++)
                hash = hash * 31 + Items[i].GetHashCode();
            return hash;
        }
    }
}
