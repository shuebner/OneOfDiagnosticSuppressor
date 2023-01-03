namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
static class CodeHelper
{
    public static string WrapInNamespaceAndUsingAndClass(string code) => $@"
using OneOf;
using System;
using System.Threading.Tasks;

namespace MyCode
{{
    static class SwitchTest
    {{
        {code}
    }}
}}
";
}
