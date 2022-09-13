using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SwitchStatementSuppressor : DiagnosticSuppressor
{
    static readonly string[] SuppressedDiagnosticIds = { "IDE0010" };

    public static readonly IReadOnlyDictionary<string, SuppressionDescriptor> SuppressionDescriptorByDiagnosticId = SuppressedDiagnosticIds.ToDictionary(
        id => id,
        id => new SuppressionDescriptor("ONEOFSUPP002", id, "every possible type of the OneOf instance was matched without restrictions"));

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

            if (node is not SwitchStatementSyntax switchStatement)
            {
                return;
            }

            ExpressionSyntax switchee = switchStatement.Expression;
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

            SemanticModel switcheeModel = context.GetSemanticModel(switchee.SyntaxTree);
            TypeInfo valueSourceInfo = switcheeModel.GetTypeInfo(valueSource);
            ITypeSymbol? valueSourceType = valueSourceInfo.Type;

            if (!(valueSourceType is INamedTypeSymbol t && OneOfTypeHelper.GetOneOfSubTypes(t) is IEnumerable<INamedTypeSymbol> subtypes))
            {
                return;
            }

            bool mustHandleNullCase = t.TypeArgumentNullableAnnotations.Any(a => a is not NullableAnnotation.NotAnnotated);

            var sections = switchStatement.Sections;

            if (mustHandleNullCase && !sections.Any(SectionHandlesNullCase))
            {
                return;
            }

            var unhandledSubtypes = subtypes.Where(t => !sections.Any(a => SectionHandlesTypeWithoutRestrictions(a, t)));
            if (unhandledSubtypes.Any())
            {
                return;
            }

            context.ReportSuppression(Suppression.Create(SuppressionDescriptorByDiagnosticId[diagnostic.Id], diagnostic));
                
            static bool SectionHandlesNullCase(SwitchSectionSyntax s) =>
                // we bail at when clauses and do not try to understand them
                // both "_" and "null" match null
                s.Labels.Any(LabelHandlesNullCase);

            static bool LabelHandlesNullCase(SwitchLabelSyntax s) =>
                s is CaseSwitchLabelSyntax { Value: LiteralExpressionSyntax { Token: SyntaxToken { Value: null } } };

            bool SectionHandlesTypeWithoutRestrictions(SwitchSectionSyntax s, INamedTypeSymbol t) =>
                s.Labels.Any(s => LabelHandlesTypeWithoutRestriction(s, t));

            bool LabelHandlesTypeWithoutRestriction(SwitchLabelSyntax s, INamedTypeSymbol t) =>
                s switch
                {
                    CaseSwitchLabelSyntax @case => CaseSwitchLabelHandlesTypeWithoutRestriction(@case, t),
                    CasePatternSwitchLabelSyntax { WhenClause: null } casePattern => PatternHelper.HandlesAsNonNullableTypeWithoutRestrictions(casePattern.Pattern, t, switcheeModel, context.Compilation),
                    _ => false
                };

            bool CaseSwitchLabelHandlesTypeWithoutRestriction(CaseSwitchLabelSyntax s, INamedTypeSymbol t) =>
                switcheeModel.GetSymbolInfo(s.Value).Symbol is INamedTypeSymbol type && context.Compilation.HasImplicitConversion(t, type);
        }
    }
}
