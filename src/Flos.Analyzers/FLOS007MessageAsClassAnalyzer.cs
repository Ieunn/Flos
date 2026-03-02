using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// FLOS007: ICommand/IEvent as class → prefer readonly record struct.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS007MessageAsClassAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.FLOS007,
        title: "Command/Event should be readonly record struct",
        messageFormat: "Type '{0}' implements {1} but is declared as a class; prefer 'readonly record struct' for zero-allocation messaging",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (typeSymbol is null) return;

        string? messageType = null;
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            var display = iface.ToDisplayString();
            if (display == TypeNames.ICommand)
            {
                messageType = "ICommand";
                break;
            }
            if (display == TypeNames.IEvent)
            {
                messageType = "IEvent";
                break;
            }
        }

        if (messageType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, classDecl.Identifier.GetLocation(),
                typeSymbol.Name, messageType));
        }
    }
}
