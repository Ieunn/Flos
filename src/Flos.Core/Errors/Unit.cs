namespace Flos.Core.Errors;

/// <summary>
/// Represents the absence of a meaningful value, used as the success type in <see cref="Result{T}"/>
/// when an operation succeeds but produces no data (e.g., <c>Result&lt;Unit&gt;</c>).
/// </summary>
public readonly record struct Unit
{
    /// <summary>
    /// Gets the singleton <see cref="Unit"/> value.
    /// </summary>
    public static readonly Unit Value = default;
}
