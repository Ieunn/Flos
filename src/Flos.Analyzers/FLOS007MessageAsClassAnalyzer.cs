using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Flos.Analyzers;

/// <summary>
/// FLOS007: ICommand/IEvent as class or non-readonly struct → prefer readonly record struct.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FLOS007MessageAsClassAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ClassRule = new(
        DiagnosticIds.FLOS007,
        title: "Command/Event should be readonly record struct",
        messageFormat: "Type '{0}' implements {1} but is declared as a class; prefer 'readonly record struct' for zero-allocation messaging",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MutableStructRule = new(
        DiagnosticIds.FLOS007,
        title: "Command/Event should be readonly record struct",
        messageFormat: "Type '{0}' implements {1} but is not a readonly struct; prefer 'readonly record struct' for zero-allocation messaging",
        category: "Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ClassRule, MutableStructRule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.RecordDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeStructDeclaration, SyntaxKind.StructDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeRecordStructDeclaration, SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDecl = (TypeDeclarationSyntax)context.Node;
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
        if (typeSymbol is null) return;

        var messageType = GetMessageInterface(typeSymbol);
        if (messageType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(ClassRule, typeDecl.Identifier.GetLocation(),
                typeSymbol.Name, messageType));
        }
    }

    private static void AnalyzeStructDeclaration(SyntaxNodeAnalysisContext context)
    {
        var structDecl = (StructDeclarationSyntax)context.Node;
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(structDecl) as INamedTypeSymbol;
        if (typeSymbol is null) return;

        var messageType = GetMessageInterface(typeSymbol);
        if (messageType is not null && !typeSymbol.IsReadOnly)
        {
            context.ReportDiagnostic(Diagnostic.Create(MutableStructRule, structDecl.Identifier.GetLocation(),
                typeSymbol.Name, messageType));
        }
    }

    private static void AnalyzeRecordStructDeclaration(SyntaxNodeAnalysisContext context)
    {
        var recordStructDecl = (RecordDeclarationSyntax)context.Node;
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(recordStructDecl) as INamedTypeSymbol;
        if (typeSymbol is null) return;

        var messageType = GetMessageInterface(typeSymbol);
        if (messageType is not null && !typeSymbol.IsReadOnly)
        {
            context.ReportDiagnostic(Diagnostic.Create(MutableStructRule, recordStructDecl.Identifier.GetLocation(),
                typeSymbol.Name, messageType));
        }
    }

    private static string? GetMessageInterface(INamedTypeSymbol typeSymbol)
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            var display = iface.ToDisplayString();
            if (display == TypeNames.ICommand)
                return "ICommand";
            if (display == TypeNames.IEvent)
                return "IEvent";
        }
        return null;
    }
}
