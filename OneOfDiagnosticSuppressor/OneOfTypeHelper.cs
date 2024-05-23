using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
static class OneOfTypeHelper
{
    static readonly string[] OneOfTypeNames = ["OneOf", "OneOfBase"];

    public static IEnumerable<INamedTypeSymbol>? GetOneOfSubTypes(INamedTypeSymbol oneOfCandidate)
    {
        if (OneOfTypeNames.Any(name => oneOfCandidate.Name.Equals(name, StringComparison.Ordinal) &&
            oneOfCandidate.ContainingAssembly.Name.Equals("OneOf", StringComparison.Ordinal)))
        {
            var namedTypeArguments = oneOfCandidate.TypeArguments.OfType<INamedTypeSymbol>().ToArray();
            if (namedTypeArguments.Length == oneOfCandidate.TypeArguments.Length)
            {
                return namedTypeArguments;
            }
        }

        if (TryGetRootType(oneOfCandidate) is INamedTypeSymbol baseType &&
            baseType.Name.Equals("OneOfBase", StringComparison.Ordinal))
        {
            var namedTypeArguments = baseType.TypeArguments.OfType<INamedTypeSymbol>().ToArray();
            if (namedTypeArguments.Length == baseType.TypeArguments.Length)
            {
                return namedTypeArguments;
            }
        }

        return null;

        static INamedTypeSymbol? TryGetRootType(INamedTypeSymbol typeSymbol)
        {
            INamedTypeSymbol rootType = typeSymbol;
            while (rootType.BaseType is not null &&
                   rootType.BaseType.SpecialType is not SpecialType.System_Object)
            {
                rootType = rootType.BaseType;
            }

            return rootType;
        }
    }
}
