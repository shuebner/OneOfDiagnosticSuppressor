﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            // we need to explicitly handle the await expression because otherwise we get
            // wrong nullable annotations on the generic type arguments of OneOf<...> when
            // the Task<OneOf<...>> was declared with the var keyword
            AwaitExpressionSyntax awaitSyntax => GetTypeOfTask(GetTypeSymbolFromExpression(model, awaitSyntax.Expression)),
            _ => model.GetTypeInfo(expression).Type,
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
