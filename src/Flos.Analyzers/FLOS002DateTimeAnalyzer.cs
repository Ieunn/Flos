using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// Roslyn analyzer that reports usage of <c>DateTime.Now</c>, <c>Environment.TickCount</c>,
/// and similar non-deterministic time sources in handlers. Diagnostic FLOS002.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS002DateTimeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS002,
        title: "Do not use DateTime.Now or Environment.TickCount in handlers",
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
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
        var symbol = symbolInfo.Symbol;
        if (symbol is null) return;

        var containingType = symbol.ContainingType?.ToDisplayString();
        var memberName = symbol.Name;

        bool isFlagged = false;
        string? displayName = null;

        if (containingType == TypeNames.DateTime)
        {
            if (memberName is "Now" or "UtcNow" or "Today")
            {
                isFlagged = true;
                displayName = $"DateTime.{memberName}";
            }
        }
        else if (containingType == TypeNames.DateTimeOffset)
        {
            if (memberName is "Now" or "UtcNow")
            {
                isFlagged = true;
                displayName = $"DateTimeOffset.{memberName}";
            }
        }
        else if (containingType == TypeNames.Environment)
        {
            if (memberName is "TickCount" or "TickCount64")
            {
                isFlagged = true;
                displayName = $"Environment.{memberName}";
            }
        }
        else if (containingType == TypeNames.Stopwatch)
        {
            if (memberName is "GetTimestamp" or "StartNew")
            {
                isFlagged = true;
                displayName = $"Stopwatch.{memberName}";
            }
        }

        if (isFlagged && ScopeHelper.IsInScopedContext(memberAccess, context.SemanticModel))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.GetLocation(), displayName));
        }
    }
}
