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

    static Task EnsureNotSuppressed(string code, NullableContextOptions nullableContextOptions) =>
        DiagnosticSuppressorAnalyer.EnsureNotSuppressed(
            new SwitchStatementSuppressor(),
            code,
            nullableContextOptions,
            ("IDE0010", IDE0010Analyzer));

    static Task EnsureSuppressed(string code, NullableContextOptions nullableContextOptions) =>
        DiagnosticSuppressorAnalyer.EnsureSuppressed(
            new SwitchStatementSuppressor(),
            SwitchStatementSuppressor.SuppressionDescriptorByDiagnosticId.Values,
            code,
            nullableContextOptions,
            ("IDE0010", IDE0010Analyzer));

    [Test]
    public Task When_not_all_type_arguments_are_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static void DoSwitch(OneOf<int, string> oneof)
{
    switch (oneof.Value)
    {
        case int:
            break;
    };
}
");

        return EnsureNotSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_nullable_is_disabled_And_type_arguments_include_reference_type_And_null_is_not_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static void DoSwitch(OneOf<int, string> oneof)
{
    switch (oneof.Value)
    {
        case int:
            break;
        case string:
            break;
    };
}
");

        return EnsureNotSuppressed(code, NullableContextOptions.Disable);
    }

    [Test]
    public Task When_nullable_is_disabled_And_type_arguments_only_include_nonnullable_value_types_And_null_is_not_matched_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static void DoSwitch(OneOf<int, bool> oneof)
{
    switch (oneof.Value)
    {
        case int:
            break;
        case bool:
            break;
    };
}
");

        return EnsureSuppressed(code, NullableContextOptions.Disable);
    }

    [Test]
    public Task When_all_type_arguments_are_matched_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static void DoSwitch(OneOf<int, string> oneof)
{
    switch (oneof.Value)
    {
        case int:
            break;
        case string:
            break;
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
    
public static void DoSwitch(Wrapper<Wrapper<OneOf<int, string>>> wrapper)
{
    switch (wrapper.Inner.Inner.Value)
    {
        case int:
            break;
        case string:
            break;
    };
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_Value_property_is_from_invocation_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static OneOf<int, string> OneOfFunc()
{
    return 1;
}

public static void DoSwitch()
{
    switch (OneOfFunc().Value)
    {
        case int:
            break;

        case string:
            break;
    }
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_value_types_and_null_is_not_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static void DoSwitch(OneOf<int?, string> oneof)
{
    switch (oneof.Value)
    {
        case int:
            break;
        case string:
            break;
    };
}
");
        return EnsureNotSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_reference_types_and_null_is_not_matched_Then_do_not_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static void DoSwitch(OneOf<int, string?> oneof)
{
    switch (oneof.Value)
    {
        case int:
            break;
        case string:
            break;
    };
}
");
        return EnsureNotSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_value_types_and_null_is_matched_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static void DoSwitch(OneOf<int?, string> oneof)
{
    switch (oneof.Value)
    {
        case int:
            break;
        case string:
            break;
        case null:
            break;
    };
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_type_arguments_include_nullable_reference_types_and_null_is_matched_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static void DoSwitch(OneOf<int, string?> oneof)
{
    switch (oneof.Value)
    {
        case int:
            break;
        case string:
            break;
        case null:
            break;
    };
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_Value_property_is_from_await_identifier_expression_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static async Task DoSwitch()
{
    var task = Task.FromResult(OneOf<int, string>.FromT0(0));
    switch ((await task).Value)
    {
        case int:
            break;

        case string:
            break;
    }
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_Value_property_if_from_nested_await_expression_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
static Task<Task<OneOf<int, string>>> Func()
{
    return Task.FromResult(Task.FromResult(OneOf<int, string>.FromT0(0)));
}

public static async Task DoSwitch()
{
    switch ((await await Func()).Value)
    {
        case int:
            break;

        case string:
            break;
    }
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_Value_property_is_from_await_member_expression_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static async Task DoSwitch()
{
    var obj = new { Task = Task.FromResult(OneOf<int, string>.FromT0(0)) };
    switch ((await obj.Task).Value)
    {
        case int:
            break;

        case string:
            break;
    }
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }

    [Test]
    public Task When_Value_property_is_from_await_invocation_expression_Then_suppress()
    {
        var code = CodeHelper.WrapInNamespaceAndUsingAndClass(@"
public static Task<OneOf<int, string>> AsyncFunc()
{
    return Task.FromResult<OneOf<int, string>>(1);
}

public static async Task DoSwitch()
{
    switch ((await AsyncFunc()).Value)
    {
        case int:
            break;

        case string:
            break;
    }
}
");
        return EnsureSuppressed(code, NullableContextOptions.Enable);
    }
}
