namespace Flos.Core.Annotations;

/// <summary>
/// Marks a type or method as performance-critical.
/// The FLOS012 analyzer enforces zero-allocation constraints within annotated scopes.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class HotPathAttribute : Attribute { }
