using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// FLOS006: Mutable static field access.
/// Warns on read/write access to mutable static fields (non-readonly, non-const).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS006MutableStaticAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS006,
        title: "Mutable static field access",
        messageFormat: "Avoid accessing mutable static field '{0}'; static mutable state breaks determinism",
        category: "Determinism",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
    {
        var identifier = (IdentifierNameSyntax)context.Node;

        if (identifier.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == identifier)
            return;

        CheckSymbol(context, identifier);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        CheckSymbol(context, memberAccess);
    }

    private static void CheckSymbol(SyntaxNodeAnalysisContext context, ExpressionSyntax node)
    {
        if (!ScopeHelper.IsInScopedContext(node, context.SemanticModel))
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken);
        if (symbolInfo.Symbol is not IFieldSymbol field) return;

        if (!field.IsStatic) return;
        if (field.IsReadOnly || field.IsConst) return;

        var containingNs = field.ContainingType?.ContainingNamespace?.ToDisplayString() ?? "";
        if (containingNs.StartsWith("System", System.StringComparison.Ordinal)) return;
        if (containingNs.StartsWith("Microsoft", System.StringComparison.Ordinal)) return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), field.Name));
    }
}
