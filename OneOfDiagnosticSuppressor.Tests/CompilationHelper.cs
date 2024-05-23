using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Reflection;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
static class CompilationHelper
{
    public static Compilation CreateCompilation(
        string? code = null,
        NullableContextOptions nullableContextOptions = NullableContextOptions.Disable)
    {
        var syntaxTrees = code is null
            ? null
            : new[] { CSharpSyntaxTree.ParseText(code) };

        return CSharpCompilation.Create(
            Guid.NewGuid().ToString("N"),
            syntaxTrees,
            references: new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(global::OneOf.OneOf<bool>).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=8.0.0.0").Location),
            },
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                reportSuppressedDiagnostics: true,
                nullableContextOptions: nullableContextOptions));
    }
}
