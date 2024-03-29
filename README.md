# Exhaustiveness check for `OneOf<...>.Value`

This project enhances the exhaustiveness check of the C# compiler for switch statements and switch expressions on the `OneOf<...>.Value` property from the [OneOf library](https://github.com/mcintyre321/OneOf).

This gives you all the syntactic power of native `switch` while keeping compile-time safety. Compile-time safety is achieved by escalating the exhaustiveness diagnostics' severity to error in your `.editorconfig`.

See [this issue](https://github.com/mcintyre321/OneOf/issues/109).

(If you are using structurally closed type hierarchies instead, see my other repo [ClosedTypeHierarchyDiagnosticSuppressor](https://github.com/shuebner/ClosedTypeHierarchyDiagnosticSuppressor).)

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

## Treating non-exhaustive switches as errors

In your project, enable IDE0010 (for switch statements) and IDE0072 (for switch expressions, not strictly necessary because we have CS8509 anyway) like this:
```csproj
<PropertyGroup>
  <!-- enables (among others) IDE0010 and IDE0072 -->
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

In your `.editorconfig` configure the severity of the exhaustiveness diagnostics:
```
[*.cs]
dotnet_diagnostic.IDE0010.severity = error
dotnet_diagnostic.IDE0072.severity = error
dotnet_diagnostic.CS8509.severity = error
```

# Development Experience

If you set diagnostic severity to error for diagnostics that may be suppressed by a diagnostic suppressor like this one, you may have to take additional action for a good development experience.

## Visual Studio (for Windows)

By default, Visual Studio does not run diagnostic suppressors when the build is implicitly triggered like by running a test.
If you have errors that are suppressed by diagnostic suppressors, those builds will fail, e. g. preventing the test from running (unless you do an explicit build first).
You can configure Visual Studio to always run analyzers (towards whom diagnostic suppressors are counted):

![image](https://user-images.githubusercontent.com/1770684/182022215-23902b8a-2c01-4fe1-bb47-943fc7bda140.png)

See also [here](https://developercommunity2.visualstudio.com/t/Test-run-fails-build-because-Diagnostic/10023425).

## Rider

[Rider does not support Diagnostic Suppressors](https://youtrack.jetbrains.com/issue/RSRP-481121) as of 2022-07-31.
Your development experience may suffer.

## Visual Studio Code

[OmniSharp and thus Visual Studio Code does not support Diagnostic Suppressors](https://github.com/OmniSharp/omnisharp-roslyn/issues/1711) as of 2022-07-31.
Your development experience may suffer.

Thanks to [rutgersc](https://github.com/rutgersc) for [bringing this up](https://github.com/shuebner/OneOfDiagnosticSuppressor/issues/1).

## Visual Studio for Mac
[Visual Studio for Mac does not support Diagnostic Suppressors](https://developercommunity.visualstudio.com/t/Support-for-Diagnostic-Suppressors/10247137?q=Diagnostic+Suppressors) as of 2023-01-06.

# Features

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
