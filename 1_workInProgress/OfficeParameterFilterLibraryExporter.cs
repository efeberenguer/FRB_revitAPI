using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

// ============================================================
// REVIT 2024 - OFFICE PARAMETER FILTER LIBRARY EXPORTER
//
// READ ONLY
// No Transaction
// No model modification
//
// OUTPUT:
// Desktop\Office_Filter_Library_yyyy.MM.json
//
// Assumes ArchSmarter Launchpad exposes:
//     Document doc
// ============================================================


// ============================================================
// CONFIGURATION
// ============================================================

string libraryVersion = DateTime.Now.ToString("yyyy.MM");

string libraryName = "Office Parameter Filter Library";

string outputPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
    "Office_Filter_Library_" + libraryVersion + ".json"
);


// ============================================================
// BASIC HELPERS
// ============================================================

Func<string, string> JsonEscape = value =>
{
    if (value == null)
        return "";

    StringBuilder s = new StringBuilder();

    foreach (char c in value)
    {
        switch (c)
        {
            case '\\': s.Append("\\\\"); break;
            case '"':  s.Append("\\\""); break;
            case '\b': s.Append("\\b"); break;
            case '\f': s.Append("\\f"); break;
            case '\n': s.Append("\\n"); break;
            case '\r': s.Append("\\r"); break;
            case '\t': s.Append("\\t"); break;

            default:
                if (c < 32)
                    s.Append("\\u" + ((int)c).ToString("x4"));
                else
                    s.Append(c);
                break;
        }
    }

    return s.ToString();
};


Func<string, string> JString = value =>
{
    if (value == null)
        return "null";

    return "\"" + JsonEscape(value) + "\"";
};


Func<bool, string> JBool = value =>
{
    return value ? "true" : "false";
};


Func<string, string> Sha256 = text =>
{
    using (SHA256 sha = SHA256.Create())
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text ?? "");
        byte[] hash = sha.ComputeHash(bytes);

        StringBuilder sb = new StringBuilder();

        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
};


// ============================================================
// PARAMETER INFORMATION
// ============================================================

Func<ElementId, string> GetParameterName = parameterId =>
{
    if (parameterId == null)
        return null;

    try
    {
        Element e = doc.GetElement(parameterId);

        if (e != null && !string.IsNullOrWhiteSpace(e.Name))
            return e.Name;
    }
    catch
    {
    }

    try
    {
        if (parameterId.IntegerValue < 0)
        {
            BuiltInParameter bip =
                (BuiltInParameter)parameterId.IntegerValue;

            return LabelUtils.GetLabelFor(bip);
        }
    }
    catch
    {
    }

    return null;
};


Func<ElementId, string> GetParameterSource = parameterId =>
{
    if (parameterId == null)
        return "Other";

    if (parameterId.IntegerValue < 0)
        return "Built-in";

    try
    {
        Element e = doc.GetElement(parameterId);

        SharedParameterElement spe = e as SharedParameterElement;

        if (spe != null)
            return "Shared";

        ParameterElement pe = e as ParameterElement;

        if (pe != null)
            return "Project";
    }
    catch
    {
    }

    return "Other";
};


Func<ElementId, string> GetParameterGuid = parameterId =>
{
    try
    {
        SharedParameterElement spe =
            doc.GetElement(parameterId) as SharedParameterElement;

        if (spe != null)
            return spe.GuidValue.ToString();
    }
    catch
    {
    }

    return null;
};

// ============================================================
// PORTABLE PARAMETER IDENTITY
//
// IMPORTANT:
// Revit ElementId is retained as metadata, but is NOT used as
// the canonical identity for shared/project parameters.
//
// Built-in:
//     BIP:-1002001
//
// Shared:
//     GUID:f04b438c-dac0-4e90-853e-d87712fdcc27
//
// Project:
//     PROJECT:My Parameter
//
// Other:
//     NAME:My Parameter
// ============================================================

Func<ElementId, string> GetParameterKey = parameterId =>
{
    if (parameterId == null)
        return "UNKNOWN";


    // --------------------------------------------------------
    // Built-in parameter
    // Negative IDs are stable Revit built-in identities.
    // --------------------------------------------------------

    if (parameterId.IntegerValue < 0)
    {
        return
            "BIP:" +
            parameterId.IntegerValue.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            );
    }


    try
    {
        Element e = doc.GetElement(parameterId);


        // ----------------------------------------------------
        // Shared parameter
        // GUID is the portable identity.
        // ----------------------------------------------------

        SharedParameterElement spe =
            e as SharedParameterElement;

        if (spe != null)
        {
            return
                "GUID:" +
                spe.GuidValue
                    .ToString()
                    .ToLowerInvariant();
        }


        // ----------------------------------------------------
        // Project parameter
        //
        // Revit does not give these a shared GUID.
        // For V1 we therefore use the parameter name as the
        // portable fallback identity.
        //
        // Raw ElementId is still exported separately.
        // ----------------------------------------------------

        ParameterElement pe =
            e as ParameterElement;

        if (pe != null)
        {
            return
                "PROJECT:" +
                (pe.Name ?? "")
                    .Trim();
        }


        // ----------------------------------------------------
        // Other resolvable parameter element
        // ----------------------------------------------------

        if (e != null)
        {
            return
                "NAME:" +
                (e.Name ?? "")
                    .Trim();
        }
    }
    catch
    {
    }


    // --------------------------------------------------------
    // Last-resort fallback.
    //
    // This is deliberately marked LOCAL because it cannot be
    // assumed portable between RVT files.
    // --------------------------------------------------------

    return
        "LOCAL:" +
        parameterId.IntegerValue.ToString(
            System.Globalization.CultureInfo.InvariantCulture
        );
};

// ============================================================
// REFLECTION HELPERS
// ============================================================

Func<object, string, object> GetPropertyValue =
    (obj, propertyName) =>
{
    if (obj == null)
        return null;

    try
    {
        PropertyInfo p = obj.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance
        );

        if (p != null &&
            p.CanRead &&
            p.GetIndexParameters().Length == 0)
        {
            return p.GetValue(obj, null);
        }
    }
    catch
    {
    }

    return null;
};


Func<object, string, object> InvokeNoArg =
    (obj, methodName) =>
{
    if (obj == null)
        return null;

    try
    {
        MethodInfo m = obj.GetType().GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null
        );

        if (m != null)
            return m.Invoke(obj, null);
    }
    catch
    {
    }

    return null;
};


// ============================================================
// VALUE FORMATTING
// ============================================================

Func<object, string> RawValueText = value =>
{
    if (value == null)
        return null;

    ElementId eid = value as ElementId;

    if (eid != null)
        return eid.IntegerValue.ToString();

    double? d = value as double?;

    if (d.HasValue)
        return d.Value.ToString(
            "R",
            System.Globalization.CultureInfo.InvariantCulture
        );

    return Convert.ToString(
        value,
        System.Globalization.CultureInfo.InvariantCulture
    );
};


// ============================================================
// OPERATOR NORMALIZATION
// ============================================================

Func<string, bool, string> NormalizeOperator =
    (evaluatorName, inverted) =>
{
    string op = evaluatorName ?? "";

    if (op.StartsWith("Filter"))
        op = op.Substring("Filter".Length);

    op = op
        .Replace("String", "")
        .Replace("Numeric", "");

    switch (op)
    {
        case "Equals":
            return inverted ? "NotEquals" : "Equals";

        case "Contains":
            return inverted ? "DoesNotContain" : "Contains";

        case "BeginsWith":
            return inverted ? "DoesNotBeginWith" : "BeginsWith";

        case "EndsWith":
            return inverted ? "DoesNotEndWith" : "EndsWith";

        case "Greater":
            return inverted ? "LessThanOrEqual" : "GreaterThan";

        case "GreaterOrEqual":
            return inverted ? "LessThan" : "GreaterThanOrEqual";

        case "Less":
            return inverted ? "GreaterThanOrEqual" : "LessThan";

        case "LessOrEqual":
            return inverted ? "GreaterThan" : "LessThanOrEqual";

        default:
            return inverted
                ? "NOT(" + op + ")"
                : op;
    }
};


// ============================================================
// RULE VALUE EXTRACTION
// ============================================================

Func<FilterRule, object> GetRuleValue = rule =>
{
    if (rule == null)
        return null;

    string[] propertyNames =
    {
        "RuleString",
        "RuleValue"
    };

    foreach (string propertyName in propertyNames)
    {
        object v = GetPropertyValue(rule, propertyName);

        if (v != null)
            return v;
    }

    string[] methodNames =
    {
        "GetStringValue",
        "GetDoubleValue",
        "GetIntegerValue",
        "GetElementIdValue"
    };

    foreach (string methodName in methodNames)
    {
        object v = InvokeNoArg(rule, methodName);

        if (v != null)
            return v;
    }

    return null;
};


// ============================================================
// FILTER CATEGORY RULE SUPPORT
// ============================================================

Func<FilterRule, List<int>> GetCategoryRuleIds = rule =>
{
    List<int> result = new List<int>();

    if (rule == null)
        return result;

    object single = InvokeNoArg(rule, "GetCategoryId");

    ElementId singleId = single as ElementId;

    if (singleId != null)
        result.Add(singleId.IntegerValue);

    object multiple = InvokeNoArg(rule, "GetCategoryIds");

    IEnumerable enumerable = multiple as IEnumerable;

    if (enumerable != null)
    {
        foreach (object item in enumerable)
        {
            ElementId id = item as ElementId;

            if (id != null)
                result.Add(id.IntegerValue);
        }
    }

    return result
        .Distinct()
        .OrderBy(x => x)
        .ToList();
};


// ============================================================
// RULE SERIALIZATION
// ============================================================

Func<FilterRule, bool, string> BuildRuleSignature = null;
Func<FilterRule, bool, string> RuleToJson = null;


BuildRuleSignature = (rule, inheritedInverse) =>
{
    if (rule == null)
        return "NULL_RULE";

    Type type = rule.GetType();


    // --------------------------------------------------------
    // Inverse wrapper
    //
    // Normalize the inverse into the resulting operator.
    // --------------------------------------------------------

    if (rule is FilterInverseRule)
    {
        FilterRule inner =
            InvokeNoArg(
                rule,
                "GetInnerRule"
            ) as FilterRule;

        return BuildRuleSignature(
            inner,
            !inheritedInverse
        );
    }


    // --------------------------------------------------------
    // Category rule
    // --------------------------------------------------------

    if (type.Name == "FilterCategoryRule")
    {
        List<int> ids =
            GetCategoryRuleIds(rule);

        return
            "CATEGORY_RULE|" +
            string.Join(
                ",",
                ids.Select(
                    x => x.ToString(
                        System.Globalization.CultureInfo.InvariantCulture
                    )
                )
            );
    }


    // --------------------------------------------------------
    // Parameter
    // --------------------------------------------------------

    ElementId parameterId =
        InvokeNoArg(
            rule,
            "GetRuleParameter"
        ) as ElementId;


    string parameterKey =
        GetParameterKey(parameterId);


    // --------------------------------------------------------
    // Evaluator
    // --------------------------------------------------------

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


    // --------------------------------------------------------
    // Value
    // --------------------------------------------------------

    object value =
        GetRuleValue(rule);

    string rawValue =
        RawValueText(value);


    // --------------------------------------------------------
    // CANONICAL RULE SIGNATURE
    //
    // NOTE:
    // parameterId is intentionally NOT used here.
    // --------------------------------------------------------

    return string.Join(
        "|",
        new string[]
        {
            "RULE",
            type.Name,
            "PARAM=" + parameterKey,
            "OP=" + (operatorName ?? ""),
            "VALUE=" + (rawValue ?? "")
        }
    );
};BuildRuleSignature = (rule, inheritedInverse) =>
{
    if (rule == null)
        return "NULL_RULE";

    Type type = rule.GetType();

    // --------------------------------------------------------
    // Inverse wrapper
    // --------------------------------------------------------

    if (rule is FilterInverseRule)
    {
        FilterRule inner =
            InvokeNoArg(rule, "GetInnerRule") as FilterRule;

        return BuildRuleSignature(
            inner,
            !inheritedInverse
        );
    }


    // --------------------------------------------------------
    // Category rule
    // --------------------------------------------------------

    if (type.Name == "FilterCategoryRule")
    {
        List<int> ids = GetCategoryRuleIds(rule);

        return
            "CATEGORY_RULE|" +
            string.Join(",", ids.Select(x => x.ToString()));
    }


    // --------------------------------------------------------
    // Parameter ID
    // --------------------------------------------------------

    ElementId parameterId =
        InvokeNoArg(rule, "GetRuleParameter") as ElementId;


    // --------------------------------------------------------
    // Evaluator
    // --------------------------------------------------------

    object evaluator =
        InvokeNoArg(rule, "GetEvaluator");

    string evaluatorName =
        evaluator != null
        ? evaluator.GetType().Name
        : null;


    string operatorName =
        NormalizeOperator(
            evaluatorName,
            inheritedInverse
        );


    // --------------------------------------------------------
    // Value
    // --------------------------------------------------------

    object value =
        GetRuleValue(rule);

    string rawValue =
        RawValueText(value);


    return string.Join(
        "|",
        new string[]
        {
            "RULE",
            type.Name,
            parameterId != null
                ? parameterId.IntegerValue.ToString()
                : "",
            operatorName ?? "",
            rawValue ?? ""
        }
    );
};


RuleToJson = (rule, inheritedInverse) =>
{
    StringBuilder sb = new StringBuilder();

    if (rule == null)
        return "null";


    Type ruleType = rule.GetType();


    // --------------------------------------------------------
    // INVERSE RULE
    // --------------------------------------------------------

    if (rule is FilterInverseRule)
    {
        FilterRule innerRule =
            InvokeNoArg(rule, "GetInnerRule") as FilterRule;

        sb.Append("{");
        sb.Append("\"nodeType\":\"rule\",");
        sb.Append("\"ruleType\":\"FilterInverseRule\",");
        sb.Append("\"isInverted\":true,");
        sb.Append("\"innerRule\":");

        sb.Append(
            RuleToJson(
                innerRule,
                !inheritedInverse
            )
        );

        sb.Append("}");

        return sb.ToString();
    }


    // --------------------------------------------------------
    // NORMAL RULE
    // --------------------------------------------------------

    ElementId parameterId =
        InvokeNoArg(rule, "GetRuleParameter") as ElementId;

    string parameterName =
        GetParameterName(parameterId);

    string parameterSource =
        GetParameterSource(parameterId);

    string parameterGuid =
        GetParameterGuid(parameterId);
        
	string parameterKey =
	    GetParameterKey(parameterId);

    object evaluator =
        InvokeNoArg(rule, "GetEvaluator");

    string evaluatorName =
        evaluator != null
        ? evaluator.GetType().Name
        : null;


    string operatorName =
        NormalizeOperator(
            evaluatorName,
            inheritedInverse
        );


    object value =
        GetRuleValue(rule);

    string rawValue =
        RawValueText(value);


    sb.Append("{");

    sb.Append("\"nodeType\":\"rule\",");

    sb.Append(
        "\"ruleType\":" +
        JString(ruleType.Name) +
        ","
    );

    sb.Append(
        "\"isInverted\":" +
        JBool(inheritedInverse) +
        ","
    );


    // --------------------------------------------------------
    // Category rule
    // --------------------------------------------------------

    if (ruleType.Name == "FilterCategoryRule")
    {
        List<int> categoryIds =
            GetCategoryRuleIds(rule);

        sb.Append("\"categoryIds\":[");

        for (int i = 0; i < categoryIds.Count; i++)
        {
            if (i > 0)
                sb.Append(",");

            sb.Append(categoryIds[i]);
        }

        sb.Append("],");

        sb.Append(
            "\"signature\":" +
            JString(
                BuildRuleSignature(
                    rule,
                    inheritedInverse
                )
            )
        );

        sb.Append("}");

        return sb.ToString();
    }


    // --------------------------------------------------------
    // Parameter metadata
    // --------------------------------------------------------

    sb.Append(
        "\"parameterId\":" +
        (
            parameterId != null
            ? parameterId.IntegerValue.ToString()
            : "null"
        ) +
        ","
    );
    
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


    // --------------------------------------------------------
    // Evaluator + operator
    // --------------------------------------------------------

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


    // --------------------------------------------------------
    // Value
    // --------------------------------------------------------

    sb.Append(
        "\"rawValue\":" +
        JString(rawValue) +
        ","
    );


    // --------------------------------------------------------
    // Epsilon for double rules
    // --------------------------------------------------------

    object epsilon =
        GetPropertyValue(rule, "Epsilon");

    sb.Append(
        "\"epsilon\":" +
        JString(RawValueText(epsilon)) +
        ","
    );


    // --------------------------------------------------------
    // Rule signature
    // --------------------------------------------------------

    sb.Append(
        "\"signature\":" +
        JString(
            BuildRuleSignature(
                rule,
                inheritedInverse
            )
        )
    );

    sb.Append("}");

    return sb.ToString();
};


// ============================================================
// ELEMENT FILTER TREE SERIALIZATION
// ============================================================

Func<ElementFilter, string> BuildTreeSignature = null;
Func<ElementFilter, string> FilterTreeToJson = null;


BuildTreeSignature = filter =>
{
    if (filter == null)
        return "NO_RULES";


    // --------------------------------------------------------
    // Logical filters
    // --------------------------------------------------------

    ElementLogicalFilter logical =
        filter as ElementLogicalFilter;

    if (logical != null)
    {
        string logicType =
            filter is LogicalOrFilter
            ? "OR"
            : "AND";

        IList<ElementFilter> children =
            logical.GetFilters();

        List<string> childSignatures =
            children
            .Select(x => BuildTreeSignature(x))
            .ToList();


        // Logical AND / OR is commutative.
        // Sort children so order does not affect matching.
        childSignatures.Sort(
            StringComparer.Ordinal
        );


        // Remove a redundant single-child logical wrapper.
        if (childSignatures.Count == 1)
            return childSignatures[0];


        return
            logicType +
            "(" +
            string.Join(",", childSignatures) +
            ")";
    }


    // --------------------------------------------------------
    // ElementParameterFilter
    // --------------------------------------------------------

    ElementParameterFilter epf =
        filter as ElementParameterFilter;

    if (epf != null)
    {
        IList<FilterRule> rules =
            epf.GetRules();

        List<string> ruleSignatures =
            rules
            .Select(x => BuildRuleSignature(x, false))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();


        // Multiple rules inside ElementParameterFilter are AND.
        if (ruleSignatures.Count == 1)
            return ruleSignatures[0];


        return
            "AND(" +
            string.Join(",", ruleSignatures) +
            ")";
    }


    return
        "UNKNOWN_FILTER|" +
        filter.GetType().FullName;
};


FilterTreeToJson = filter =>
{
    if (filter == null)
    {
        return
            "{" +
            "\"nodeType\":\"none\"," +
            "\"logic\":\"NONE\"," +
            "\"children\":[]" +
            "}";
    }


    // --------------------------------------------------------
    // Logical AND / OR
    // --------------------------------------------------------

    ElementLogicalFilter logical =
        filter as ElementLogicalFilter;

    if (logical != null)
    {
        string logicType =
            filter is LogicalOrFilter
            ? "OR"
            : "AND";

        IList<ElementFilter> children =
            logical.GetFilters();

        StringBuilder sb = new StringBuilder();

        sb.Append("{");
        sb.Append("\"nodeType\":\"logical\",");
        sb.Append("\"logic\":" + JString(logicType) + ",");
        sb.Append("\"children\":[");

        for (int i = 0; i < children.Count; i++)
        {
            if (i > 0)
                sb.Append(",");

            sb.Append(
                FilterTreeToJson(
                    children[i]
                )
            );
        }

        sb.Append("],");

        sb.Append(
            "\"signature\":" +
            JString(
                BuildTreeSignature(filter)
            )
        );

        sb.Append("}");

        return sb.ToString();
    }


    // --------------------------------------------------------
    // ElementParameterFilter
    // --------------------------------------------------------

    ElementParameterFilter epf =
        filter as ElementParameterFilter;

    if (epf != null)
    {
        IList<FilterRule> rules =
            epf.GetRules();

        StringBuilder sb = new StringBuilder();

        sb.Append("{");
        sb.Append("\"nodeType\":\"parameterFilter\",");
        sb.Append("\"logic\":\"AND\",");
        sb.Append("\"rules\":[");

        for (int i = 0; i < rules.Count; i++)
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
                BuildTreeSignature(filter)
            )
        );

        sb.Append("}");

        return sb.ToString();
    }


    // --------------------------------------------------------
    // Unexpected filter type
    // --------------------------------------------------------

    return
        "{" +
        "\"nodeType\":\"unknown\"," +
        "\"runtimeType\":" +
        JString(filter.GetType().FullName) +
        "}";
};


// ============================================================
// MAIN EXPORT
// ============================================================

try
{
    List<ParameterFilterElement> filters =
        new FilteredElementCollector(doc)
        .OfClass(typeof(ParameterFilterElement))
        .Cast<ParameterFilterElement>()
        .OrderBy(x => x.Name)
        .ToList();


    StringBuilder json = new StringBuilder();

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
            DateTime.UtcNow.ToString(
                "yyyy-MM-ddTHH:mm:ssZ"
            )
        ) +
        ","
    );

    json.AppendLine(
        "  \"revitVersion\": " +
        JString(doc.Application.VersionNumber) +
        ","
    );

    json.AppendLine(
        "  \"revitBuild\": " +
        JString(doc.Application.VersionBuild) +
        ","
    );

    json.AppendLine(
        "  \"sourceDocument\": " +
        JString(doc.Title) +
        ","
    );

    json.AppendLine(
        "  \"filterCount\": " +
        filters.Count +
        ","
    );

    json.AppendLine(
        "  \"filters\": ["
    );


    for (int f = 0; f < filters.Count; f++)
    {
        ParameterFilterElement pfe =
            filters[f];


        // ====================================================
        // CATEGORIES
        // ====================================================

        List<ElementId> categoryIds =
            pfe.GetCategories()
            .OrderBy(x => x.IntegerValue)
            .ToList();


        List<string> categorySignatureParts =
            categoryIds
            .Select(
                x => x.IntegerValue.ToString()
            )
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();


        string categorySignature =
            string.Join(
                ",",
                categorySignatureParts
            );


        // ====================================================
        // FILTER TREE
        // ====================================================

        ElementFilter root =
            pfe.GetElementFilter();


        string treeSignature =
            BuildTreeSignature(root);


        // ====================================================
        // COMPLETE DEFINITION SIGNATURE
        //
        // Name is deliberately included because our agreed
        // office-match definition is:
        //
        // Name + Categories + Complete Rules
        // ====================================================

        string canonicalDefinition =
            "NAME=" +
            pfe.Name +
            "|CATEGORIES=" +
            categorySignature +
            "|RULES=" +
            treeSignature;


        string definitionHash =
            Sha256(canonicalDefinition);


        // ====================================================
        // WRITE FILTER
        // ====================================================

        json.AppendLine("    {");

        json.AppendLine(
            "      \"name\": " +
            JString(pfe.Name) +
            ","
        );

        json.AppendLine(
            "      \"sourceElementId\": " +
            pfe.Id.IntegerValue +
            ","
        );

        json.AppendLine(
            "      \"sourceUniqueId\": " +
            JString(pfe.UniqueId) +
            ","
        );


        // ----------------------------------------------------
        // Categories
        // ----------------------------------------------------

        json.AppendLine(
            "      \"categories\": ["
        );


        for (int c = 0; c < categoryIds.Count; c++)
        {
            ElementId categoryId =
                categoryIds[c];

            Category category = null;

            try
            {
                category =
                    Category.GetCategory(
                        doc,
                        categoryId
                    );
            }
            catch
            {
            }

            string categoryName =
                category != null
                ? category.Name
                : null;


            json.Append(
                "        {" +
                "\"id\":" +
                categoryId.IntegerValue +
                "," +
                "\"name\":" +
                JString(categoryName) +
                "}"
            );

            if (c < categoryIds.Count - 1)
                json.Append(",");

            json.AppendLine();
        }


        json.AppendLine(
            "      ],"
        );


        // ----------------------------------------------------
        // Rule tree
        // ----------------------------------------------------

        json.AppendLine(
            "      \"ruleTree\": " +
            FilterTreeToJson(root) +
            ","
        );


        // ----------------------------------------------------
        // Canonical signatures
        // ----------------------------------------------------

        json.AppendLine(
            "      \"categorySignature\": " +
            JString(categorySignature) +
            ","
        );

        json.AppendLine(
            "      \"ruleSignature\": " +
            JString(treeSignature) +
            ","
        );

        json.AppendLine(
            "      \"definitionSignature\": " +
            JString(canonicalDefinition) +
            ","
        );

        json.AppendLine(
            "      \"definitionHash\": " +
            JString(definitionHash)
        );


        json.Append("    }");

        if (f < filters.Count - 1)
            json.Append(",");

        json.AppendLine();
    }


    json.AppendLine(
        "  ]"
    );

    json.AppendLine(
        "}"
    );


    // ========================================================
    // SAVE
    // ========================================================

    File.WriteAllText(
        outputPath,
        json.ToString(),
        new UTF8Encoding(false)
    );


    TaskDialog.Show(
        "Office Filter Library",
        "Office filter library exported successfully.\n\n" +
        "Filters exported: " +
        filters.Count +
        "\n\n" +
        outputPath
    );
}
catch (Exception ex)
{
    TaskDialog.Show(
        "Office Filter Library - Error",
        ex.ToString()
    );
}