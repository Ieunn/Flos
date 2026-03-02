using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// FLOS009: float/double in Handler/System → consider fixed-point.
/// Configurable, off by default. For competitive networked games requiring bit-exact replay.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS009FloatingPointAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS009,
        title: "Floating-point in handler/system",
        messageFormat: "'{0}' used in handler/system; consider fixed-point for cross-platform determinism",
        category: "Determinism",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeVariable, SyntaxKind.VariableDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeVariable(SyntaxNodeAnalysisContext context)
    {
        var varDecl = (VariableDeclarationSyntax)context.Node;
        if (!ScopeHelper.IsInScopedContext(varDecl, context.SemanticModel)) return;

        var typeInfo = context.SemanticModel.GetTypeInfo(varDecl.Type, context.CancellationToken);
        CheckFloatingPoint(typeInfo.Type, varDecl.Type.GetLocation(), context);
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var param = (ParameterSyntax)context.Node;
        if (param.Type is null) return;
        if (!ScopeHelper.IsInScopedContext(param, context.SemanticModel)) return;

        var typeInfo = context.SemanticModel.GetTypeInfo(param.Type, context.CancellationToken);
        CheckFloatingPoint(typeInfo.Type, param.Type.GetLocation(), context);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var fieldDecl = (FieldDeclarationSyntax)context.Node;
        if (!ScopeHelper.IsInScopedContext(fieldDecl, context.SemanticModel)) return;

        var typeInfo = context.SemanticModel.GetTypeInfo(fieldDecl.Declaration.Type, context.CancellationToken);
        CheckFloatingPoint(typeInfo.Type, fieldDecl.Declaration.Type.GetLocation(), context);
    }

    private static void CheckFloatingPoint(ITypeSymbol? type, Location location, SyntaxNodeAnalysisContext context)
    {
        if (type is null) return;

        if (type.SpecialType == SpecialType.System_Single)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, "float"));
        }
        else if (type.SpecialType == SpecialType.System_Double)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, "double"));
        }
    }
}
