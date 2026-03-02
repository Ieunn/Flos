using Flos.Core.Errors;

namespace Flos.Pattern.ECS;

/// <summary>
/// Error codes for the ECS pattern. Category 200 (Pattern.ECS).
/// </summary>
public static class ECSErrors
{
    public static readonly ErrorCode NoAdapter = new(200, 1);
    public static readonly ErrorCode AdapterAlreadySet = new(200, 2);
}
