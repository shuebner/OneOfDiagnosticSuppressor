namespace SvSoft.OneOf.Analyzers.SwitchDiagnosticSuppression;
static class CodeHelper
{
    public static string WrapInNamespaceAndUsing(string code) => $@"
using OneOf;
using System;

namespace MyCode
{{
    {code}
}}
";

    public static string SwitchExpressionOneOfVariationsTemplate(string typeParams, string returnType, string switchArms)
    {
        return WrapInNamespaceAndUsing(@$"
[GenerateOneOf]
public partial class GeneratedOneOf: OneOfBase<{typeParams}>
{{
    public {returnType} DoSwitch()
    {{
        return this.Value switch
        {{
            {switchArms}
        }};
    }}
}}

static class SwitchTest
{{
    public static {returnType} DoSwitch(OneOf<{typeParams}> oneof)
    {{
        return oneof.Value switch
        {{
            {switchArms}
        }};
    }}

    public static {returnType} DoSwitch(GeneratedOneOf oneof)
    {{
        return oneof.Value switch
        {{
            {switchArms}
        }};
    }}
}}
");
    }
    
    public static string SwitchStatementOneOfVariationsTemplate(string typeParams, string switchArms)
    {
        return WrapInNamespaceAndUsing(@$"
[GenerateOneOf]
public partial class GeneratedOneOf: OneOfBase<{typeParams}>
{{
    public void DoSwitch()
    {{
        switch (this.Value)
        {{
            {switchArms}
        }};
    }}
}}

static class SwitchTest
{{
    public static void DoSwitch(OneOf<{typeParams}> oneof)
    {{
        switch (oneof.Value)
        {{
            {switchArms}
        }};
    }}

    public static void DoSwitch(GeneratedOneOf oneof)
    {{
        switch (oneof.Value)
        {{
            {switchArms}
        }};
    }}
}}
");
    }
}
