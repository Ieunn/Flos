using Flos.Core.Errors;

namespace Flos.Adapter;

/// <summary>
/// Error codes for adapter bridge operations (category 400).
/// </summary>
public static class AdapterErrors
{
    /// <summary>Asset not found.</summary>
    public static readonly ErrorCode AssetNotFound = new(400, 1);

    /// <summary>Asset loading failed.</summary>
    public static readonly ErrorCode AssetLoadFailed = new(400, 2);

    /// <summary>Save operation failed.</summary>
    public static readonly ErrorCode SaveFailed = new(400, 10);

    /// <summary>Load operation failed (slot missing or corrupt).</summary>
    public static readonly ErrorCode LoadFailed = new(400, 11);

    /// <summary>Delete operation failed.</summary>
    public static readonly ErrorCode DeleteFailed = new(400, 12);

    /// <summary>Save slot not found.</summary>
    public static readonly ErrorCode SlotNotFound = new(400, 13);
}
