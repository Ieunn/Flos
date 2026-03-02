using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// Roslyn analyzer that reports <c>Guid.NewGuid()</c> and <c>new Random()</c> in handlers.
/// Diagnostic FLOS016.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS016NonDeterministicAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS016,
        title: "Do not use Guid.NewGuid() or new Random() in handlers",
        messageFormat: "Do not use '{0}' in command handlers, event appliers, or [HotPath] code; it breaks determinism",
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
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        var method = symbolInfo.Symbol as IMethodSymbol;
        if (method is null) return;

        if (method.ContainingType?.ToDisplayString() == TypeNames.Guid && method.Name == "NewGuid")
        {
            if (ScopeHelper.IsInScopedContext(invocation, context.SemanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), "Guid.NewGuid()"));
            }
        }
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);

        if (typeInfo.Type?.ToDisplayString() == TypeNames.SystemRandom)
        {
            if (ScopeHelper.IsInScopedContext(creation, context.SemanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation(), "new Random()"));
            }
        }
    }
}
