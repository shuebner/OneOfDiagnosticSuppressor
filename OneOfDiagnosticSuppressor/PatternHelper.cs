using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
static class PatternHelper
{
    // we do not check for nullability here, because the null case must be matched explicitly anyway (?)
    public static bool HandlesAsNonNullableTypeWithoutRestrictions(PatternSyntax patternSyntax, INamedTypeSymbol type, SemanticModel model, Compilation compilation)
    {
        SyntaxNode? typeSource = patternSyntax switch
        {
            ConstantPatternSyntax constantPattern => constantPattern.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
                IdentifierNameSyntax identifier => identifier,
                _ => null
            },
            TypePatternSyntax typePattern => typePattern.Type,
            DeclarationPatternSyntax declaration => declaration.Type,
            RecursivePatternSyntax recursive => recursive.Type,
            _ => null
        };

        if (typeSource is null)
        {
            return false;
        }

        if (model.GetSymbolInfo(typeSource).Symbol is not INamedTypeSymbol namedType)
        {
            return false;
        }

        var typeToBeMatched = type.OriginalDefinition?.SpecialType is SpecialType.System_Nullable_T
            ? type.TypeArguments[0]
            : type;
        bool matchedTypeIsSubtype = compilation.HasImplicitConversion(typeToBeMatched, namedType);

        return patternSyntax switch
        {
            RecursivePatternSyntax recursivePatternSyntax => matchedTypeIsSubtype && IsRecursivePatternNonRestrictive(recursivePatternSyntax),
            _ => matchedTypeIsSubtype
        };

        bool IsSubpatternNonRestrictive(SubpatternSyntax subpatternSyntax) =>
            subpatternSyntax.Pattern switch
            {
                VarPatternSyntax => true,
                RecursivePatternSyntax sub => IsRecursivePatternNonRestrictive(sub),
                DeclarationPatternSyntax declaration => IsDeclarationPatternNonRestrictive(declaration, subpatternSyntax),
                _ => false
            };

        bool IsRecursivePatternNonRestrictive(RecursivePatternSyntax recursivePatternSyntax) =>
            recursivePatternSyntax.PropertyPatternClause?.Subpatterns.All(IsSubpatternNonRestrictive) ?? false;

        bool IsDeclarationPatternNonRestrictive(DeclarationPatternSyntax declarationPatternSyntax, SubpatternSyntax containingSubpatternSyntax)
        {
            SymbolInfo declaredSymbolInfo = model.GetSymbolInfo(declarationPatternSyntax.Type);

            if (declaredSymbolInfo.Symbol is INamedTypeSymbol declaredType)
            {
                if (containingSubpatternSyntax.NameColon is { Name: IdentifierNameSyntax propertyName })
                {
                    ISymbol? maybePropertySymbol = model.GetSymbolInfo(propertyName).Symbol;
                    if (maybePropertySymbol is IPropertySymbol propertySymbol)
                    {
                        if (compilation.HasImplicitConversion(propertySymbol.Type, declaredType))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
