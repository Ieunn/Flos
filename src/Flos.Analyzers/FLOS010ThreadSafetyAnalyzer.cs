using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// Roslyn analyzer that reports core service calls (<c>IMessageBus.Publish</c>, <c>IWorld.Get</c>)
/// from within parallel/worker lambdas. Diagnostic FLOS010.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS010ThreadSafetyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS010,
        title: "Do not call core services from worker threads",
        messageFormat: "Do not call '{0}' from a parallel/worker context; use IDispatcher.Enqueue() instead",
        category: "ThreadSafety",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        var method = symbolInfo.Symbol as IMethodSymbol;
        if (method is null) return;

        var receiverType = ReceiverHelper.GetReceiverTypeString(invocation, context.SemanticModel);
        var containingType = method.ContainingType?.ToDisplayString();
        var methodName = method.Name;

        bool isCoreServiceCall = false;
        string? displayName = null;

        if ((containingType == TypeNames.IMessageBus || receiverType == TypeNames.IMessageBus) && methodName == "Publish")
        {
            isCoreServiceCall = true;
            displayName = "IMessageBus.Publish";
        }
        else if (containingType == TypeNames.IWorld || receiverType == TypeNames.IWorld)
        {
            if (methodName is "Get" or "Register")
            {
                isCoreServiceCall = true;
                displayName = $"IWorld.{methodName}";
            }
        }

        if (!isCoreServiceCall) return;

        if (IsInsideParallelLambda(invocation, context.SemanticModel))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), displayName));
        }
    }

    private static bool IsInsideParallelLambda(SyntaxNode node, SemanticModel model)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is MethodDeclarationSyntax or LocalFunctionStatementSyntax or TypeDeclarationSyntax)
                return false;

            if (ancestor is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
            {
                if (ancestor.Parent is ArgumentSyntax arg &&
                    arg.Parent is ArgumentListSyntax argList &&
                    argList.Parent is InvocationExpressionSyntax outerInvocation)
                {
                    var outerSymbol = model.GetSymbolInfo(outerInvocation).Symbol as IMethodSymbol;
                    if (outerSymbol is not null && IsParallelOrTaskRun(outerSymbol))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        return false;
    }

    private static bool IsParallelOrTaskRun(IMethodSymbol method)
    {
        var containingType = method.ContainingType?.ToDisplayString();
        var name = method.Name;

        if (containingType == TypeNames.Parallel && name is "For" or "ForEach")
            return true;
        if (containingType == TypeNames.Task && name == "Run")
            return true;
        return false;
    }
}
