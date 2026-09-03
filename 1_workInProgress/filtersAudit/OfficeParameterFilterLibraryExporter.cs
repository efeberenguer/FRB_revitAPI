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
// READ ONLY
// No transaction required.
//
// Assumes Launchpad exposes:
//     Document doc
//
// Output:
//     Desktop\Office_Filter_Library_yyyy.MM.json
//
// Schema:
//     1.1
//
// Exact Office definition:
//     Filter Name + Categories + Complete Rules
//
// Portable parameter identities:
//     Built-in : BIP:<integer>
//     Shared   : GUID:<guid>
//     Project  : PROJECT:<name>
//     Other    : NAME:<name>
//     Fallback : LOCAL:<element id>
//
// Portable ElementId rule values:
//     Element type/material/etc. are represented semantically by
//     class/category/name rather than source-document ElementId.
//
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
// JSON HELPERS
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
// SHA256
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
// PARAMETER INFORMATION
// ============================================================================

Func<ElementId, string> GetParameterName =
    delegate(ElementId parameterId)
{
    if (parameterId == null)
        return null;

    if (parameterId.IntegerValue == -1)
        return null;


    if (parameterId.IntegerValue < 0)
    {
        try
        {
            BuiltInParameter bip =
                (BuiltInParameter)
                parameterId.IntegerValue;

            return LabelUtils.GetLabelFor(bip);
        }
        catch
        {
            return
                parameterId.IntegerValue
                    .ToString(
                        CultureInfo.InvariantCulture
                    );
        }
    }


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

    if (parameterId.IntegerValue == -1)
        return "Other";

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
// ============================================================================

Func<ElementId, string> GetParameterKey =
    delegate(ElementId parameterId)
{
    if (parameterId == null)
        return "UNKNOWN";

    if (parameterId.IntegerValue == -1)
        return "UNKNOWN";


    // Built-in parameter.
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


        // Shared parameter.
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


        // Project parameter.
        ParameterElement pe =
            element as ParameterElement;

        if (pe != null)
        {
            return
                "PROJECT:" +
                (pe.Name ?? "")
                    .Trim();
        }


        // Other identifiable element.
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


    return
        "LOCAL:" +
        parameterId.IntegerValue
            .ToString(
                CultureInfo.InvariantCulture
            );
};


// ============================================================================
// RULE VALUE EXTRACTION
//
// IMPORTANT FIX:
// Revit 2024 rules expose their test values primarily through properties:
//
//     FilterStringRule    -> RuleString
//     FilterIntegerRule   -> RuleValue
//     FilterDoubleRule    -> RuleValue
//     FilterElementIdRule -> RuleValue
//
// We try properties first, then retain method fallbacks.
// ============================================================================

Func<FilterRule, object> GetRuleValue =
    delegate(FilterRule rule)
{
    if (rule == null)
        return null;


    // ------------------------------------------------------------------------
    // 1. STRING RULE
    // ------------------------------------------------------------------------

    object value =
        GetPublicProperty(
            rule,
            "RuleString"
        );

    if (value != null)
        return value;


    // ------------------------------------------------------------------------
    // 2. NUMERIC / ELEMENT ID RULE
    // ------------------------------------------------------------------------

    value =
        GetPublicProperty(
            rule,
            "RuleValue"
        );

    if (value != null)
        return value;


    // ------------------------------------------------------------------------
    // 3. FALLBACK METHODS
    // ------------------------------------------------------------------------

    string[] methodNames =
    {
        "GetStringValue",
        "GetDoubleValue",
        "GetIntegerValue",
        "GetElementIdValue"
    };


    foreach (string methodName in methodNames)
    {
        value =
            InvokeNoArg(
                rule,
                methodName
            );

        if (value != null)
            return value;
    }


    return null;
};


// ============================================================================
// RAW VALUE TEXT
//
// This preserves the actual Revit rule value for diagnostics/export.
// ElementIds remain raw IDs here. The canonical signature uses a different
// portable value representation.
// ============================================================================

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


    return
        Convert.ToString(
            value,
            CultureInfo.InvariantCulture
        );
};


// ============================================================================
// PORTABLE ELEMENT-ID VALUE
//
// FilterElementIdRule values can be document-local ElementIds.
//
// Example:
//     Material ElementId 123456
//
// That ID is useless for comparison with another RVT, so the signature uses
// semantic identity instead:
//     ELEMENT:Material|CAT:Materials|NAME:Concrete
//
// Raw ID is still exported separately.
// ============================================================================

Func<ElementId, string> GetPortableElementIdValue =
    delegate(ElementId valueId)
{
    if (valueId == null)
        return "INVALID";


    int id =
        valueId.IntegerValue;


    if (id == -1)
        return "INVALID";


    // Some API values may legitimately be negative enumerated IDs.
    if (id < 0)
    {
        return
            "ID:" +
            id.ToString(
                CultureInfo.InvariantCulture
            );
    }


    try
    {
        Element element =
            doc.GetElement(valueId);

        if (element != null)
        {
            string className =
                element.GetType().Name;

            string categoryName =
                "";

            string categoryIdText =
                "";


            try
            {
                Category category =
                    element.Category;

                if (category != null)
                {
                    categoryName =
                        category.Name ?? "";

                    categoryIdText =
                        category.Id.IntegerValue
                            .ToString(
                                CultureInfo.InvariantCulture
                            );
                }
            }
            catch
            {
            }


            string elementName =
                "";

            try
            {
                elementName =
                    element.Name ?? "";
            }
            catch
            {
            }


            return
                "ELEMENT:" +
                className +
                "|CATID:" +
                categoryIdText +
                "|CAT:" +
                categoryName +
                "|NAME:" +
                elementName;
        }
    }
    catch
    {
    }


    // Explicitly mark unresolved IDs as document-local.
    return
        "LOCAL:" +
        id.ToString(
            CultureInfo.InvariantCulture
        );
};


// ============================================================================
// CANONICAL VALUE
// ============================================================================

Func<FilterRule, object, string> GetCanonicalRuleValue =
    delegate(FilterRule rule, object value)
{
    if (value == null)
        return "";


    ElementId elementId =
        value as ElementId;

    if (elementId != null)
    {
        return
            GetPortableElementIdValue(
                elementId
            );
    }


    return
        RawValueText(value) ?? "";
};


// ============================================================================
// EPSILON
// ============================================================================

Func<FilterRule, double?> GetRuleEpsilon =
    delegate(FilterRule rule)
{
    if (rule == null)
        return null;


    // Revit rule property if exposed.
    object value =
        GetPublicProperty(
            rule,
            "Epsilon"
        );


    if (value is double)
        return (double)value;


    // Reflection fallback.
    value =
        InvokeNoArg(
            rule,
            "GetEpsilon"
        );


    if (value is double)
        return (double)value;


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
            {
                result.Add(
                    id.IntegerValue
                );
            }
        }
    }


    // Try singular API shape.
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
        {
            result.Add(
                id.IntegerValue
            );
        }
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
    string op;


    switch (evaluatorName)
    {
        // String evaluators.

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


        // Numeric evaluators.

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
                !string.IsNullOrWhiteSpace(
                    evaluatorName
                )
                ? evaluatorName
                : "Unknown";

            break;
    }


    if (!inverted)
        return op;


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
// SINGLE SOURCE OF TRUTH.
//
// Every terminal rule signature must come through this function.
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
    // INVERSE RULE
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
    // CATEGORY RULE
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
                    x =>
                        x.ToString(
                            CultureInfo.InvariantCulture
                        )
                )
            );
    }


    // ------------------------------------------------------------------------
    // PARAMETER
    // ------------------------------------------------------------------------

    ElementId parameterId =
        InvokeNoArg(
            rule,
            "GetRuleParameter"
        ) as ElementId;


    string parameterKey =
        GetParameterKey(
            parameterId
        );


    // ------------------------------------------------------------------------
    // EVALUATOR / OPERATOR
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


    // Value-presence rules.
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


    if (
        type.Name ==
        "HasNoValueFilterRule"
    )
    {
        operatorName =
            inheritedInverse
            ? "HasValue"
            : "HasNoValue";
    }


    // ------------------------------------------------------------------------
    // VALUE
    // ------------------------------------------------------------------------

    object ruleValue =
        GetRuleValue(rule);


    string canonicalValue =
        GetCanonicalRuleValue(
            rule,
            ruleValue
        );


    // ------------------------------------------------------------------------
    // EPSILON
    // ------------------------------------------------------------------------

    double? epsilon =
        GetRuleEpsilon(rule);


    // ------------------------------------------------------------------------
    // SIGNATURE
    // ------------------------------------------------------------------------

    StringBuilder signature =
        new StringBuilder();


    signature.Append(
        "RULE|"
    );


    signature.Append(
        type.Name
    );


    signature.Append(
        "|PARAM="
    );


    signature.Append(
        parameterKey ?? "UNKNOWN"
    );


    signature.Append(
        "|OP="
    );


    signature.Append(
        operatorName ?? ""
    );


    signature.Append(
        "|VALUE="
    );


    signature.Append(
        canonicalValue ?? ""
    );


    if (epsilon.HasValue)
    {
        signature.Append(
            "|EPSILON="
        );


        signature.Append(
            epsilon.Value.ToString(
                "R",
                CultureInfo.InvariantCulture
            )
        );
    }


    return
        signature.ToString();
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
    // FILTER INVERSE RULE
    // ------------------------------------------------------------------------

    if (rule is FilterInverseRule)
    {
        FilterRule inner =
            InvokeNoArg(
                rule,
                "GetInnerRule"
            ) as FilterRule;


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
            "\"isInverted\":true,"
        );


        sb.Append(
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
    }


    // ------------------------------------------------------------------------
    // FILTER CATEGORY RULE
    // ------------------------------------------------------------------------

    if (
        type.Name ==
        "FilterCategoryRule"
    )
    {
        List<int> categoryIds =
            GetCategoryRuleIds(rule);


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


        sb.Append(
            "\"categoryIds\":["
        );


        for (
            int i = 0;
            i < categoryIds.Count;
            i++
        )
        {
            if (i > 0)
                sb.Append(",");


            sb.Append(
                categoryIds[i]
                    .ToString(
                        CultureInfo.InvariantCulture
                    )
            );
        }


        sb.Append("],");


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
    }


    // ------------------------------------------------------------------------
    // NORMAL TERMINAL RULE
    // ------------------------------------------------------------------------

    ElementId parameterId =
        InvokeNoArg(
            rule,
            "GetRuleParameter"
        ) as ElementId;


    string parameterKey =
        GetParameterKey(
            parameterId
        );


    string parameterName =
        GetParameterName(
            parameterId
        );


    string parameterSource =
        GetParameterSource(
            parameterId
        );


    string parameterGuid =
        GetParameterGuid(
            parameterId
        );


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


    if (
        type.Name ==
        "HasNoValueFilterRule"
    )
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


    string canonicalValue =
        GetCanonicalRuleValue(
            rule,
            value
        );


    double? epsilon =
        GetRuleEpsilon(rule);


    StringBuilder json =
        new StringBuilder();


    json.Append("{");


    json.Append(
        "\"nodeType\":\"rule\","
    );


    json.Append(
        "\"ruleType\":" +
        JString(type.Name) +
        ","
    );


    json.Append(
        "\"isInverted\":" +
        JBool(inheritedInverse) +
        ","
    );


    // Raw Revit parameter ID.
    json.Append(
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


    // Portable parameter identity.
    json.Append(
        "\"parameterKey\":" +
        JString(parameterKey) +
        ","
    );


    json.Append(
        "\"parameterName\":" +
        JString(parameterName) +
        ","
    );


    json.Append(
        "\"parameterSource\":" +
        JString(parameterSource) +
        ","
    );


    json.Append(
        "\"parameterGuid\":" +
        JString(parameterGuid) +
        ","
    );


    json.Append(
        "\"evaluator\":" +
        JString(evaluatorName) +
        ","
    );


    json.Append(
        "\"operator\":" +
        JString(operatorName) +
        ","
    );


    // Raw diagnostic representation.
    json.Append(
        "\"rawValue\":" +
        JString(rawValue) +
        ","
    );


    // Portable comparison representation.
    json.Append(
        "\"canonicalValue\":" +
        JString(canonicalValue) +
        ","
    );


    json.Append(
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


    json.Append(
        "\"signature\":" +
        JString(
            BuildPortableRuleSignature(
                rule,
                inheritedInverse
            )
        )
    );


    json.Append("}");


    return
        json.ToString();
};


// ============================================================================
// CANONICAL FILTER TREE SIGNATURE
// ============================================================================

Func<ElementFilter, string>
    BuildTreeSignature = null;


BuildTreeSignature =
    delegate(ElementFilter elementFilter)
{
    if (elementFilter == null)
        return "NO_RULES";


    // ------------------------------------------------------------------------
    // AND
    // ------------------------------------------------------------------------

    LogicalAndFilter andFilter =
        elementFilter
        as LogicalAndFilter;


    if (andFilter != null)
    {
        IList<ElementFilter> children =
            andFilter.GetFilters();


        List<string> signatures =
            children
                .Select(
                    x =>
                        BuildTreeSignature(x)
                )
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(x)
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
    // OR
    // ------------------------------------------------------------------------

    LogicalOrFilter orFilter =
        elementFilter
        as LogicalOrFilter;


    if (orFilter != null)
    {
        IList<ElementFilter> children =
            orFilter.GetFilters();


        List<string> signatures =
            children
                .Select(
                    x =>
                        BuildTreeSignature(x)
                )
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(x)
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
    // ELEMENT PARAMETER FILTER
    // ------------------------------------------------------------------------

    ElementParameterFilter parameterFilter =
        elementFilter
        as ElementParameterFilter;


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
                    x =>
                        !string.IsNullOrWhiteSpace(x)
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


    // Preserve unknown type rather than guessing.
    return
        "UNSUPPORTED_FILTER:" +
        elementFilter
            .GetType()
            .Name;
};


// ============================================================================
// RULE TREE JSON
// ============================================================================

Func<ElementFilter, string>
    ElementFilterToJson = null;


ElementFilterToJson =
    delegate(ElementFilter elementFilter)
{
    if (elementFilter == null)
        return "null";


    // ------------------------------------------------------------------------
    // AND
    // ------------------------------------------------------------------------

    LogicalAndFilter andFilter =
        elementFilter
        as LogicalAndFilter;


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
    // OR
    // ------------------------------------------------------------------------

    LogicalOrFilter orFilter =
        elementFilter
        as LogicalOrFilter;


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
    // PARAMETER FILTER
    // ------------------------------------------------------------------------

    ElementParameterFilter parameterFilter =
        elementFilter
        as ElementParameterFilter;


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
    // UNKNOWN ELEMENT FILTER
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


    return
        unsupported.ToString();
};


// ============================================================================
// COLLECT PARAMETER FILTERS
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
// BUILD JSON
// ============================================================================

StringBuilder output =
    new StringBuilder();


output.AppendLine("{");


output.AppendLine(
    "  \"schemaVersion\": \"1.1\","
);


output.AppendLine(
    "  \"libraryName\": " +
    JString(libraryName) +
    ","
);


output.AppendLine(
    "  \"libraryVersion\": " +
    JString(libraryVersion) +
    ","
);


output.AppendLine(
    "  \"generatedUtc\": " +
    JString(
        DateTime.UtcNow.ToString(
            "yyyy-MM-ddTHH:mm:ssZ",
            CultureInfo.InvariantCulture
        )
    ) +
    ","
);


output.AppendLine(
    "  \"revitVersion\": " +
    JString(
        doc.Application.VersionNumber
    ) +
    ","
);


output.AppendLine(
    "  \"revitBuild\": " +
    JString(
        doc.Application.VersionBuild
    ) +
    ","
);


output.AppendLine(
    "  \"sourceDocument\": " +
    JString(doc.Title) +
    ","
);


output.AppendLine(
    "  \"filterCount\": " +
    filters.Count.ToString(
        CultureInfo.InvariantCulture
    ) +
    ","
);


output.AppendLine(
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
    // CATEGORIES
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
            categoryIds.Select(
                x =>
                    x.IntegerValue
                        .ToString(
                            CultureInfo.InvariantCulture
                        )
            )
        );


    // ------------------------------------------------------------------------
    // FILTER TREE
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


    string ruleSignature =
        BuildTreeSignature(
            elementFilter
        );


    // ------------------------------------------------------------------------
    // COMPLETE DEFINITION
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
    // FILTER JSON
    // ------------------------------------------------------------------------

    output.AppendLine("    {");


    output.AppendLine(
        "      \"name\": " +
        JString(filter.Name) +
        ","
    );


    output.AppendLine(
        "      \"sourceElementId\": " +
        filter.Id.IntegerValue
            .ToString(
                CultureInfo.InvariantCulture
            ) +
        ","
    );


    output.AppendLine(
        "      \"sourceUniqueId\": " +
        JString(filter.UniqueId) +
        ","
    );


    // ------------------------------------------------------------------------
    // CATEGORY JSON
    // ------------------------------------------------------------------------

    output.AppendLine(
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


        output.Append(
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
            output.Append(",");
        }


        output.AppendLine();
    }


    output.AppendLine(
        "      ],"
    );


    // ------------------------------------------------------------------------
    // RAW STRUCTURE
    // ------------------------------------------------------------------------

    output.AppendLine(
        "      \"ruleTree\": " +
        ElementFilterToJson(
            elementFilter
        ) +
        ","
    );


    // ------------------------------------------------------------------------
    // CANONICAL SIGNATURES
    // ------------------------------------------------------------------------

    output.AppendLine(
        "      \"categorySignature\": " +
        JString(
            categorySignature
        ) +
        ","
    );


    output.AppendLine(
        "      \"ruleSignature\": " +
        JString(
            ruleSignature
        ) +
        ","
    );


    output.AppendLine(
        "      \"definitionSignature\": " +
        JString(
            definitionSignature
        ) +
        ","
    );


    output.AppendLine(
        "      \"definitionHash\": " +
        JString(
            definitionHash
        )
    );


    output.Append("    }");


    if (
        filterIndex <
        filters.Count - 1
    )
    {
        output.Append(",");
    }


    output.AppendLine();
}


output.AppendLine("  ]");
output.AppendLine("}");


// ============================================================================
// VALIDATION
// ============================================================================

string finalJson =
    output.ToString();


// ---------------------------------------------------------------------------
// Check portable shared parameter signatures.
// ---------------------------------------------------------------------------

if (
    finalJson.IndexOf(
        "\"parameterKey\":\"GUID:",
        StringComparison.Ordinal
    ) >= 0 &&
    finalJson.IndexOf(
        "PARAM=GUID:",
        StringComparison.Ordinal
    ) < 0
)
{
    TaskDialog.Show(
        "Office Filter Export",
        "EXPORT ABORTED.\n\n" +
        "Shared parameter GUIDs were extracted, but portable " +
        "GUID signatures were not generated.\n\n" +
        "No file has been written."
    );

    return;
}


// ---------------------------------------------------------------------------
// Check portable built-in signatures.
// ---------------------------------------------------------------------------

if (
    finalJson.IndexOf(
        "\"parameterKey\":\"BIP:",
        StringComparison.Ordinal
    ) >= 0 &&
    finalJson.IndexOf(
        "PARAM=BIP:",
        StringComparison.Ordinal
    ) < 0
)
{
    TaskDialog.Show(
        "Office Filter Export",
        "EXPORT ABORTED.\n\n" +
        "Built-in parameter IDs were extracted, but portable " +
        "BIP signatures were not generated.\n\n" +
        "No file has been written."
    );

    return;
}


// ---------------------------------------------------------------------------
// Detect the specific problem we just fixed:
// all ordinary rules having null raw values.
//
// Presence/value-only/category rules are excluded.
// ---------------------------------------------------------------------------

int terminalRuleCount = 0;
int rulesWithValues = 0;


foreach (
    ParameterFilterElement pfe
    in filters
)
{
    ElementFilter root = null;


    try
    {
        root =
            pfe.GetElementFilter();
    }
    catch
    {
    }


    if (root == null)
        continue;


    // Stack avoids needing another recursive delegate here.
    Stack<ElementFilter> stack =
        new Stack<ElementFilter>();


    stack.Push(root);


    while (stack.Count > 0)
    {
        ElementFilter current =
            stack.Pop();


        LogicalAndFilter andFilter =
            current as LogicalAndFilter;


        if (andFilter != null)
        {
            foreach (
                ElementFilter child
                in andFilter.GetFilters()
            )
            {
                stack.Push(child);
            }

            continue;
        }


        LogicalOrFilter orFilter =
            current as LogicalOrFilter;


        if (orFilter != null)
        {
            foreach (
                ElementFilter child
                in orFilter.GetFilters()
            )
            {
                stack.Push(child);
            }

            continue;
        }


        ElementParameterFilter epf =
            current as ElementParameterFilter;


        if (epf == null)
            continue;


        foreach (
            FilterRule originalRule
            in epf.GetRules()
        )
        {
            FilterRule terminalRule =
                originalRule;


            while (
                terminalRule
                is FilterInverseRule
            )
            {
                FilterRule inner =
                    InvokeNoArg(
                        terminalRule,
                        "GetInnerRule"
                    )
                    as FilterRule;


                if (inner == null)
                    break;


                terminalRule =
                    inner;
            }


            if (terminalRule == null)
                continue;


            string ruleType =
                terminalRule
                    .GetType()
                    .Name;


            if (
                ruleType ==
                "FilterCategoryRule"
            )
            {
                continue;
            }


            if (
                ruleType ==
                "HasValueFilterRule" ||
                ruleType ==
                "HasNoValueFilterRule" ||
                ruleType ==
                "ParameterValuePresenceRule"
            )
            {
                continue;
            }


            terminalRuleCount++;


            object value =
                GetRuleValue(
                    terminalRule
                );


            if (value != null)
                rulesWithValues++;
        }
    }
}


// If this library contains ordinary comparison rules but none
// produced values, abort rather than generating a bad office library.
if (
    terminalRuleCount > 0 &&
    rulesWithValues == 0
)
{
    TaskDialog.Show(
        "Office Filter Export",
        "EXPORT ABORTED.\n\n" +
        "The exporter found " +
        terminalRuleCount.ToString(
            CultureInfo.InvariantCulture
        ) +
        " comparison rules, but none of their rule values could " +
        "be extracted.\n\n" +
        "No JSON file has been written."
    );

    return;
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
// RESULT COUNTS
// ============================================================================

Func<string, string, int> CountOccurrences =
    delegate(string text, string token)
{
    if (
        string.IsNullOrEmpty(text) ||
        string.IsNullOrEmpty(token)
    )
    {
        return 0;
    }


    int count = 0;
    int index = 0;


    while (
        (
            index =
                text.IndexOf(
                    token,
                    index,
                    StringComparison.Ordinal
                )
        ) >= 0
    )
    {
        count++;

        index += token.Length;
    }


    return count;
};


int guidSignatureCount =
    CountOccurrences(
        finalJson,
        "PARAM=GUID:"
    );


int bipSignatureCount =
    CountOccurrences(
        finalJson,
        "PARAM=BIP:"
    );


int populatedValueCount =
    rulesWithValues;


// ============================================================================
// SUCCESS
// ============================================================================

TaskDialog.Show(
    "Office Filter Export",
    "Office filter library exported successfully.\n\n" +

    "Schema: 1.1\n" +

    "Filters: " +
    filters.Count.ToString(
        CultureInfo.InvariantCulture
    ) +
    "\n" +

    "Comparison rules: " +
    terminalRuleCount.ToString(
        CultureInfo.InvariantCulture
    ) +
    "\n" +

    "Rules with extracted values: " +
    populatedValueCount.ToString(
        CultureInfo.InvariantCulture
    ) +
    "\n" +

    "PARAM=GUID occurrences: " +
    guidSignatureCount.ToString(
        CultureInfo.InvariantCulture
    ) +
    "\n" +

    "PARAM=BIP occurrences: " +
    bipSignatureCount.ToString(
        CultureInfo.InvariantCulture
    ) +
    "\n\n" +

    outputPath
);
