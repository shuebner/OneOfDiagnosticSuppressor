using Microsoft.CodeAnalysis;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
class OneOfTypeHelperTests
{
    [Test]
    [TestCase("bool")]
    [TestCase("bool, string")]
    [TestCase("bool?, string")]
    [TestCase("bool, string?")]
    [TestCase("object, object?, object")]
    public void When_type_is_a_OneOf_Then_returns_subtypes(string oneOfTypeParameterList)
    {
        var oneOfType = $"OneOf.OneOf<{oneOfTypeParameterList}>";
        var dummyParameterList = string.Join(", ", oneOfTypeParameterList.Split(", ").Select((type, index) => $"{type} t{index}"));
        var code = WrapInNamespace($@"static class Foo {{ public static void Do({oneOfType} instance, {dummyParameterList}) {{ }} }}");
        Compilation compilation = CompilationHelper.CreateCompilation(code);
        IReadOnlyList<INamedTypeSymbol> parameterTypes = compilation.GetSymbolsWithName("Do")
            .OfType<IMethodSymbol>()
            .Single().Parameters
            .Select(p => p.Type)
            .OfType<INamedTypeSymbol>()
            .ToArray();

        INamedTypeSymbol instanceType = parameterTypes.First();
        var expectedSubtypes = parameterTypes.Skip(1);

        var subtypes = OneOfTypeHelper.GetOneOfSubTypes(instanceType);

        Assert.IsNotNull(subtypes);
        Assert.IsTrue(
            subtypes!.SequenceEqual(expectedSubtypes, SymbolEqualityComparer.IncludeNullability),
            "got {0} but expected {1}",
            string.Join(", ", subtypes!),
            string.Join(", ", expectedSubtypes));
    }

    [Test]
    [TestCase("OneOf.IOneOf")]
    [TestCase("OtherNamespace.OneOf<bool>")]
    public void When_type_is_not_a_OneOf_Then_returns_null(string notOneOfType)
    {
        var code = WrapInNamespace($@"static class Foo {{ public static void Do({notOneOfType} instance) {{ }} }}");
        Compilation compilation = CompilationHelper.CreateCompilation(code);
        IReadOnlyList<INamedTypeSymbol> parameterTypes = compilation.GetSymbolsWithName("Do")
            .OfType<IMethodSymbol>()
            .Single().Parameters
            .Select(p => p.Type)
            .OfType<INamedTypeSymbol>()
            .ToArray();

        INamedTypeSymbol instanceType = parameterTypes.First();

        var subtypes = OneOfTypeHelper.GetOneOfSubTypes(instanceType);

        Assert.IsNull(subtypes);
    }

    static string WrapInNamespace(string code) => $@"
namespace MyCode
{{
{code}
}}
";
}
