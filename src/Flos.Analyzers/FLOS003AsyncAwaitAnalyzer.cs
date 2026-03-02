using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// Roslyn analyzer that reports <c>async</c>/<c>await</c> usage in command handlers,
/// event appliers, and <c>[HotPath]</c> code. Diagnostic FLOS003.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS003AsyncAwaitAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS003,
        title: "Do not use async/await in handlers",
        messageFormat: "Do not use async/await in command handlers, event appliers, or [HotPath] code",
        category: "Determinism",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDecl = (MethodDeclarationSyntax)context.Node;

        if (!methodDecl.Modifiers.Any(SyntaxKind.AsyncKeyword))
            return;

        if (ScopeHelper.IsInScopedContext(methodDecl, context.SemanticModel))
        {
            var asyncToken = methodDecl.Modifiers.First(m => m.IsKind(SyntaxKind.AsyncKeyword));
            context.ReportDiagnostic(Diagnostic.Create(Rule, asyncToken.GetLocation()));
        }
    }

    private static void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
    {
        var awaitExpr = (AwaitExpressionSyntax)context.Node;

        if (ScopeHelper.IsInScopedContext(awaitExpr, context.SemanticModel))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, awaitExpr.GetLocation()));
        }
    }
}
