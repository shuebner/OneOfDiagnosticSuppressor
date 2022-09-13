using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
static class OneOfTypeHelper
{
    public static IEnumerable<INamedTypeSymbol>? GetOneOfSubTypes(INamedTypeSymbol oneOfCandidate)
    {
        if (oneOfCandidate.Name.Equals("OneOf", StringComparison.Ordinal) &&
            oneOfCandidate.ContainingAssembly.Name.Equals("OneOf", StringComparison.Ordinal)
            )
        {
            var namedTypeArguments = oneOfCandidate.TypeArguments.OfType<INamedTypeSymbol>().ToArray();
            if (namedTypeArguments.Length == oneOfCandidate.TypeArguments.Length)
            {
                return namedTypeArguments;
            }
        }

        return null;
    }
}
