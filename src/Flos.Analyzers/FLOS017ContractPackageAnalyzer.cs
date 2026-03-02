using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// FLOS017: Implementation types in Contract package assembly.
/// Scans for non-interface, non-abstract, non-record-struct types that don't match
/// the allowed patterns (message types, IStateSlice defs, read-only interfaces, ErrorCode constants).
/// Triggered by assembly name ending in ".Contracts".
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS017ContractPackageAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS017,
        title: "Implementation in contract package",
        messageFormat: "Type '{0}' in contract package contains implementation; contracts should only contain message types, IStateSlice definitions, read-only interfaces, and ErrorCode constants",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var assemblyName = context.SemanticModel.Compilation.AssemblyName ?? "";
        if (!assemblyName.EndsWith(".Contracts", System.StringComparison.OrdinalIgnoreCase) &&
            !assemblyName.EndsWith(".Contract", System.StringComparison.OrdinalIgnoreCase))
            return;

        var classDecl = (ClassDeclarationSyntax)context.Node;
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (typeSymbol is null) return;

        if (typeSymbol.TypeKind == TypeKind.Interface) return;

        if (typeSymbol.IsAbstract) return;

        if (typeSymbol.IsStatic) return;

        if (ImplementsInterface(typeSymbol, TypeNames.IStateSlice)) return;

        if (ImplementsInterface(typeSymbol, TypeNames.ICommand) ||
            ImplementsInterface(typeSymbol, TypeNames.IEvent) ||
            ImplementsInterface(typeSymbol, "Flos.Core.Messaging.IMessage"))
            return;

        if (InheritsFrom(typeSymbol, "System.Attribute"))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, classDecl.Identifier.GetLocation(), typeSymbol.Name));
    }

    private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, string interfaceFullName)
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (iface.ToDisplayString() == interfaceFullName)
                return true;
        }
        return false;
    }

    private static bool InheritsFrom(INamedTypeSymbol typeSymbol, string baseFullName)
    {
        var current = typeSymbol.BaseType;
        while (current is not null)
        {
            if (current.ToDisplayString() == baseFullName)
                return true;
            current = current.BaseType;
        }
        return false;
    }
}
