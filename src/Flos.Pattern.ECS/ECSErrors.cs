using Flos.Core.Errors;

namespace Flos.Pattern.ECS;

/// <summary>
/// Error codes for the ECS pattern. Category 200 (Pattern.ECS).
/// Reserved for use by <see cref="IECSAdapter"/> implementations.
/// </summary>
public static class ECSErrors
{
    /// <summary>FLOS-200-0001. No ECS adapter was registered.</summary>
    public static readonly ErrorCode NoAdapter = new(200, 1);

    /// <summary>FLOS-200-0002. An ECS adapter is already registered and cannot be replaced.</summary>
    public static readonly ErrorCode AdapterAlreadySet = new(200, 2);
}
