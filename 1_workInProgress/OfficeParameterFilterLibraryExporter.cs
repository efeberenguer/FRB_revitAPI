using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;


// ============================================================================
// OFFICE PARAMETER FILTER LIBRARY EXPORTER
// Revit 2024 / Launchpad
//
// READ ONLY:
// - No transaction
// - Does not modify the RVT
//
// Assumption:
// Launchpad provides:
//     Document doc
//
// Output:
//     Desktop\Office_Filter_Library_yyyy.MM.json
//
// Schema:
//     1.1
//
// Canonical parameter identity:
//     Built-in parameter : BIP:<integer id>
//     Shared parameter   : GUID:<guid>
//     Project parameter  : PROJECT:<name>
//     Other              : NAME:<name>
//     Last resort        : LOCAL:<element id>
//
// IMPORTANT:
// Raw ElementIds are retained in JSON for diagnostics,
// but are NOT used for shared/project-parameter matching.
// ============================================================================


// ============================================================================
// SETTINGS
// ============================================================================

string libraryName = "Office Parameter Filter Library";
string libraryVersion = DateTime.Now.ToString("yyyy.MM");

string desktop =
    Environment.GetFolderPath(
        Environment.SpecialFolder.DesktopDirectory
    );

string outputPath =
    Path.Combine(
        desktop,
        "Office_Filter_Library_" +
        libraryVersion +
        ".json"
    );


// ============================================================================
// BASIC JSON HELPERS
// ============================================================================

Func<string, string> JsonEscape = delegate(string value)
{
    if (value == null)
        return null;

    StringBuilder sb = new StringBuilder();

    foreach (char c in value)
    {
        switch (c)
        {
            case '"':
                sb.Append("\\\"");
                break;

            case '\\':
                sb.Append("\\\\");
                break;

            case '\b':
                sb.Append("\\b");
                break;

            case '\f':
                sb.Append("\\f");
                break;

            case '\n':
                sb.Append("\\n");
                break;

            case '\r':
                sb.Append("\\r");
                break;

            case '\t':
                sb.Append("\\t");
                break;

            default:
                if (c < 32)
                {
                    sb.Append(
                        "\\u" +
                        ((int)c).ToString("x4")
                    );
                }
                else
                {
                    sb.Append(c);
                }
                break;
        }
    }

    return sb.ToString();
};


Func<string, string> JString = delegate(string value)
{
    if (value == null)
        return "null";

    return "\"" + JsonEscape(value) + "\"";
};


Func<bool, string> JBool = delegate(bool value)
{
    return value ? "true" : "false";
};


// ============================================================================
// REFLECTION HELPERS
//
// Reflection is used deliberately for rule subclasses because Revit exposes
// rule-specific values through different methods.
// ============================================================================

Func<object, string, object> InvokeNoArg =
    delegate(object target, string methodName)
{
    if (target == null)
        return null;

    try
    {
        MethodInfo method =
            target
                .GetType()
                .GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.Public
                );

        if (method == null)
            return null;

        return method.Invoke(
            target,
            null
        );
    }
    catch
    {
        return null;
    }
};


Func<object, string, object> GetPublicProperty =
    delegate(object target, string propertyName)
{
    if (target == null)
        return null;

    try
    {
        PropertyInfo property =
            target
                .GetType()
                .GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public
                );

        if (property == null)
            return null;

        return property.GetValue(
            target,
            null
        );
    }
    catch
    {
        return null;
    }
};


// ============================================================================
// SHA-256
// ============================================================================

Func<string, string> Sha256 = delegate(string value)
{
    if (value == null)
        value = "";

    using (SHA256 sha = SHA256.Create())
    {
        byte[] bytes =
            Encoding.UTF8.GetBytes(value);

        byte[] hash =
            sha.ComputeHash(bytes);

        StringBuilder sb =
            new StringBuilder();

        foreach (byte b in hash)
        {
            sb.Append(
                b.ToString("x2")
            );
        }

        return sb.ToString();
    }
};


// ============================================================================
// PARAMETER METADATA
// ============================================================================

Func<ElementId, string> GetParameterName =
    delegate(ElementId parameterId)
{
    if (parameterId == null)
        return null;

    if (
        parameterId == ElementId.InvalidElementId ||
        parameterId.IntegerValue == -1
    )
    {
        return null;
    }


    // ------------------------------------------------------------------------
    // Built-in parameter
    // ------------------------------------------------------------------------

    if (parameterId.IntegerValue < 0)
    {
        try
        {
            BuiltInParameter bip =
                (BuiltInParameter)
                parameterId.IntegerValue;

            string label =
                LabelUtils.GetLabelFor(bip);

            if (!string.IsNullOrWhiteSpace(label))
                return label;
        }
        catch
        {
        }

        return
            parameterId.IntegerValue
                .ToString(
                    CultureInfo.InvariantCulture
                );
    }


    // ------------------------------------------------------------------------
    // Parameter element
    // ------------------------------------------------------------------------

    try
    {
        Element element =
            doc.GetElement(parameterId);

        if (element != null)
            return element.Name;
    }
    catch
    {
    }

    return null;
};


Func<ElementId, string> GetParameterSource =
    delegate(ElementId parameterId)
{
    if (parameterId == null)
        return "Other";

    if (
        parameterId == ElementId.InvalidElementId ||
        parameterId.IntegerValue == -1
    )
    {
        return "Other";
    }

    if (parameterId.IntegerValue < 0)
        return "Built-in";

    try
    {
        Element element =
            doc.GetElement(parameterId);

        if (element is SharedParameterElement)
            return "Shared";

        if (element is ParameterElement)
            return "Project";
    }
    catch
    {
    }

    return "Other";
};


Func<ElementId, string> GetParameterGuid =
    delegate(ElementId parameterId)
{
    if (parameterId == null)
        return null;

    if (parameterId.IntegerValue < 0)
        return null;

    try
    {
        SharedParameterElement spe =
            doc.GetElement(parameterId)
            as SharedParameterElement;

        if (spe != null)
        {
            return
                spe.GuidValue
                    .ToString()
                    .ToLowerInvariant();
        }
    }
    catch
    {
    }

    return null;
};


// ============================================================================
// PORTABLE PARAMETER KEY
//
// This is the parameter identity used by canonical filter signatures.
// ============================================================================

Func<ElementId, string> GetParameterKey =
    delegate(ElementId parameterId)
{
    if (parameterId == null)
        return "UNKNOWN";

    if (
        parameterId == ElementId.InvalidElementId ||
        parameterId.IntegerValue == -1
    )
    {
        return "UNKNOWN";
    }


    // ------------------------------------------------------------------------
    // Built-in parameter
    //
    // Stable across RVT files.
// ------------------------------------------------------------------------

    if (parameterId.IntegerValue < 0)
    {
        return
            "BIP:" +
            parameterId.IntegerValue
                .ToString(
                    CultureInfo.InvariantCulture
                );
    }


    try
    {
        Element element =
            doc.GetElement(parameterId);


        // --------------------------------------------------------------------
        // Shared parameter
        //
        // GUID is the portable identity.
        // --------------------------------------------------------------------

        SharedParameterElement spe =
            element as SharedParameterElement;

        if (spe != null)
        {
            return
                "GUID:" +
                spe.GuidValue
                    .ToString()
                    .ToLowerInvariant();
        }


        // --------------------------------------------------------------------
        // Project parameter
        //
        // No GUID is available, so V1.1 falls back to parameter name.
        // --------------------------------------------------------------------

        ParameterElement pe =
            element as ParameterElement;

        if (pe != null)
        {
            return
                "PROJECT:" +
                (pe.Name ?? "")
                    .Trim();
        }


        // --------------------------------------------------------------------
        // Other identifiable element
        // --------------------------------------------------------------------

        if (element != null)
        {
            return
                "NAME:" +
                (element.Name ?? "")
                    .Trim();
        }
    }
    catch
    {
    }


    // ------------------------------------------------------------------------
    // Last resort.
    //
    // LOCAL explicitly means the identity is document-specific.
    // ------------------------------------------------------------------------

    return
        "LOCAL:" +
        parameterId.IntegerValue
            .ToString(
                CultureInfo.InvariantCulture
            );
};


// ============================================================================
// RULE VALUES
// ============================================================================

Func<FilterRule, object> GetRuleValue =
    delegate(FilterRule rule)
{
    if (rule == null)
        return null;


    string[] methods =
    {
        "GetStringValue",
        "GetDoubleValue",
        "GetIntegerValue",
        "GetElementIdValue"
    };


    foreach (string methodName in methods)
    {
        try
        {
            MethodInfo method =
                rule
                    .GetType()
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public
                    );

            if (method == null)
                continue;

            object value =
                method.Invoke(
                    rule,
                    null
                );

            if (value != null)
                return value;
        }
        catch
        {
        }
    }


    return null;
};


Func<object, string> RawValueText =
    delegate(object value)
{
    if (value == null)
        return null;

    ElementId elementId =
        value as ElementId;

    if (elementId != null)
    {
        return
            elementId.IntegerValue
                .ToString(
                    CultureInfo.InvariantCulture
                );
    }

    if (value is double)
    {
        return
            ((double)value)
                .ToString(
                    "R",
                    CultureInfo.InvariantCulture
                );
    }

    if (value is float)
    {
        return
            ((float)value)
                .ToString(
                    "R",
                    CultureInfo.InvariantCulture
                );
    }

    if (value is int)
    {
        return
            ((int)value)
                .ToString(
                    CultureInfo.InvariantCulture
                );
    }

    if (value is long)
    {
        return
            ((long)value)
                .ToString(
                    CultureInfo.InvariantCulture
                );
    }

    if (value is bool)
    {
        return
            ((bool)value)
            ? "true"
            : "false";
    }

    return Convert.ToString(
        value,
        CultureInfo.InvariantCulture
    );
};


// ============================================================================
// EPSILON
// ============================================================================

Func<FilterRule, double?> GetRuleEpsilon =
    delegate(FilterRule rule)
{
    if (rule == null)
        return null;

    try
    {
        object value =
            InvokeNoArg(
                rule,
                "GetEpsilon"
            );

        if (value is double)
            return (double)value;
    }
    catch
    {
    }

    return null;
};


// ============================================================================
// CATEGORY RULE IDS
// ============================================================================

Func<FilterRule, List<int>> GetCategoryRuleIds =
    delegate(FilterRule rule)
{
    List<int> result =
        new List<int>();

    if (rule == null)
        return result;


    // ------------------------------------------------------------------------
    // Try plural method/property first.
// ------------------------------------------------------------------------

    object idsObject =
        InvokeNoArg(
            rule,
            "GetCategoryIds"
        );

    if (idsObject == null)
    {
        idsObject =
            GetPublicProperty(
                rule,
                "CategoryIds"
            );
    }


    IEnumerable enumerable =
        idsObject as IEnumerable;

    if (enumerable != null)
    {
        foreach (object item in enumerable)
        {
            ElementId id =
                item as ElementId;

            if (id != null)
                result.Add(id.IntegerValue);
        }
    }


    // ------------------------------------------------------------------------
    // Try singular category ID.
// ------------------------------------------------------------------------

    if (result.Count == 0)
    {
        object idObject =
            InvokeNoArg(
                rule,
                "GetCategoryId"
            );

        if (idObject == null)
        {
            idObject =
                GetPublicProperty(
                    rule,
                    "CategoryId"
                );
        }

        ElementId id =
            idObject as ElementId;

        if (id != null)
            result.Add(id.IntegerValue);
    }


    return
        result
            .Distinct()
            .OrderBy(x => x)
            .ToList();
};


// ============================================================================
// OPERATOR NORMALISATION
// ============================================================================

Func<string, bool, string> NormalizeOperator =
    delegate(string evaluatorName, bool inverted)
{
    string op = null;

    switch (evaluatorName)
    {
        // --------------------------------------------------------------------
        // String
        // --------------------------------------------------------------------

        case "FilterStringEquals":
            op = "Equals";
            break;

        case "FilterStringContains":
            op = "Contains";
            break;

        case "FilterStringBeginsWith":
            op = "BeginsWith";
            break;

        case "FilterStringEndsWith":
            op = "EndsWith";
            break;

        case "FilterStringGreater":
            op = "GreaterThan";
            break;

        case "FilterStringGreaterOrEqual":
            op = "GreaterThanOrEqual";
            break;

        case "FilterStringLess":
            op = "LessThan";
            break;

        case "FilterStringLessOrEqual":
            op = "LessThanOrEqual";
            break;


        // --------------------------------------------------------------------
        // Numeric
        // --------------------------------------------------------------------

        case "FilterNumericEquals":
            op = "Equals";
            break;

        case "FilterNumericGreater":
            op = "GreaterThan";
            break;

        case "FilterNumericGreaterOrEqual":
            op = "GreaterThanOrEqual";
            break;

        case "FilterNumericLess":
            op = "LessThan";
            break;

        case "FilterNumericLessOrEqual":
            op = "LessThanOrEqual";
            break;

        default:
            op =
                !string.IsNullOrWhiteSpace(evaluatorName)
                ? evaluatorName
                : "Unknown";
            break;
    }


    if (!inverted)
        return op;


    // ------------------------------------------------------------------------
    // Normalise inverse wrapper to explicit operator.
    // ------------------------------------------------------------------------

    switch (op)
    {
        case "Equals":
            return "NotEquals";

        case "Contains":
            return "DoesNotContain";

        case "BeginsWith":
            return "DoesNotBeginWith";

        case "EndsWith":
            return "DoesNotEndWith";

        case "GreaterThan":
            return "LessThanOrEqual";

        case "GreaterThanOrEqual":
            return "LessThan";

        case "LessThan":
            return "GreaterThanOrEqual";

        case "LessThanOrEqual":
            return "GreaterThan";

        default:
            return "NOT(" + op + ")";
    }
};


// ============================================================================
// PORTABLE RULE SIGNATURE
//
// SINGLE SOURCE OF TRUTH FOR RULE MATCHING.
//
// Nothing else in this script should construct a parameter-rule signature.
// ============================================================================

Func<FilterRule, bool, string>
    BuildPortableRuleSignature = null;


BuildPortableRuleSignature =
    delegate(FilterRule rule, bool inheritedInverse)
{
    if (rule == null)
        return "NULL_RULE";


    Type type =
        rule.GetType();


    // ------------------------------------------------------------------------
    // Inverse wrapper
    //
    // Do not include the wrapper itself in the canonical signature.
    // Instead invert the terminal rule operator.
    // ------------------------------------------------------------------------

    if (rule is FilterInverseRule)
    {
        FilterRule inner =
            InvokeNoArg(
                rule,
                "GetInnerRule"
            ) as FilterRule;

        if (inner == null)
        {
            return
                "RULE|" +
                type.Name +
                "|INVERSE=TRUE|INNER=UNKNOWN";
        }

        return
            BuildPortableRuleSignature(
                inner,
                !inheritedInverse
            );
    }


    // ------------------------------------------------------------------------
    // Category rule
    // ------------------------------------------------------------------------

    if (type.Name == "FilterCategoryRule")
    {
        List<int> ids =
            GetCategoryRuleIds(rule);

        return
            "CATEGORY_RULE|CATEGORIES=" +
            string.Join(
                ",",
                ids.Select(
                    x => x.ToString(
                        CultureInfo.InvariantCulture
                    )
                )
            );
    }


    // ------------------------------------------------------------------------
    // Parameter identity
    // ------------------------------------------------------------------------

    ElementId parameterId =
        InvokeNoArg(
            rule,
            "GetRuleParameter"
        ) as ElementId;


    string parameterKey =
        GetParameterKey(parameterId);


    // ------------------------------------------------------------------------
    // Evaluator/operator
    // ------------------------------------------------------------------------

    object evaluator =
        InvokeNoArg(
            rule,
            "GetEvaluator"
        );


    string evaluatorName =
        evaluator != null
        ? evaluator.GetType().Name
        : null;


    string operatorName =
        NormalizeOperator(
            evaluatorName,
            inheritedInverse
        );


    // ------------------------------------------------------------------------
    // Special value-presence rules
    // ------------------------------------------------------------------------

    if (
        type.Name == "HasValueFilterRule" ||
        type.Name == "ParameterValuePresenceRule"
    )
    {
        operatorName =
            inheritedInverse
            ? "HasNoValue"
            : "HasValue";
    }
    else if (type.Name == "HasNoValueFilterRule")
    {
        operatorName =
            inheritedInverse
            ? "HasValue"
            : "HasNoValue";
    }


    // ------------------------------------------------------------------------
    // Value
    // ------------------------------------------------------------------------

    object value =
        GetRuleValue(rule);

    string rawValue =
        RawValueText(value);


    // ------------------------------------------------------------------------
    // Epsilon
    //
    // Include epsilon for a complete numeric rule identity when available.
    // ------------------------------------------------------------------------

    double? epsilon =
        GetRuleEpsilon(rule);


    string epsilonText =
        epsilon.HasValue
        ? epsilon.Value.ToString(
            "R",
            CultureInfo.InvariantCulture
        )
        : "";


    // ------------------------------------------------------------------------
    // CANONICAL PORTABLE SIGNATURE
    // ------------------------------------------------------------------------

    StringBuilder signature =
        new StringBuilder();

    signature.Append("RULE|");
    signature.Append(type.Name);

    signature.Append("|PARAM=");
    signature.Append(parameterKey ?? "UNKNOWN");

    signature.Append("|OP=");
    signature.Append(operatorName ?? "");

    signature.Append("|VALUE=");
    signature.Append(rawValue ?? "");

    if (epsilon.HasValue)
    {
        signature.Append("|EPSILON=");
        signature.Append(epsilonText);
    }

    return signature.ToString();
};


// ============================================================================
// RULE JSON
// ============================================================================

Func<FilterRule, bool, string>
    RuleToJson = null;


RuleToJson =
    delegate(FilterRule rule, bool inheritedInverse)
{
    if (rule == null)
        return "null";


    Type type =
        rule.GetType();


    // ------------------------------------------------------------------------
    // Inverse wrapper
    // ------------------------------------------------------------------------

    if (rule is FilterInverseRule)
    {
        FilterRule inner =
            InvokeNoArg(
                rule,
                "GetInnerRule"
            ) as FilterRule;


        StringBuilder inverseJson =
            new StringBuilder();

        inverseJson.Append("{");

        inverseJson.Append(
            "\"nodeType\":\"rule\","
        );

        inverseJson.Append(
            "\"ruleType\":" +
            JString(type.Name) +
            ","
        );

        inverseJson.Append(
            "\"isInverted\":true,"
        );

        inverseJson.Append(
            "\"innerRule\":" +
            (
                inner != null
                ? RuleToJson(
                    inner,
                    !inheritedInverse
                )
                : "null"
            ) +
            ","
        );

        inverseJson.Append(
            "\"signature\":" +
            JString(
                BuildPortableRuleSignature(
                    rule,
                    inheritedInverse
                )
            )
        );

        inverseJson.Append("}");

        return inverseJson.ToString();
    }


    // ------------------------------------------------------------------------
    // Category rule
    // ------------------------------------------------------------------------

    if (type.Name == "FilterCategoryRule")
    {
        List<int> categoryIds =
            GetCategoryRuleIds(rule);


        StringBuilder categoryJson =
            new StringBuilder();

        categoryJson.Append("{");

        categoryJson.Append(
            "\"nodeType\":\"rule\","
        );

        categoryJson.Append(
            "\"ruleType\":" +
            JString(type.Name) +
            ","
        );

        categoryJson.Append(
            "\"isInverted\":" +
            JBool(inheritedInverse) +
            ","
        );

        categoryJson.Append(
            "\"categoryIds\":["
        );

        for (
            int i = 0;
            i < categoryIds.Count;
            i++
        )
        {
            if (i > 0)
                categoryJson.Append(",");

            categoryJson.Append(
                categoryIds[i].ToString(
                    CultureInfo.InvariantCulture
                )
            );
        }

        categoryJson.Append("],");

        categoryJson.Append(
            "\"signature\":" +
            JString(
                BuildPortableRuleSignature(
                    rule,
                    inheritedInverse
                )
            )
        );

        categoryJson.Append("}");

        return categoryJson.ToString();
    }


    // ------------------------------------------------------------------------
    // Normal terminal rule
    // ------------------------------------------------------------------------

    ElementId parameterId =
        InvokeNoArg(
            rule,
            "GetRuleParameter"
        ) as ElementId;


    string parameterKey =
        GetParameterKey(parameterId);


    string parameterName =
        GetParameterName(parameterId);


    string parameterSource =
        GetParameterSource(parameterId);


    string parameterGuid =
        GetParameterGuid(parameterId);


    object evaluator =
        InvokeNoArg(
            rule,
            "GetEvaluator"
        );


    string evaluatorName =
        evaluator != null
        ? evaluator.GetType().Name
        : null;


    string operatorName =
        NormalizeOperator(
            evaluatorName,
            inheritedInverse
        );


    if (
        type.Name == "HasValueFilterRule" ||
        type.Name == "ParameterValuePresenceRule"
    )
    {
        operatorName =
            inheritedInverse
            ? "HasNoValue"
            : "HasValue";
    }
    else if (type.Name == "HasNoValueFilterRule")
    {
        operatorName =
            inheritedInverse
            ? "HasValue"
            : "HasNoValue";
    }


    object value =
        GetRuleValue(rule);


    string rawValue =
        RawValueText(value);


    double? epsilon =
        GetRuleEpsilon(rule);


    StringBuilder sb =
        new StringBuilder();

    sb.Append("{");


    sb.Append(
        "\"nodeType\":\"rule\","
    );


    sb.Append(
        "\"ruleType\":" +
        JString(type.Name) +
        ","
    );


    sb.Append(
        "\"isInverted\":" +
        JBool(inheritedInverse) +
        ","
    );


    // ------------------------------------------------------------------------
    // Raw Revit parameter ElementId
    //
    // Informational only.
    // ------------------------------------------------------------------------

    sb.Append(
        "\"parameterId\":" +
        (
            parameterId != null
            ? parameterId.IntegerValue
                .ToString(
                    CultureInfo.InvariantCulture
                )
            : "null"
        ) +
        ","
    );


    // ------------------------------------------------------------------------
    // Portable parameter identity
    // ------------------------------------------------------------------------

    sb.Append(
        "\"parameterKey\":" +
        JString(parameterKey) +
        ","
    );


    sb.Append(
        "\"parameterName\":" +
        JString(parameterName) +
        ","
    );


    sb.Append(
        "\"parameterSource\":" +
        JString(parameterSource) +
        ","
    );


    sb.Append(
        "\"parameterGuid\":" +
        JString(parameterGuid) +
        ","
    );


    sb.Append(
        "\"evaluator\":" +
        JString(evaluatorName) +
        ","
    );


    sb.Append(
        "\"operator\":" +
        JString(operatorName) +
        ","
    );


    sb.Append(
        "\"rawValue\":" +
        JString(rawValue) +
        ","
    );


    sb.Append(
        "\"epsilon\":" +
        (
            epsilon.HasValue
            ? epsilon.Value.ToString(
                "R",
                CultureInfo.InvariantCulture
            )
            : "null"
        ) +
        ","
    );


    // ------------------------------------------------------------------------
    // IMPORTANT:
    // Signature comes ONLY from BuildPortableRuleSignature.
// ------------------------------------------------------------------------

    sb.Append(
        "\"signature\":" +
        JString(
            BuildPortableRuleSignature(
                rule,
                inheritedInverse
            )
        )
    );


    sb.Append("}");


    return sb.ToString();
};


// ============================================================================
// TREE SIGNATURE
//
// Canonicalises logically equivalent AND/OR structures by:
// - recursively signing children
// - sorting children
// - collapsing single-child logical wrappers
//
// All terminal rule signatures call BuildPortableRuleSignature.
// ============================================================================

Func<ElementFilter, string>
    BuildTreeSignature = null;


BuildTreeSignature =
    delegate(ElementFilter elementFilter)
{
    if (elementFilter == null)
        return "NO_RULES";


    // ------------------------------------------------------------------------
    // Logical AND
    // ------------------------------------------------------------------------

    LogicalAndFilter andFilter =
        elementFilter as LogicalAndFilter;

    if (andFilter != null)
    {
        IList<ElementFilter> children =
            andFilter.GetFilters();


        List<string> signatures =
            children
                .Select(
                    x => BuildTreeSignature(x)
                )
                .Where(
                    x => !string.IsNullOrWhiteSpace(x)
                )
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal
                )
                .ToList();


        if (signatures.Count == 0)
            return "NO_RULES";

        if (signatures.Count == 1)
            return signatures[0];


        return
            "AND(" +
            string.Join(
                ",",
                signatures
            ) +
            ")";
    }


    // ------------------------------------------------------------------------
    // Logical OR
    // ------------------------------------------------------------------------

    LogicalOrFilter orFilter =
        elementFilter as LogicalOrFilter;

    if (orFilter != null)
    {
        IList<ElementFilter> children =
            orFilter.GetFilters();


        List<string> signatures =
            children
                .Select(
                    x => BuildTreeSignature(x)
                )
                .Where(
                    x => !string.IsNullOrWhiteSpace(x)
                )
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal
                )
                .ToList();


        if (signatures.Count == 0)
            return "NO_RULES";

        if (signatures.Count == 1)
            return signatures[0];


        return
            "OR(" +
            string.Join(
                ",",
                signatures
            ) +
            ")";
    }


    // ------------------------------------------------------------------------
    // ElementParameterFilter
    // ------------------------------------------------------------------------

    ElementParameterFilter parameterFilter =
        elementFilter as ElementParameterFilter;

    if (parameterFilter != null)
    {
        IList<FilterRule> rules =
            parameterFilter.GetRules();


        List<string> signatures =
            rules
                .Select(
                    x =>
                        BuildPortableRuleSignature(
                            x,
                            false
                        )
                )
                .Where(
                    x => !string.IsNullOrWhiteSpace(x)
                )
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal
                )
                .ToList();


        if (signatures.Count == 0)
            return "NO_RULES";

        if (signatures.Count == 1)
            return signatures[0];


        return
            "AND(" +
            string.Join(
                ",",
                signatures
            ) +
            ")";
    }


    // ------------------------------------------------------------------------
    // Unsupported element-filter type
    // ------------------------------------------------------------------------

    return
        "UNSUPPORTED_FILTER:" +
        elementFilter.GetType().Name;
};


// ============================================================================
// TREE JSON
//
// Raw-ish structure retained separately from canonical signatures.
// ============================================================================

Func<ElementFilter, string>
    ElementFilterToJson = null;


ElementFilterToJson =
    delegate(ElementFilter elementFilter)
{
    if (elementFilter == null)
        return "null";


    // ------------------------------------------------------------------------
    // Logical AND
    // ------------------------------------------------------------------------

    LogicalAndFilter andFilter =
        elementFilter as LogicalAndFilter;

    if (andFilter != null)
    {
        IList<ElementFilter> children =
            andFilter.GetFilters();


        StringBuilder sb =
            new StringBuilder();

        sb.Append("{");

        sb.Append(
            "\"nodeType\":\"logical\","
        );

        sb.Append(
            "\"logic\":\"AND\","
        );

        sb.Append(
            "\"children\":["
        );


        for (
            int i = 0;
            i < children.Count;
            i++
        )
        {
            if (i > 0)
                sb.Append(",");

            sb.Append(
                ElementFilterToJson(
                    children[i]
                )
            );
        }


        sb.Append("],");

        sb.Append(
            "\"signature\":" +
            JString(
                BuildTreeSignature(
                    elementFilter
                )
            )
        );

        sb.Append("}");

        return sb.ToString();
    }


    // ------------------------------------------------------------------------
    // Logical OR
    // ------------------------------------------------------------------------

    LogicalOrFilter orFilter =
        elementFilter as LogicalOrFilter;

    if (orFilter != null)
    {
        IList<ElementFilter> children =
            orFilter.GetFilters();


        StringBuilder sb =
            new StringBuilder();

        sb.Append("{");

        sb.Append(
            "\"nodeType\":\"logical\","
        );

        sb.Append(
            "\"logic\":\"OR\","
        );

        sb.Append(
            "\"children\":["
        );


        for (
            int i = 0;
            i < children.Count;
            i++
        )
        {
            if (i > 0)
                sb.Append(",");

            sb.Append(
                ElementFilterToJson(
                    children[i]
                )
            );
        }


        sb.Append("],");

        sb.Append(
            "\"signature\":" +
            JString(
                BuildTreeSignature(
                    elementFilter
                )
            )
        );

        sb.Append("}");

        return sb.ToString();
    }


    // ------------------------------------------------------------------------
    // ElementParameterFilter
    // ------------------------------------------------------------------------

    ElementParameterFilter parameterFilter =
        elementFilter as ElementParameterFilter;

    if (parameterFilter != null)
    {
        IList<FilterRule> rules =
            parameterFilter.GetRules();


        StringBuilder sb =
            new StringBuilder();

        sb.Append("{");

        sb.Append(
            "\"nodeType\":\"parameterFilter\","
        );

        sb.Append(
            "\"logic\":\"AND\","
        );

        sb.Append(
            "\"rules\":["
        );


        for (
            int i = 0;
            i < rules.Count;
            i++
        )
        {
            if (i > 0)
                sb.Append(",");

            sb.Append(
                RuleToJson(
                    rules[i],
                    false
                )
            );
        }


        sb.Append("],");

        sb.Append(
            "\"signature\":" +
            JString(
                BuildTreeSignature(
                    elementFilter
                )
            )
        );

        sb.Append("}");

        return sb.ToString();
    }


    // ------------------------------------------------------------------------
    // Unsupported element-filter type
    // ------------------------------------------------------------------------

    StringBuilder unsupported =
        new StringBuilder();

    unsupported.Append("{");

    unsupported.Append(
        "\"nodeType\":\"unsupported\","
    );

    unsupported.Append(
        "\"filterType\":" +
        JString(
            elementFilter
                .GetType()
                .Name
        ) +
        ","
    );

    unsupported.Append(
        "\"signature\":" +
        JString(
            BuildTreeSignature(
                elementFilter
            )
        )
    );

    unsupported.Append("}");

    return unsupported.ToString();
};


// ============================================================================
// COLLECT OFFICE PARAMETER FILTERS
// ============================================================================

List<ParameterFilterElement> filters =
    new FilteredElementCollector(doc)
        .OfClass(
            typeof(ParameterFilterElement)
        )
        .Cast<ParameterFilterElement>()
        .OrderBy(
            x => x.Name,
            StringComparer.OrdinalIgnoreCase
        )
        .ToList();


// ============================================================================
// BUILD ROOT JSON
// ============================================================================

StringBuilder json =
    new StringBuilder();


json.AppendLine("{");

json.AppendLine(
    "  \"schemaVersion\": \"1.1\","
);

json.AppendLine(
    "  \"libraryName\": " +
    JString(libraryName) +
    ","
);

json.AppendLine(
    "  \"libraryVersion\": " +
    JString(libraryVersion) +
    ","
);

json.AppendLine(
    "  \"generatedUtc\": " +
    JString(
        DateTime.UtcNow
            .ToString(
                "yyyy-MM-ddTHH:mm:ssZ",
                CultureInfo.InvariantCulture
            )
    ) +
    ","
);

json.AppendLine(
    "  \"revitVersion\": " +
    JString(
        doc.Application.VersionNumber
    ) +
    ","
);

json.AppendLine(
    "  \"revitBuild\": " +
    JString(
        doc.Application.VersionBuild
    ) +
    ","
);

json.AppendLine(
    "  \"sourceDocument\": " +
    JString(doc.Title) +
    ","
);

json.AppendLine(
    "  \"filterCount\": " +
    filters.Count.ToString(
        CultureInfo.InvariantCulture
    ) +
    ","
);

json.AppendLine(
    "  \"filters\": ["
);


// ============================================================================
// FILTER LOOP
// ============================================================================

for (
    int filterIndex = 0;
    filterIndex < filters.Count;
    filterIndex++
)
{
    ParameterFilterElement filter =
        filters[filterIndex];


    // ------------------------------------------------------------------------
    // Categories
    // ------------------------------------------------------------------------

    List<ElementId> categoryIds =
        filter
            .GetCategories()
            .OrderBy(
                x => x.IntegerValue
            )
            .ToList();


    string categorySignature =
        string.Join(
            ",",
            categoryIds
                .Select(
                    x =>
                        x.IntegerValue
                            .ToString(
                                CultureInfo.InvariantCulture
                            )
                )
        );


    // ------------------------------------------------------------------------
    // Element-filter tree
    // ------------------------------------------------------------------------

    ElementFilter elementFilter =
        null;

    try
    {
        elementFilter =
            filter.GetElementFilter();
    }
    catch
    {
        elementFilter =
            null;
    }


    // ------------------------------------------------------------------------
    // Canonical portable rules
    // ------------------------------------------------------------------------

    string ruleSignature =
        BuildTreeSignature(
            elementFilter
        );


    // ------------------------------------------------------------------------
    // Complete definition
    //
    // Exact Office match:
    // Filter Name + Categories + Complete Rules
    // ------------------------------------------------------------------------

    string definitionSignature =
        "NAME=" +
        (filter.Name ?? "") +
        "|CATEGORIES=" +
        categorySignature +
        "|RULES=" +
        ruleSignature;


    string definitionHash =
        Sha256(
            definitionSignature
        );


    // ------------------------------------------------------------------------
    // Filter JSON
    // ------------------------------------------------------------------------

    json.AppendLine("    {");


    json.AppendLine(
        "      \"name\": " +
        JString(filter.Name) +
        ","
    );


    json.AppendLine(
        "      \"sourceElementId\": " +
        filter.Id.IntegerValue
            .ToString(
                CultureInfo.InvariantCulture
            ) +
        ","
    );


    json.AppendLine(
        "      \"sourceUniqueId\": " +
        JString(filter.UniqueId) +
        ","
    );


    // ------------------------------------------------------------------------
    // Categories JSON
    // ------------------------------------------------------------------------

    json.AppendLine(
        "      \"categories\": ["
    );


    for (
        int categoryIndex = 0;
        categoryIndex < categoryIds.Count;
        categoryIndex++
    )
    {
        ElementId categoryId =
            categoryIds[categoryIndex];


        string categoryName =
            null;

        try
        {
            Category category =
                Category.GetCategory(
                    doc,
                    categoryId
                );

            if (category != null)
                categoryName =
                    category.Name;
        }
        catch
        {
        }


        json.Append(
            "        {" +
            "\"id\":" +
            categoryId.IntegerValue
                .ToString(
                    CultureInfo.InvariantCulture
                ) +
            "," +
            "\"name\":" +
            JString(categoryName) +
            "}"
        );


        if (
            categoryIndex <
            categoryIds.Count - 1
        )
        {
            json.Append(",");
        }


        json.AppendLine();
    }


    json.AppendLine(
        "      ],"
    );


    // ------------------------------------------------------------------------
    // Raw tree JSON
    // ------------------------------------------------------------------------

    json.AppendLine(
        "      \"ruleTree\": " +
        ElementFilterToJson(
            elementFilter
        ) +
        ","
    );


    // ------------------------------------------------------------------------
    // Canonical signatures
    // ------------------------------------------------------------------------

    json.AppendLine(
        "      \"categorySignature\": " +
        JString(
            categorySignature
        ) +
        ","
    );


    json.AppendLine(
        "      \"ruleSignature\": " +
        JString(
            ruleSignature
        ) +
        ","
    );


    json.AppendLine(
        "      \"definitionSignature\": " +
        JString(
            definitionSignature
        ) +
        ","
    );


    json.AppendLine(
        "      \"definitionHash\": " +
        JString(
            definitionHash
        )
    );


    json.Append("    }");


    if (
        filterIndex <
        filters.Count - 1
    )
    {
        json.Append(",");
    }


    json.AppendLine();
}


json.AppendLine("  ]");
json.AppendLine("}");


// ============================================================================
// HARD VALIDATION
//
// Prevents another apparently-successful export using legacy signatures.
// ============================================================================

string finalJson =
    json.ToString();


if (
    finalJson.IndexOf(
        "\"parameterKey\":\"GUID:",
        StringComparison.Ordinal
    ) >= 0
)
{
    if (
        finalJson.IndexOf(
            "PARAM=GUID:",
            StringComparison.Ordinal
        ) < 0
    )
    {
        TaskDialog.Show(
            "Office Filter Export",
            "EXPORT ABORTED.\n\n" +
            "Shared parameter GUID keys were found, but the " +
            "canonical signatures do not contain PARAM=GUID:.\n\n" +
            "No JSON file has been written."
        );

        return;
    }
}


if (
    finalJson.IndexOf(
        "\"parameterKey\":\"BIP:",
        StringComparison.Ordinal
    ) >= 0
)
{
    if (
        finalJson.IndexOf(
            "PARAM=BIP:",
            StringComparison.Ordinal
        ) < 0
    )
    {
        TaskDialog.Show(
            "Office Filter Export",
            "EXPORT ABORTED.\n\n" +
            "Built-in parameter keys were found, but the " +
            "canonical signatures do not contain PARAM=BIP:.\n\n" +
            "No JSON file has been written."
        );

        return;
    }
}


// ============================================================================
// SAVE
// ============================================================================

File.WriteAllText(
    outputPath,
    finalJson,
    new UTF8Encoding(false)
);


// ============================================================================
// SUCCESS
// ============================================================================

int portableGuidOccurrences = 0;
int portableBipOccurrences = 0;


int searchIndex = 0;

while (
    (
        searchIndex =
            finalJson.IndexOf(
                "PARAM=GUID:",
                searchIndex,
                StringComparison.Ordinal
            )
    ) >= 0
)
{
    portableGuidOccurrences++;
    searchIndex += 11;
}


searchIndex = 0;

while (
    (
        searchIndex =
            finalJson.IndexOf(
                "PARAM=BIP:",
                searchIndex,
                StringComparison.Ordinal
            )
    ) >= 0
)
{
    portableBipOccurrences++;
    searchIndex += 10;
}


TaskDialog.Show(
    "Office Filter Export",
    "Office filter library exported successfully.\n\n" +

    "Schema: 1.1\n" +
    "Filters: " +
    filters.Count.ToString(
        CultureInfo.InvariantCulture
    ) +
    "\n" +

    "PARAM=GUID occurrences: " +
    portableGuidOccurrences.ToString(
        CultureInfo.InvariantCulture
    ) +
    "\n" +

    "PARAM=BIP occurrences: " +
    portableBipOccurrences.ToString(
        CultureInfo.InvariantCulture
    ) +
    "\n\n" +

    outputPath
);