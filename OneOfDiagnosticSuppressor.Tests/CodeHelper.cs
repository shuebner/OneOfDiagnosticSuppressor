namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
static class CodeHelper
{
    public static string WrapInNamespaceAndUsingAndClass(string code) => $@"
using OneOf;
using System;

namespace MyCode
{{
    static class SwitchTest
    {{
        {code}
    }}
}}
";
}
