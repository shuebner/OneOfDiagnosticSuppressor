using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SwitchExpressionSuppressor : DiagnosticSuppressor
{
    static readonly string[] SuppressedDiagnosticIds = { "CS8509", "IDE0072" };

    public static readonly IReadOnlyDictionary<string, SuppressionDescriptor> SuppressionDescriptorByDiagnosticId = SuppressedDiagnosticIds.ToDictionary(
        id => id,
        id => new SuppressionDescriptor("ONEOFSUPP001", id, "every possible type of the OneOf instance was matched without restrictions"));

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.CreateRange(SuppressionDescriptorByDiagnosticId.Values);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            if (SuppressedDiagnosticIds.Contains(diagnostic.Id))
            {
                HandleDiagnostic(diagnostic);
            }
        }

        void HandleDiagnostic(Diagnostic diagnostic)
        {
            SyntaxNode? node = diagnostic.Location.SourceTree?
                .GetRoot(context.CancellationToken)
                .FindNode(diagnostic.Location.SourceSpan);

            if (node is null)
            {
                return;
            }

            var switchExpression = node.DescendantNodesAndSelf().OfType<SwitchExpressionSyntax>().FirstOrDefault();

            ExpressionSyntax switchee = switchExpression.GoverningExpression;
            if (switchee is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Value" } valueAccess)
            {
                return;
            }

            var valueSource = valueAccess.Expression switch
            {
                IdentifierNameSyntax id => id,
                MemberAccessExpressionSyntax { Name: IdentifierNameSyntax id } => id,
                _ => null
            };

            if (valueSource is null)
            {
                return;
            }

            var switcheeModel = context.GetSemanticModel(switchee.SyntaxTree);
            var valueSourceInfo = switcheeModel.GetTypeInfo(valueSource);
            var valueSourceType = valueSourceInfo.Type;
            IEnumerable<INamedTypeSymbol> subtypes;
            if (valueSourceType is INamedTypeSymbol t)
            {
                if (t.Name.Equals("OneOf", StringComparison.Ordinal) &&
                    t.ContainingAssembly.Name.Equals("OneOf", StringComparison.Ordinal))
                {
                    var namedTypeArguments = t.TypeArguments.OfType<INamedTypeSymbol>().ToArray();
                    if (namedTypeArguments.Length == t.TypeArguments.Length)
                    {
                        subtypes = namedTypeArguments;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            bool mustHandleNullCase = t.TypeArgumentNullableAnnotations.Any(a => a is not NullableAnnotation.NotAnnotated);

            var arms = switchExpression.Arms;

            if (mustHandleNullCase && !arms.Any(HandlesNullCase))
            {
                return;
            }

            var unhandledSubtypes = subtypes.Where(t => !arms.Any(a => ArmHandlesTypeWithoutRestrictions(a, t)));
            if (unhandledSubtypes.Any())
            {
                return;
            }

            context.ReportSuppression(Suppression.Create(SuppressionDescriptorByDiagnosticId[diagnostic.Id], diagnostic));

            static bool HandlesNullCase(SwitchExpressionArmSyntax a) =>
                // we bail at when clauses and do not try to understand them
                // both "_" and "null" match null
                a.WhenClause is null && a.Pattern is
                    DiscardPatternSyntax or
                    ConstantPatternSyntax { Expression: LiteralExpressionSyntax { Token: SyntaxToken { Value: null } } };

            bool ArmHandlesTypeWithoutRestrictions(SwitchExpressionArmSyntax a, INamedTypeSymbol t) =>
                // we bail at when clauses and do not try to understand them
                a.WhenClause is null && PatternHelper.HandlesAsNonNullableTypeWithoutRestrictions(a.Pattern, t, switcheeModel, context.Compilation);
        }
    }
}
