using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

// ============================================================
// REVIT 2024 PARAMETER FILTER API DIAGNOSTIC
//
// READ ONLY:
// - No Transaction
// - No model modifications
//
// OUTPUT:
// Desktop\Revit_Filter_API_Diagnostic.txt
// ============================================================


// ------------------------------------------------------------
// Configuration
// ------------------------------------------------------------

string outputPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
    "Revit_Filter_API_Diagnostic.txt"
);


// ------------------------------------------------------------
// Counters
// ------------------------------------------------------------

Dictionary<string, int> elementFilterTypes =
    new Dictionary<string, int>();

Dictionary<string, int> ruleTypes =
    new Dictionary<string, int>();

HashSet<string> reflectionErrors =
    new HashSet<string>();


// ------------------------------------------------------------
// Helper: increment type counter
// ------------------------------------------------------------

Action<Dictionary<string, int>, string> CountType = (dictionary, typeName) =>
{
    if (dictionary.ContainsKey(typeName))
        dictionary[typeName]++;
    else
        dictionary[typeName] = 1;
};


// ------------------------------------------------------------
// Helper: safely convert ElementId
// ------------------------------------------------------------

Func<ElementId, string> ElementIdText = id =>
{
    if (id == null)
        return "<null>";

    return id.IntegerValue.ToString();
};


// ------------------------------------------------------------
// Helper: try to get parameter name
// ------------------------------------------------------------

Func<ElementId, string> GetParameterName = parameterId =>
{
    if (parameterId == null)
        return "<null>";

    try
    {
        Element parameterElement = doc.GetElement(parameterId);

        if (parameterElement != null)
            return parameterElement.Name;
    }
    catch
    {
    }

    // Built-in parameters normally have negative ElementIds.
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

    return "<unable to resolve>";
};


// ------------------------------------------------------------
// Helper: safe reflection value formatting
// ------------------------------------------------------------

Func<object, string> FormatValue = value =>
{
    if (value == null)
        return "<null>";

    ElementId eid = value as ElementId;

    if (eid != null)
        return "ElementId(" + eid.IntegerValue + ")";

    string text = value.ToString();

    if (string.IsNullOrWhiteSpace(text))
        return "<empty>";

    return text;
};


// ------------------------------------------------------------
// Recursive method: inspect a FilterRule
// ------------------------------------------------------------

Action<FilterRule, StringBuilder, string> DumpRule = null;

DumpRule = (rule, sb, indent) =>
{
    if (rule == null)
    {
        sb.AppendLine(indent + "<NULL RULE>");
        return;
    }

    Type ruleType = rule.GetType();

    CountType(ruleTypes, ruleType.Name);

    sb.AppendLine(indent + "RULE TYPE: " + ruleType.FullName);


    // --------------------------------------------------------
    // GetRuleParameter()
    // --------------------------------------------------------

    try
    {
        MethodInfo method =
            ruleType.GetMethod(
                "GetRuleParameter",
                BindingFlags.Public | BindingFlags.Instance
            );

        if (method != null)
        {
            object result = method.Invoke(rule, null);

            ElementId parameterId = result as ElementId;

            if (parameterId != null)
            {
                sb.AppendLine(
                    indent + "  ParameterId: " +
                    ElementIdText(parameterId)
                );

                sb.AppendLine(
                    indent + "  ParameterName: " +
                    GetParameterName(parameterId)
                );
            }
        }
    }
    catch (Exception ex)
    {
        reflectionErrors.Add(
            ruleType.Name +
            ".GetRuleParameter: " +
            ex.GetType().Name
        );
    }


    // --------------------------------------------------------
    // Public properties
    // --------------------------------------------------------

    PropertyInfo[] properties =
        ruleType.GetProperties(
            BindingFlags.Public | BindingFlags.Instance
        );

    foreach (PropertyInfo property in properties)
    {
        // Avoid generic CLR properties that aren't useful here.
        if (property.Name == "IsValidObject")
            continue;

        try
        {
            if (!property.CanRead)
                continue;

            if (property.GetIndexParameters().Length > 0)
                continue;

            object value = property.GetValue(rule, null);

            sb.AppendLine(
                indent +
                "  PROPERTY " +
                property.Name +
                ": " +
                FormatValue(value)
            );
        }
        catch (Exception ex)
        {
            reflectionErrors.Add(
                ruleType.Name +
                "." +
                property.Name +
                ": " +
                ex.GetType().Name
            );
        }
    }


    // --------------------------------------------------------
    // Interesting parameterless public methods
    //
    // Reflection is intentional here.
    // We want to discover exactly what the runtime Revit
    // rule types expose without making assumptions.
    // --------------------------------------------------------

    string[] interestingMethodNames =
    {
        "GetEvaluator",
        "GetRuleParameter",
        "GetStringValue",
        "GetDoubleValue",
        "GetIntegerValue",
        "GetElementIdValue",
        "GetInnerRule",
        "GetCategoryId",
        "GetCategoryIds"
    };

    foreach (string methodName in interestingMethodNames)
    {
        try
        {
            MethodInfo method =
                ruleType.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null
                );

            if (method == null)
                continue;

            // Already reported in detail above.
            if (methodName == "GetRuleParameter")
                continue;

            object value = method.Invoke(rule, null);

            if (value is System.Collections.IEnumerable &&
                !(value is string))
            {
                sb.AppendLine(
                    indent +
                    "  METHOD " +
                    methodName +
                    ":"
                );

                foreach (object item in
                    (System.Collections.IEnumerable)value)
                {
                    sb.AppendLine(
                        indent +
                        "    - " +
                        FormatValue(item)
                    );
                }
            }
            else
            {
                sb.AppendLine(
                    indent +
                    "  METHOD " +
                    methodName +
                    ": " +
                    FormatValue(value)
                );
            }

            // If this exposes an inner FilterRule, inspect it too.
            FilterRule innerRule = value as FilterRule;

            if (innerRule != null &&
                !object.ReferenceEquals(innerRule, rule))
            {
                sb.AppendLine(
                    indent +
                    "  INNER RULE:"
                );

                DumpRule(
                    innerRule,
                    sb,
                    indent + "    "
                );
            }
        }
        catch (Exception ex)
        {
            reflectionErrors.Add(
                ruleType.Name +
                "." +
                methodName +
                ": " +
                ex.GetType().Name
            );
        }
    }
};


// ------------------------------------------------------------
// Recursive method: inspect ElementFilter tree
// ------------------------------------------------------------

Action<ElementFilter, StringBuilder, string> DumpFilter = null;

DumpFilter = (filter, sb, indent) =>
{
    if (filter == null)
    {
        sb.AppendLine(indent + "<NO ELEMENT FILTER / NO RULES>");
        return;
    }

    Type filterType = filter.GetType();

    CountType(elementFilterTypes, filterType.Name);

    sb.AppendLine(
        indent +
        filterType.FullName
    );


    // --------------------------------------------------------
    // AND / OR filter
    // --------------------------------------------------------

    ElementLogicalFilter logicalFilter =
        filter as ElementLogicalFilter;

    if (logicalFilter != null)
    {
        IList<ElementFilter> children = null;

        try
        {
            children = logicalFilter.GetFilters();
        }
        catch (Exception ex)
        {
            sb.AppendLine(
                indent +
                "  ERROR reading child filters: " +
                ex.Message
            );

            return;
        }

        sb.AppendLine(
            indent +
            "  Children: " +
            children.Count
        );

        int childIndex = 1;

        foreach (ElementFilter child in children)
        {
            sb.AppendLine(
                indent +
                "  CHILD " +
                childIndex +
                ":"
            );

            DumpFilter(
                child,
                sb,
                indent + "    "
            );

            childIndex++;
        }

        return;
    }


    // --------------------------------------------------------
    // ElementParameterFilter
    // --------------------------------------------------------

    ElementParameterFilter parameterFilter =
        filter as ElementParameterFilter;

    if (parameterFilter != null)
    {
        IList<FilterRule> rules = null;

        try
        {
            rules = parameterFilter.GetRules();
        }
        catch (Exception ex)
        {
            sb.AppendLine(
                indent +
                "  ERROR reading rules: " +
                ex.Message
            );

            return;
        }

        sb.AppendLine(
            indent +
            "  Rules: " +
            rules.Count
        );

        int ruleIndex = 1;

        foreach (FilterRule rule in rules)
        {
            sb.AppendLine(
                indent +
                "  RULE " +
                ruleIndex +
                ":"
            );

            DumpRule(
                rule,
                sb,
                indent + "    "
            );

            ruleIndex++;
        }

        return;
    }


    // --------------------------------------------------------
    // Something unexpected
    // --------------------------------------------------------

    sb.AppendLine(
        indent +
        "  *** UNHANDLED ELEMENT FILTER TYPE ***"
    );
};


// ============================================================
// MAIN
// ============================================================

try
{
    StringBuilder sb = new StringBuilder();

    sb.AppendLine(
        "REVIT 2024 PARAMETER FILTER API DIAGNOSTIC"
    );

    sb.AppendLine(
        new string('=', 80)
    );

    sb.AppendLine(
        "Generated: " +
        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
    );

    sb.AppendLine(
        "Document Title: " +
        doc.Title
    );

    sb.AppendLine(
        "Document Path: " +
        (string.IsNullOrWhiteSpace(doc.PathName)
            ? "<unsaved / unavailable>"
            : doc.PathName)
    );

    sb.AppendLine(
        "Revit Build: " +
        doc.Application.VersionBuild
    );

    sb.AppendLine();


    // --------------------------------------------------------
    // Collect filters
    // --------------------------------------------------------

    List<ParameterFilterElement> filters =
        new FilteredElementCollector(doc)
        .OfClass(typeof(ParameterFilterElement))
        .Cast<ParameterFilterElement>()
        .OrderBy(x => x.Name)
        .ToList();


    sb.AppendLine(
        "PARAMETER FILTER COUNT: " +
        filters.Count
    );

    sb.AppendLine();


    int filterIndex = 1;


    foreach (ParameterFilterElement pfe in filters)
    {
        sb.AppendLine(
            new string('=', 80)
        );

        sb.AppendLine(
            "FILTER " +
            filterIndex +
            " OF " +
            filters.Count
        );

        sb.AppendLine(
            new string('=', 80)
        );

        sb.AppendLine(
            "Name: " +
            pfe.Name
        );

        sb.AppendLine(
            "ElementId: " +
            pfe.Id.IntegerValue
        );

        sb.AppendLine(
            "UniqueId: " +
            pfe.UniqueId
        );

        sb.AppendLine();


        // ----------------------------------------------------
        // Categories
        // ----------------------------------------------------

        sb.AppendLine("CATEGORIES");
        sb.AppendLine(new string('-', 40));

        ICollection<ElementId> categoryIds =
            pfe.GetCategories();

        sb.AppendLine(
            "Category Count: " +
            categoryIds.Count
        );

        foreach (ElementId categoryId in
            categoryIds.OrderBy(x => x.IntegerValue))
        {
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
                : "<unable to resolve>";

            sb.AppendLine(
                "  " +
                categoryName +
                " [" +
                categoryId.IntegerValue +
                "]"
            );
        }

        sb.AppendLine();


        // ----------------------------------------------------
        // Filter Tree
        // ----------------------------------------------------

        sb.AppendLine("FILTER TREE");
        sb.AppendLine(new string('-', 40));

        ElementFilter rootFilter = null;

        try
        {
            rootFilter =
                pfe.GetElementFilter();
        }
        catch (Exception ex)
        {
            sb.AppendLine(
                "ERROR calling GetElementFilter(): " +
                ex
            );
        }

        DumpFilter(
            rootFilter,
            sb,
            ""
        );

        sb.AppendLine();
        sb.AppendLine();

        filterIndex++;
    }


    // ========================================================
    // TYPE SUMMARY
    // ========================================================

    sb.AppendLine(
        new string('=', 80)
    );

    sb.AppendLine(
        "API TYPE SUMMARY"
    );

    sb.AppendLine(
        new string('=', 80)
    );

    sb.AppendLine();

    sb.AppendLine("ELEMENT FILTER TYPES");
    sb.AppendLine(new string('-', 40));

    foreach (KeyValuePair<string, int> item in
        elementFilterTypes
        .OrderByDescending(x => x.Value)
        .ThenBy(x => x.Key))
    {
        sb.AppendLine(
            item.Key.PadRight(40) +
            item.Value
        );
    }

    sb.AppendLine();

    sb.AppendLine("FILTER RULE TYPES");
    sb.AppendLine(new string('-', 40));

    foreach (KeyValuePair<string, int> item in
        ruleTypes
        .OrderByDescending(x => x.Value)
        .ThenBy(x => x.Key))
    {
        sb.AppendLine(
            item.Key.PadRight(40) +
            item.Value
        );
    }


    // ========================================================
    // Reflection issues
    // ========================================================

    sb.AppendLine();

    sb.AppendLine("REFLECTION / INSPECTION NOTES");
    sb.AppendLine(new string('-', 40));

    if (reflectionErrors.Count == 0)
    {
        sb.AppendLine("None.");
    }
    else
    {
        foreach (string error in reflectionErrors.OrderBy(x => x))
        {
            sb.AppendLine("  " + error);
        }
    }


    // --------------------------------------------------------
    // Write file
    // --------------------------------------------------------

    File.WriteAllText(
        outputPath,
        sb.ToString(),
        Encoding.UTF8
    );


    TaskDialog.Show(
        "Filter API Diagnostic",
        "Diagnostic completed.\n\n" +
        "Filters inspected: " +
        filters.Count +
        "\n\nOutput:\n" +
        outputPath
    );
}
catch (Exception ex)
{
    TaskDialog.Show(
        "Filter API Diagnostic - Error",
        ex.ToString()
    );
}