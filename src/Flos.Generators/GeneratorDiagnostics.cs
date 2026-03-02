using Microsoft.CodeAnalysis;

namespace Flos.Generators;

/// <summary>
/// Diagnostic descriptors reported by Flos source generators.
/// </summary>
internal static class GeneratorDiagnostics
{
    public static readonly DiagnosticDescriptor NonCloneableField = new(
        "FLOSGEN001",
        "Non-cloneable reference field in IStateSlice",
        "Field '{0}' of type '{1}' in '{2}' is a reference type that does not implement IDeepCloneable<T>; it cannot be auto-cloned",
        "Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor HashCollision = new(
        "FLOSGEN002",
        "TypeResolver hash collision",
        "Hash collision detected: types '{0}' and '{1}' both hash to {2}",
        "Generation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidDeepCloneTarget = new(
        "FLOSGEN003",
        "Invalid [GenerateDeepClone] target",
        "Type '{0}' has [GenerateDeepClone] but does not implement IStateSlice",
        "Generation",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
