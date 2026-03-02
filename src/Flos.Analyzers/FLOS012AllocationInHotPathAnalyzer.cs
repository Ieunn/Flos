using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// FLOS012: Allocation in [HotPath] code (yield return, closure, LINQ, boxing).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS012AllocationInHotPathAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS012,
        title: "Allocation in [HotPath] code",
        messageFormat: "Allocation detected in [HotPath] code: {0}",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeYieldReturn, SyntaxKind.YieldReturnStatement);

        context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.SimpleLambdaExpression);
        context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.ParenthesizedLambdaExpression);

        context.RegisterSyntaxNodeAction(AnalyzeQueryExpression, SyntaxKind.QueryExpression);

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeYieldReturn(SyntaxNodeAnalysisContext context)
    {
        if (!ScopeHelper.IsInHotPathContext(context.Node, context.SemanticModel)) return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(),
            "yield return allocates an iterator state machine"));
    }

    private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        if (!ScopeHelper.IsInHotPathContext(context.Node, context.SemanticModel)) return;

        var lambda = (LambdaExpressionSyntax)context.Node;
        var dataFlow = context.SemanticModel.AnalyzeDataFlow(lambda.Body);

        if (dataFlow is not null && dataFlow.Succeeded && dataFlow.Captured.Length > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, lambda.GetLocation(),
                "closure captures variables, allocating a display class"));
        }
    }

    private static void AnalyzeQueryExpression(SyntaxNodeAnalysisContext context)
    {
        if (!ScopeHelper.IsInHotPathContext(context.Node, context.SemanticModel)) return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(),
            "LINQ query expression allocates iterators"));
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (!ScopeHelper.IsInHotPathContext(context.Node, context.SemanticModel)) return;

        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        var method = symbolInfo.Symbol as IMethodSymbol;
        if (method is null) return;

        var containingNs = method.ContainingType?.ContainingNamespace?.ToDisplayString() ?? "";
        var containingType = method.ContainingType?.Name ?? "";

        if (containingNs == "System.Linq" && containingType == "Enumerable")
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(),
                $"LINQ method '{method.Name}' allocates iterators"));
        }
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (!ScopeHelper.IsInHotPathContext(context.Node, context.SemanticModel)) return;

        var creation = (ObjectCreationExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);
        if (typeInfo.Type is null) return;

        if (typeInfo.Type.IsReferenceType)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation(),
                $"'new {typeInfo.Type.Name}()' allocates on the heap"));
        }
    }
}
