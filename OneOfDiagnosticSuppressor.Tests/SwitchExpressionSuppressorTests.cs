using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
class SwitchExpressionSuppressorTests
{
    static readonly DiagnosticAnalyzer IDE0072Analyzer = (DiagnosticAnalyzer)(Activator.CreateInstance(
        "Microsoft.CodeAnalysis.CSharp.CodeStyle",
        "Microsoft.CodeAnalysis.CSharp.PopulateSwitch.CSharpPopulateSwitchExpressionDiagnosticAnalyzer")?.Unwrap()
        ?? throw new InvalidOperationException("could not instantiate populate switch expression analyzer for IDE0072"));

    Task EnsureNotSuppressed(string code, NullableContextOptions nullableContextOptions) =>
        DiagnosticSuppressorAnalyer.EnsureNotSuppressed(
            new SwitchExpressionSuppressor(),
            code,
            nullableContextOptions,
            ("IDE0072", IDE0072Analyzer));

    Task EnsureSuppressed(string code, NullableContextOptions nullableContextOptions) =>
        DiagnosticSuppressorAnalyer.EnsureSuppressed(
            new SwitchExpressionSuppressor(),
            SwitchExpressionSuppressor.SuppressionDescriptorByDiagnosticId.Values,
            code,
            nullableContextOptions,
            ("IDE0072", IDE0072Analyzer));


    [Test]
    public Task When_not_all_type_arguments_are_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static int DoSwitch(OneOf<int, string> oneof)
{
    return oneof.Value switch
    {
        int => 0
    };
}
");

        return EnsureNotSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_nullable_is_disabled_And_type_arguments_include_reference_type_And_null_is_not_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static int DoSwitch(OneOf<int, string> oneof)
{
    return oneof.Value switch
    {
        int => 0,
        string => 1
    };
}
");

        return EnsureNotSuppressed(code, NullableContextOptions.Disable);
    }

    [Test]
    public Task When_nullable_is_disabled_And_type_arguments_only_include_nonnullable_value_types_And_null_is_not_matched_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static int DoSwitch(OneOf<int, bool> oneof)
{
    return oneof.Value switch
    {
        int => 0,
        bool => 1
    };
}
");

        return EnsureSuppressed(code, NullableContextOptions.Disable);
    }

    [Test]
    public Task When_all_type_arguments_are_matched_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static int DoSwitch(OneOf<int, string> oneof)
{
    return oneof.Value switch
    {
        int => 0,
        string => 1
    };
}
");

        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_Value_property_is_nested_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public class Wrapper<T> { public Wrapper(T inner) => Inner = inner; public T Inner { get; set; } }
    
public static int DoSwitch(Wrapper<Wrapper<OneOf<int, string>>> wrapper)
{
    return wrapper.Inner.Inner.Value switch
    {
        int => 0,
        string => 1
    };
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_value_types_and_null_is_not_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static int DoSwitch(OneOf<int?, string> oneof)
{
    return oneof.Value switch
    {
        int => 0,
        string => 1
    };
}
");
        return EnsureNotSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_reference_types_and_null_is_not_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static int DoSwitch(OneOf<int, string?> oneof)
{
    return oneof.Value switch
    {
        int => 0,
        string => 1
    };
}
");
        return EnsureNotSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_value_types_and_null_is_matched_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static int DoSwitch(OneOf<int?, string> oneof)
{
    return oneof.Value switch
    {
        int => 0,
        string => 1,
        null => 2
    };
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_reference_types_and_null_is_matched_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static int DoSwitch(OneOf<int, string?> oneof)
{
    return oneof.Value switch
    {
        int => 0,
        string => 1,
        null => 2
    };
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }
}
