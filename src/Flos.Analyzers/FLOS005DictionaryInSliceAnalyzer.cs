using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// FLOS005: Dictionary/HashSet in IStateSlice field → use IOrderedMap/IOrderedSet.
/// Scans field declarations in types implementing IStateSlice.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS005DictionaryInSliceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DictionaryRule = new(
        DiagnosticIds.FLOS005,
        title: "Do not use Dictionary in IStateSlice",
        messageFormat: "Do not use Dictionary in IStateSlice fields; use IOrderedMap for deterministic iteration",
        category: "Determinism",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor HashSetRule = new(
        DiagnosticIds.FLOS005,
        title: "Do not use HashSet in IStateSlice",
        messageFormat: "Do not use HashSet in IStateSlice fields; use IOrderedSet for deterministic iteration",
        category: "Determinism",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DictionaryRule, HashSetRule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var fieldDecl = (FieldDeclarationSyntax)context.Node;
        if (!IsInStateSliceType(fieldDecl, context.SemanticModel)) return;

        var typeInfo = context.SemanticModel.GetTypeInfo(fieldDecl.Declaration.Type, context.CancellationToken);
        CheckType(typeInfo.Type, fieldDecl.Declaration.Type.GetLocation(), context);
    }

    private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
    {
        var propDecl = (PropertyDeclarationSyntax)context.Node;
        if (!IsInStateSliceType(propDecl, context.SemanticModel)) return;

        var typeInfo = context.SemanticModel.GetTypeInfo(propDecl.Type, context.CancellationToken);
        CheckType(typeInfo.Type, propDecl.Type.GetLocation(), context);
    }

    private static void CheckType(ITypeSymbol? type, Location location, SyntaxNodeAnalysisContext context)
    {
        if (type is null) return;

        var originalDef = type is INamedTypeSymbol named ? named.OriginalDefinition : type;
        var displayName = originalDef.ToDisplayString();

        if (displayName.StartsWith(TypeNames.Dictionary + "<", System.StringComparison.Ordinal) ||
            displayName == TypeNames.Dictionary ||
            originalDef.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains("System.Collections.Generic.Dictionary"))
        {
            context.ReportDiagnostic(Diagnostic.Create(DictionaryRule, location));
            return;
        }

        if (displayName.StartsWith(TypeNames.HashSet + "<", System.StringComparison.Ordinal) ||
            displayName == TypeNames.HashSet ||
            originalDef.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Contains("System.Collections.Generic.HashSet"))
        {
            context.ReportDiagnostic(Diagnostic.Create(HashSetRule, location));
        }
    }

    private static bool IsInStateSliceType(SyntaxNode node, SemanticModel model)
    {
        var typeDecl = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDecl is null) return false;

        var typeSymbol = model.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
        if (typeSymbol is null) return false;

        return ImplementsStateSlice(typeSymbol);
    }

    private static bool ImplementsStateSlice(INamedTypeSymbol typeSymbol)
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (iface.ToDisplayString() == TypeNames.IStateSlice)
                return true;
        }
        return false;
    }
}
