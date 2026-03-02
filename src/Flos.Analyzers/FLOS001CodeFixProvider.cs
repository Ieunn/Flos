using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flos.Analyzers;

/// <summary>
/// Code fix provider for FLOS001 that suggests replacing <c>System.Random</c> with <c>IRandom</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FLOS001CodeFixProvider)), Shared]
public sealed class FLOS001CodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.FLOS001);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async System.Threading.Tasks.Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        if (node is ObjectCreationExpressionSyntax creation)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace with IRandom (inject via constructor)",
                    createChangedDocument: async ct =>
                    {
                        var newRoot = root.ReplaceNode(
                            creation,
                            SyntaxFactory.ParseExpression("default(Flos.Random.IRandom) /* TODO: inject IRandom via constructor */")
                                .WithTriviaFrom(creation));
                        return context.Document.WithSyntaxRoot(newRoot);
                    },
                    equivalenceKey: DiagnosticIds.FLOS001),
                diagnostic);
        }
    }
}
