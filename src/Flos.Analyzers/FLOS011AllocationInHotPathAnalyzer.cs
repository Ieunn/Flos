using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// FLOS011: Allocation in [HotPath] code (yield return, closure, LINQ, boxing, array, string interpolation).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS011AllocationInHotPathAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS011,
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
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);

        context.RegisterSyntaxNodeAction(AnalyzeArrayCreation, SyntaxKind.ArrayCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeArrayCreation, SyntaxKind.ImplicitArrayCreationExpression);

        context.RegisterSyntaxNodeAction(AnalyzeInterpolatedString, SyntaxKind.InterpolatedStringExpression);
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

        DataFlowAnalysis? dataFlow = null;
        if (lambda.Body is ExpressionSyntax expr)
        {
            dataFlow = context.SemanticModel.AnalyzeDataFlow(expr);
        }
        else if (lambda.Body is BlockSyntax block && block.Statements.Count > 0)
        {
            dataFlow = context.SemanticModel.AnalyzeDataFlow(block.Statements.First(), block.Statements.Last());
        }

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

        var creation = (ExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);
        if (typeInfo.Type is null) return;

        if (typeInfo.Type.IsReferenceType)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation(),
                $"'new {typeInfo.Type.Name}()' allocates on the heap"));
        }
    }

    private static void AnalyzeArrayCreation(SyntaxNodeAnalysisContext context)
    {
        if (!ScopeHelper.IsInHotPathContext(context.Node, context.SemanticModel)) return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(),
            "array creation allocates on the heap"));
    }

    private static void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext context)
    {
        if (!ScopeHelper.IsInHotPathContext(context.Node, context.SemanticModel)) return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(),
            "string interpolation allocates a new string"));
    }
}
