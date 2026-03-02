namespace Flos.Core.Errors;

/// <summary>
/// Identifies a domain error by category and code, formatted as <c>FLOS-{Category}-{Code}</c>.
/// </summary>
/// <param name="Category">The error category (e.g., 0 for Core, 100+ for Patterns, 300+ for Modules, 900-999 for game-specific).</param>
/// <param name="Code">The specific error code within the category.</param>
public readonly record struct ErrorCode(int Category, int Code)
{
    /// <inheritdoc />
    public override string ToString() => $"FLOS-{Category:D3}-{Code:D4}";
}
