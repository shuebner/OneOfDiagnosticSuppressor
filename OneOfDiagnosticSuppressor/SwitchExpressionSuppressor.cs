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
                ThisExpressionSyntax id  => id as ExpressionSyntax,
                _ => null
            };

            if (valueSource is null)
            {
                return;
            }

            var switcheeModel = context.GetSemanticModel(switchee.SyntaxTree);
            var valueSourceInfo = switcheeModel.GetTypeInfo(valueSource);

            static INamedTypeSymbol? AsOneOfSymbol(ITypeSymbol? t, string name)
            {
                if (t != null &&
                    t.Name.Equals(name, StringComparison.Ordinal) &&
                    t.ContainingAssembly.Name.Equals("OneOf", StringComparison.Ordinal))
                {
                    return t as INamedTypeSymbol;
                }

                return null;
            }

            var oneOfSymbol = AsOneOfSymbol(valueSourceInfo.Type?.BaseType, "OneOfBase")
                              ?? AsOneOfSymbol(valueSourceInfo.Type, "OneOf");

            var namedTypeArguments = oneOfSymbol?.TypeArguments.OfType<INamedTypeSymbol>().ToArray();
            IEnumerable<INamedTypeSymbol>? subtypes = namedTypeArguments?.Length == oneOfSymbol?.TypeArguments.Length
                ? namedTypeArguments
                : null;

            if (oneOfSymbol == null || subtypes == null)
            {
                return;
            }

            bool mustHandleNullCase = oneOfSymbol.TypeArgumentNullableAnnotations.Any(a => a is not NullableAnnotation.NotAnnotated);

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
