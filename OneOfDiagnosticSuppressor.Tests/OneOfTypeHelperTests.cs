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

        INamedTypeSymbol instanceType = parameterTypes[0];
        var expectedSubtypes = parameterTypes.Skip(1);

        var subtypes = OneOfTypeHelper.GetOneOfSubTypes(instanceType);

        Assert.That(subtypes, Is.Not.Null);
        Assert.That(
            subtypes!.SequenceEqual(expectedSubtypes, SymbolEqualityComparer.IncludeNullability),
            Is.True,
            "got {0} but expected {1}",
            string.Join(", ", subtypes!),
            string.Join(", ", expectedSubtypes));
    }

    [Test]
    [TestCase("bool")]
    [TestCase("bool, string")]
    [TestCase("bool?, string")]
    [TestCase("bool, string?")]
    [TestCase("object, object?, object")]
    public void When_type_is_a_OneOfBase_Then_returns_subtypes(string oneOfBaseTypeParameterList)
    {
        var oneOfType = $"OneOf.OneOfBase<{oneOfBaseTypeParameterList}>";
        var dummyParameterList = string.Join(", ", oneOfBaseTypeParameterList.Split(", ").Select((type, index) => $"{type} t{index}"));
        var code = WrapInNamespace($@"static class Foo {{ public static void Do({oneOfType} instance, {dummyParameterList}) {{ }} }}");
        Compilation compilation = CompilationHelper.CreateCompilation(code);
        IReadOnlyList<INamedTypeSymbol> parameterTypes = compilation.GetSymbolsWithName("Do")
            .OfType<IMethodSymbol>()
            .Single().Parameters
            .Select(p => p.Type)
            .OfType<INamedTypeSymbol>()
            .ToArray();

        INamedTypeSymbol instanceType = parameterTypes[0];
        var expectedSubtypes = parameterTypes.Skip(1);

        var subtypes = OneOfTypeHelper.GetOneOfSubTypes(instanceType);

        Assert.That(subtypes, Is.Not.Null);
        Assert.That(
            subtypes!.SequenceEqual(expectedSubtypes, SymbolEqualityComparer.IncludeNullability),
            Is.True,
            "got {0} but expected {1}",
            string.Join(", ", subtypes!),
            string.Join(", ", expectedSubtypes));
    }

    [Test]
    [TestCase("bool")]
    [TestCase("bool, string")]
    [TestCase("bool?, string")]
    [TestCase("bool, string?")]
    [TestCase("object, object?, object")]
    public void When_type_inherits_directly_from_OneOfBase_Then_returns_subtypes(string oneOfBaseTypeParameterList)
    {
        var dummyParameterList = string.Join(", ", oneOfBaseTypeParameterList.Split(", ").Select((type, index) => $"{type} t{index}"));
        var code = WrapInNamespace($@"
class InheritedOneOfBase : OneOf.OneOfBase<{oneOfBaseTypeParameterList}> {{ }}
static class Foo {{ public static void Do(InheritedOneOfBase instance, {dummyParameterList}) {{ }} }}");
        Compilation compilation = CompilationHelper.CreateCompilation(code);
        IReadOnlyList<INamedTypeSymbol> parameterTypes = compilation.GetSymbolsWithName("Do")
            .OfType<IMethodSymbol>()
            .Single().Parameters
            .Select(p => p.Type)
            .OfType<INamedTypeSymbol>()
            .ToArray();

        INamedTypeSymbol instanceType = parameterTypes[0];
        var expectedSubtypes = parameterTypes.Skip(1);

        var subtypes = OneOfTypeHelper.GetOneOfSubTypes(instanceType);

        Assert.That(subtypes, Is.Not.Null);
        Assert.That(
            subtypes!.SequenceEqual(expectedSubtypes, SymbolEqualityComparer.IncludeNullability),
            Is.True,
            "got {0} but expected {1}",
            string.Join(", ", subtypes!),
            string.Join(", ", expectedSubtypes));
    }

    [Test]
    [TestCase("bool")]
    [TestCase("bool, string")]
    [TestCase("bool?, string")]
    [TestCase("bool, string?")]
    [TestCase("object, object?, object")]
    public void When_type_inherits_indirectly_from_OneOfBase_Then_returns_subtypes(string oneOfBaseTypeParameterList)
    {
        var dummyParameterList = string.Join(", ", oneOfBaseTypeParameterList.Split(", ").Select((type, index) => $"{type} t{index}"));
        var code = WrapInNamespace($@"
class InheritedOneOfBase : OneOf.OneOfBase<{oneOfBaseTypeParameterList}> {{ }}
class OtherInheritanceLevel : InheritedOneOfBase {{ }}
class FinalClass : OtherInheritanceLevel {{ }}
static class Foo {{ public static void Do(FinalClass instance, {dummyParameterList}) {{ }} }}");
        Compilation compilation = CompilationHelper.CreateCompilation(code);
        IReadOnlyList<INamedTypeSymbol> parameterTypes = compilation.GetSymbolsWithName("Do")
            .OfType<IMethodSymbol>()
            .Single().Parameters
            .Select(p => p.Type)
            .OfType<INamedTypeSymbol>()
            .ToArray();

        INamedTypeSymbol instanceType = parameterTypes[0];
        var expectedSubtypes = parameterTypes.Skip(1);

        var subtypes = OneOfTypeHelper.GetOneOfSubTypes(instanceType);

        Assert.That(subtypes, Is.Not.Null);
        Assert.That(
            subtypes!.SequenceEqual(expectedSubtypes, SymbolEqualityComparer.IncludeNullability),
            Is.True,
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

        INamedTypeSymbol instanceType = parameterTypes[0];

        var subtypes = OneOfTypeHelper.GetOneOfSubTypes(instanceType);

        Assert.That(subtypes, Is.Null);
    }

    static string WrapInNamespace(string code) => $@"
namespace MyCode
{{
{code}
}}
";
}
