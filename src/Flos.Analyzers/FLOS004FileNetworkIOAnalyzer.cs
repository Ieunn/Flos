using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// Roslyn analyzer that reports file and network I/O usage in handlers. Diagnostic FLOS004.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS004FileNetworkIOAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS004,
        title: "Do not use file/network I/O in handlers",
        messageFormat: "Do not use '{0}' in command handlers, event appliers, or [HotPath] code",
        category: "Determinism",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly ImmutableHashSet<string> FlaggedTypes = ImmutableHashSet.Create(
        TypeNames.File,
        TypeNames.Directory,
        TypeNames.Socket,
        TypeNames.HttpClient,
        TypeNames.WebRequest);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
        var symbol = symbolInfo.Symbol;
        if (symbol is null) return;

        var containingType = symbol.ContainingType?.ToDisplayString();
        if (containingType is null) return;

        if (IsFlaggedType(containingType) && ScopeHelper.IsInScopedContext(memberAccess, context.SemanticModel))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.GetLocation(), containingType));
        }
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);
        var typeName = typeInfo.Type?.ToDisplayString();
        if (typeName is null) return;

        if (IsFlaggedType(typeName) && ScopeHelper.IsInScopedContext(creation, context.SemanticModel))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation(), typeName));
        }
    }

    private static bool IsFlaggedType(string typeName)
    {
        return FlaggedTypes.Contains(typeName);
    }
}
