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

    static ITypeSymbol? GetTypeSymbolFromInvocation(SemanticModel model, InvocationExpressionSyntax invocation)
    {
        var invokedSymbol = model.GetSymbolInfo(invocation);
        if (invokedSymbol.Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }
        return methodSymbol.ReturnType;
    }

    static ITypeSymbol? GetTypeOfTask(ITypeSymbol? taskSymbol)
    {
        if (taskSymbol is not INamedTypeSymbol namedTaskSymbol)
        {
            return null;
        }
        if (namedTaskSymbol.TypeArguments.Length != 1)
        {
            return null;
        }

        return namedTaskSymbol.TypeArguments[0];
    }

    static ITypeSymbol? GetTypeSymbolFromExpression(SemanticModel model, ExpressionSyntax expression)
    {
        return GetUnparenthesizedExpression(expression) switch
        {
            IdentifierNameSyntax id => GetTypeSymbolFromIdentifier(model, id),
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax id } => GetTypeSymbolFromIdentifier(model, id),
            InvocationExpressionSyntax invocation => GetTypeSymbolFromInvocation(model, invocation),
            AwaitExpressionSyntax awaitSyntax => GetTypeOfTask(GetTypeSymbolFromExpression(model, awaitSyntax.Expression)),
            _ => null,
        };

    }

    public static ITypeSymbol? GetTypeOfSwitchExpressionOrStatement(SemanticModel model, ExpressionSyntax switchSyntaxExpression)
    {
        if (switchSyntaxExpression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Value" } valueAccess)
        {
            return null;
        }

        return GetTypeSymbolFromExpression(model, valueAccess.Expression);
    }
}
