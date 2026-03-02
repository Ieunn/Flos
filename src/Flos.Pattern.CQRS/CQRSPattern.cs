using Flos.Core.Module;

namespace Flos.Pattern.CQRS;

/// <summary>Identifies the CQRS gameplay pattern.</summary>
public static class CQRSPattern
{
    /// <summary>The pattern identifier registered in <see cref="Core.Module.IPatternRegistry"/>.</summary>
    public static readonly PatternId Id = new("CQRS");
}
