using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
class SwitchStatementSuppressorTests
{
    static readonly DiagnosticAnalyzer IDE0010Analyzer = (DiagnosticAnalyzer)(Activator.CreateInstance(
        "Microsoft.CodeAnalysis.CSharp.CodeStyle",
        "Microsoft.CodeAnalysis.CSharp.PopulateSwitch.CSharpPopulateSwitchStatementDiagnosticAnalyzer")?.Unwrap()
        ?? throw new InvalidOperationException("could not instantiate populate switch statement analyzer for IDE0072"));

    Task EnsureNotSuppressed(string code, NullableContextOptions nullableContextOptions) =>
        DiagnosticSuppressorAnalyer.EnsureNotSuppressed(
            new SwitchStatementSuppressor(),
            code,
            nullableContextOptions,
            ("IDE0010", IDE0010Analyzer));

    Task EnsureSuppressed(string code, NullableContextOptions nullableContextOptions) =>
        DiagnosticSuppressorAnalyer.EnsureSuppressed(
            new SwitchStatementSuppressor(),
            SwitchStatementSuppressor.SuppressionDescriptorByDiagnosticId.Values,
            code,
            nullableContextOptions,
            ("IDE0010", IDE0010Analyzer));

    [Test]
    public Task When_not_all_type_arguments_are_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.SwitchStatementOneOfVariationsTemplate(
            typeParams: "int, string",
            switchArms: @"case int: break;");

        return EnsureNotSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_nullable_is_disabled_And_type_arguments_include_reference_type_And_null_is_not_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.SwitchStatementOneOfVariationsTemplate(
            typeParams: "int, string",
            switchArms: @"case int: break;
                          case string: break;");

        return EnsureNotSuppressed(code, NullableContextOptions.Disable);
    }

    [Test]
    public Task When_nullable_is_disabled_And_type_arguments_only_include_nonnullable_value_types_And_null_is_not_matched_Then_suppress()
    {
        var code = CodeHelper.SwitchStatementOneOfVariationsTemplate(
            typeParams: "int, bool",
            switchArms: @"case int: break;
                          case bool: break;");

        return EnsureSuppressed(code, NullableContextOptions.Disable);
    }

    [Test]
    public Task When_all_type_arguments_are_matched_Then_suppress()
    {
        var code = CodeHelper.SwitchStatementOneOfVariationsTemplate(
            typeParams: "int, string",
            switchArms: @"case int: break;
                          case string: break;");

        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_Value_property_is_nested_Then_suppress()
    {
        var typeParams = "int, string";
        var switchArms = @"case int: break;
                           case string: break;"; 
        
        var code = CodeHelper.WrapInNamespaceAndUsing(@$"
[GenerateOneOf]
public partial class GeneratedOneOf: OneOfBase<{typeParams}> {{ }}

static class SwitchTest
{{
    public class Wrapper<T> {{ public Wrapper(T inner) => Inner = inner; public T Inner {{ get; set; }} }}

    public static void DoSwitch(Wrapper<Wrapper<OneOf<{typeParams}>>> wrapper)
    {{
        switch (wrapper.Inner.Inner.Value)
        {{
            {switchArms}
        }};
    }}

    public static void DoSwitch(Wrapper<Wrapper<GeneratedOneOf>> wrapper)
    {{
        switch (wrapper.Inner.Inner.Value)
        {{
            {switchArms}
        }};
    }}
}}
");
        
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_value_types_and_null_is_not_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.SwitchStatementOneOfVariationsTemplate(
            typeParams: "int?, string",
            switchArms: @"case int: break;
                          case string: break;");
        
        return EnsureNotSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_reference_types_and_null_is_not_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.SwitchStatementOneOfVariationsTemplate(
            typeParams: "int, string?",
            switchArms: @"case int: break;
                          case string: break;");

        return EnsureNotSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_value_types_and_null_is_matched_Then_suppress()
    {
        var code = CodeHelper.SwitchStatementOneOfVariationsTemplate(
            typeParams: "int?, string",
            switchArms: @"case int: break;
                          case string: break;
                          case null: break;");
        
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_reference_types_and_null_is_matched_Then_suppress()
    {
        var code = CodeHelper.SwitchStatementOneOfVariationsTemplate(
            typeParams: "int, string?",
            switchArms: @"case int: break;
                          case string: break;
                          case null: break;");

        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }
}
