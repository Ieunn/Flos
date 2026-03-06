using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// Roslyn analyzer that reports usage of <c>System.Random</c> in command handlers,
/// event appliers, and <c>[HotPath]</c>-annotated code. Diagnostic FLOS001.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS001SystemRandomAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS001,
        title: "Do not use System.Random in game logic",
        messageFormat: "Do not use System.Random in handlers or [HotPath] code; use IRandom instead",
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
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);

        if (typeInfo.Type?.ToDisplayString() == TypeNames.SystemRandom
            && ScopeHelper.IsInScopedContext(creation, context.SemanticModel))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation()));
        }
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);

        if (symbolInfo.Symbol?.ContainingType?.ToDisplayString() == TypeNames.SystemRandom)
        {
            if (memberAccess.Expression is not ObjectCreationExpressionSyntax
                && ScopeHelper.IsInScopedContext(memberAccess, context.SemanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.GetLocation()));
            }
        }
    }
}
