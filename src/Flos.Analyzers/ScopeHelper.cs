using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flos.Analyzers;

internal static class ScopeHelper
{
    /// <summary>
    /// Returns true if the node is inside a type that implements ICommandHandler&lt;&gt; or IEventApplier&lt;,&gt;.
    /// </summary>
    public static bool IsInHandlerOrApplierType(SyntaxNode node, SemanticModel model)
    {
        var typeDecl = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDecl is null) return false;

        var typeSymbol = model.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
        if (typeSymbol is null) return false;

        return ImplementsHandlerOrApplier(typeSymbol);
    }

    /// <summary>
    /// Returns true if the node is inside a method or type annotated with [HotPath].
    /// </summary>
    public static bool IsInHotPathContext(SyntaxNode node, SemanticModel model)
    {
        var methodDecl = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDecl is not null)
        {
            var methodSymbol = model.GetDeclaredSymbol(methodDecl);
            if (methodSymbol is not null && HasHotPathAttribute(methodSymbol))
                return true;
        }

        var typeDecl = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDecl is not null)
        {
            var typeSymbol = model.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
            if (typeSymbol is not null && HasHotPathAttribute(typeSymbol))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the node is in a scoped context: handler/applier type OR [HotPath] method/type.
    /// </summary>
    public static bool IsInScopedContext(SyntaxNode node, SemanticModel model)
    {
        return IsInHandlerOrApplierType(node, model) || IsInHotPathContext(node, model);
    }

    private static bool ImplementsHandlerOrApplier(INamedTypeSymbol typeSymbol)
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            var original = iface.OriginalDefinition;
            if (IsInterface(original, TypeNames.ICommandHandlerNamespace, TypeNames.ICommandHandlerName, TypeNames.ICommandHandlerArity))
                return true;
            if (IsInterface(original, TypeNames.IEventApplierNamespace, TypeNames.IEventApplierName, TypeNames.IEventApplierArity))
                return true;
        }
        return false;
    }

    private static bool IsInterface(INamedTypeSymbol iface, string ns, string name, int arity)
    {
        return iface.Arity == arity
            && iface.Name == name
            && iface.ContainingNamespace?.ToDisplayString() == ns;
    }

    private static bool HasHotPathAttribute(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == TypeNames.HotPathAttributeFullName)
                return true;
        }
        return false;
    }
}
