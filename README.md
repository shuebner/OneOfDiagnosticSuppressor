# Exhaustiveness check for `OneOf<...>.Value`

This project enhances the exhaustiveness check of the C# compiler for switch statements and switch expressions on the `OneOf<...>.Value` property from the [OneOf library](https://github.com/mcintyre321/OneOf).

This gives you all the syntactic power of native `switch` while keeping compile-time safety. Compile-time safety is achieved by escalating the exhaustiveness diagnostics' severity to error in your `.editorconfig`.

See [this issue](https://github.com/mcintyre321/OneOf/issues/109).

```csharp
using OneOf;

#nullable enable
// without this suppressor: warning CS8509
// with this suppressor: no warnings
public static int Get(OneOf<int, string> intOrString) => intOrString.Value switch
{
    int => 1,
    string => 2
};

#nullable disable
// without this suppressor: warning CS8509
// with this suppressor: no warnings
public static int Get(OneOf<int, string> intOrString) => intOrString.Value switch
{
    int => 1,
    string => 2
    // without NRTs, null must be matched to be exhaustive
    null => 0 
};
```

Get on [nuget.org](https://www.nuget.org/packages/SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression) or just include with
```csproj
<PackageReference Include="SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression" Version="0.0.1" PrivateAssets="All" />
```

There are no attributes and no configuration.
It will just do The Right Thing™.

See the test samples for [switch statement](https://github.com/shuebner/OneOfDiagnosticSuppressor/blob/main/OneOfDiagnosticSuppressor.Tests/SwitchStatementSuppressorTests.cs) and [switch expression](https://github.com/shuebner/OneOfDiagnosticSuppressor/blob/main/OneOfDiagnosticSuppressor.Tests/SwitchExpressionSuppressorTests.cs) to see what is supported.
I may add more documentation and examples in this README soon.


## Features

### NRT-aware

### Pattern matching support

Pattern matching, including nested pattern matching is taken into account.

# Background

## The problem with the built-in `Match` method

`OneOf` encourages you to use its `Match` method and supply N lambda expressions to do the switching. This has the advantage that breaking changes in the type arguments of the `OneOf<...>` instance on which you are switching lead to compiler errors, because the number or type of the lambdas no longer fits.

However, this approach can be cumbersome in some cases.
It prevents you from using pattern matching.
It is also inferior to C#'s native `switch` expression when it comes to type inference.

Because `OneOf<...>.Value` is of type `object`, using a native `switch` expression on `Value` will lead to compiler warnings about exhaustiveness.
This may tempt you to either include a default/discard case, or suppress the exhaustiveness warning. In both scenarios you lose compile-time checking and will not get a warning when the type arguments of the underlying `OneOf<...>` change.

Hence this project, that will suppress the exhaustiveness warnings based on the guarantees that the OneOf library provides.
As long as you match all type arguments (and maybe `null`), you will no longer get exhaustiveness warnings.

Now that there are no more false positives, you can escalate the exhaustiveness diagnostics to errors in your `.editorconfig`.

You now have the best of both worlds: the syntactic power of native `switch` and the compile-time safety of `OneOf`'s `Match` method.

## Implementation

It is implemented as a `DiagnosticSuppressor`.
It suppresses the compiler's own exhaustiveness warnings only when it is sure that the switch is exhaustive.
It suppresses IDE0010, IDE0072 and CS8509.