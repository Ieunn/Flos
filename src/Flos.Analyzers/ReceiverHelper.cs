using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Flos.Analyzers;

/// <summary>
/// Resolves the declared type of the receiver expression in a method invocation.
/// This is necessary because when an interface method is declared on a parent interface
/// (e.g., <c>Get&lt;T&gt;</c> declared on <c>IStateReader</c>), the method's
/// <c>ContainingType</c> will be the declaring interface, not the variable's type.
/// By checking the receiver's type we can correctly detect calls like <c>_world.Get&lt;T&gt;()</c>
/// even when <c>Get&lt;T&gt;</c> is inherited from <c>IStateReader</c>.
/// </summary>
internal static class ReceiverHelper
{
    /// <summary>
    /// Returns the fully qualified name of the receiver expression's type,
    /// or <c>null</c> if it cannot be determined.
    /// </summary>
    public static string? GetReceiverTypeString(
        InvocationExpressionSyntax invocation,
        SemanticModel model)
    {
        ExpressionSyntax? receiver = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
            _ => null,
        };

        if (receiver is null) return null;

        var typeInfo = model.GetTypeInfo(receiver);
        return typeInfo.Type?.ToDisplayString();
    }
}
