using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;

static class ExpressionHelper
{
    static ExpressionSyntax GetUnparenthesizedExpression(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesizedExpression)
            expression = parenthesizedExpression.Expression;

        return expression;
    }

    static ITypeSymbol? GetTypeSymbolFromIdentifier(SemanticModel model, IdentifierNameSyntax syntax)
        => model.GetTypeInfo(syntax).Type;

    static ITypeSymbol? GetTypeSymbolFromInvocation(SemanticModel model, InvocationExpressionSyntax invocation, bool expectTaskReturnType)
    {
        var invokedSymbol = model.GetSymbolInfo(invocation);
        if (invokedSymbol.Symbol is not IMethodSymbol methodSymbol || methodSymbol.ReturnType is not INamedTypeSymbol returnTypeSymbol)
        {
            return null;
        }

        if (expectTaskReturnType)
        {
            if (returnTypeSymbol.TypeArguments.Length != 1)
            {
                return null;
            }

            return returnTypeSymbol.TypeArguments[0];
        }

        return returnTypeSymbol;
    }

    static ITypeSymbol? GetTypeSymbolFromAwaitExpression(SemanticModel model, AwaitExpressionSyntax awaitSyntax)
    {
        return GetUnparenthesizedExpression(awaitSyntax.Expression) switch
        {
            IdentifierNameSyntax id => GetTypeSymbolFromIdentifier(model, id),
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax id } => GetTypeSymbolFromIdentifier(model, id),
            InvocationExpressionSyntax invocation => GetTypeSymbolFromInvocation(model, invocation, expectTaskReturnType: true),
            _ => null,
        };
    }

    public static ITypeSymbol? GetTypeOfSwitchExpressionOrStatement(SemanticModel model, ExpressionSyntax switchSyntaxExpression)
    {
        if (switchSyntaxExpression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Value" } valueAccess)
        {
            return null;
        }

        return GetUnparenthesizedExpression(valueAccess.Expression) switch
        {
            IdentifierNameSyntax id => GetTypeSymbolFromIdentifier(model, id),
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax id } => GetTypeSymbolFromIdentifier(model, id),
            InvocationExpressionSyntax invocation => GetTypeSymbolFromInvocation(model, invocation, expectTaskReturnType: false),
            AwaitExpressionSyntax awaitSyntax => GetTypeSymbolFromAwaitExpression(model, awaitSyntax),
            _ => null,
        };
    }
}
