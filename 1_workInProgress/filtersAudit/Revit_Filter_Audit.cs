// ============================================================================
// REVIT FILTER AUDIT
// Revit 2024 / ArchSmarter Launchpad
//
// READ-ONLY AUDIT
// This script does not modify the Revit document.
//
// External inputs:
//     X:\20_Areas\Programming\Office_Filter_Library_2026.09.json
//
// Output:
//     <ProjectName>_Filter_Audit_yyyyMMdd_HHmmss.xlsx
//
// ============================================================================


// ============================================================================
// REVIT API
// ============================================================================

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;


// ============================================================================
// .NET
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

// ============================================================================
// FILTER AUDIT - FILE LOCATIONS
// ============================================================================

string programmingFolder =
    @"X:\20_Areas\Programming";

string officeLibraryPath =
    Path.Combine(
        programmingFolder,
        "Office_Filter_Library_2026.09.json"
    );

string diagnosticTxtPath =
    Path.Combine(
        programmingFolder,
        "Revit_Filter_API_Diagnostic.txt"
    );


// ============================================================================
// VALIDATE PROGRAMMING FOLDER
// ============================================================================

if (!Directory.Exists(programmingFolder))
{
    TaskDialog.Show(
        "Filter Audit",
        "Programming folder could not be found:\n\n" +
        programmingFolder
    );

    return;
}


// ============================================================================
// VALIDATE OFFICE FILTER LIBRARY
// ============================================================================

if (!File.Exists(officeLibraryPath))
{
    TaskDialog.Show(
        "Filter Audit",
        "Office filter library could not be found:\n\n" +
        officeLibraryPath +
        "\n\n" +
        "Expected file:\n" +
        "Office_Filter_Library_2026.09.json"
    );

    return;
}


// ============================================================================
// READ OFFICE FILTER LIBRARY
// ============================================================================

string officeLibraryJson;

try
{
    officeLibraryJson =
        File.ReadAllText(
            officeLibraryPath,
            Encoding.UTF8
        );
}
catch (Exception ex)
{
    TaskDialog.Show(
        "Filter Audit",
        "Could not read the Office Filter Library.\n\n" +
        officeLibraryPath +
        "\n\n" +
        ex.Message
    );

    return;
}


if (string.IsNullOrWhiteSpace(officeLibraryJson))
{
    TaskDialog.Show(
        "Filter Audit",
        "The Office Filter Library is empty:\n\n" +
        officeLibraryPath
    );

    return;
}


// ============================================================================
// BASIC LIBRARY VALIDATION
// ============================================================================

if (
    officeLibraryJson.IndexOf(
        "\"schemaVersion\": \"1.1\"",
        StringComparison.Ordinal
    ) < 0 &&
    officeLibraryJson.IndexOf(
        "\"schemaVersion\":\"1.1\"",
        StringComparison.Ordinal
    ) < 0
)
{
    TaskDialog.Show(
        "Filter Audit",
        "The Office Filter Library does not appear to use " +
        "the expected schema version 1.1.\n\n" +
        officeLibraryPath
    );

    return;
}


if (
    officeLibraryJson.IndexOf(
        "\"filters\"",
        StringComparison.Ordinal
    ) < 0
)
{
    TaskDialog.Show(
        "Filter Audit",
        "The Office Filter Library does not contain a filters collection.\n\n" +
        officeLibraryPath
    );

    return;
}

// ============================================================================
// AUDIT OUTPUT PATH
// ============================================================================

string projectName =
    doc.Title;

foreach (char invalidChar in Path.GetInvalidFileNameChars())
{
    projectName =
        projectName.Replace(
            invalidChar,
            '_'
        );
}

string auditOutputPath =
    Path.Combine(
        programmingFolder,
        projectName +
        "_Filter_Audit_" +
        DateTime.Now.ToString("yyyyMMdd_HHmmss") +
        ".xlsx"
    );
    
// ============================================================================
// BLOCK 3
// OFFICE FILTER LIBRARY - PARSE CANONICAL DEFINITIONS
//
// Requires:
//     using System;
//     using System.Collections.Generic;
//     using System.Text;
//     using System.Text.RegularExpressions;
//
// This intentionally avoids Newtonsoft.Json / System.Text.Json so the
// Launchpad script has no additional assembly dependency.
//
// The Office Library schema is locked at 1.1.
// We only need these canonical values for project comparison:
//
//     name
//     categorySignature
//     ruleSignature
//     definitionSignature
//     definitionHash
//
// ============================================================================


// ============================================================================
// JSON STRING UNESCAPER
// ============================================================================

Func<string, string> JsonUnescape =
    delegate(string value)
{
    if (value == null)
        return null;

    StringBuilder sb =
        new StringBuilder();

    for (int i = 0; i < value.Length; i++)
    {
        char c = value[i];

        if (
            c != '\\' ||
            i == value.Length - 1
        )
        {
            sb.Append(c);
            continue;
        }

        char next =
            value[++i];

        switch (next)
        {
            case '"':
                sb.Append('"');
                break;

            case '\\':
                sb.Append('\\');
                break;

            case '/':
                sb.Append('/');
                break;

            case 'b':
                sb.Append('\b');
                break;

            case 'f':
                sb.Append('\f');
                break;

            case 'n':
                sb.Append('\n');
                break;

            case 'r':
                sb.Append('\r');
                break;

            case 't':
                sb.Append('\t');
                break;

            case 'u':

                if (i + 4 < value.Length)
                {
                    string hex =
                        value.Substring(
                            i + 1,
                            4
                        );

                    int code;

                    if (
                        int.TryParse(
                            hex,
                            System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out code
                        )
                    )
                    {
                        sb.Append(
                            (char)code
                        );

                        i += 4;
                    }
                    else
                    {
                        sb.Append("\\u");
                    }
                }
                else
                {
                    sb.Append("\\u");
                }

                break;

            default:

                // Preserve unexpected escape rather than silently
                // destroying data.
                sb.Append('\\');
                sb.Append(next);
                break;
        }
    }

    return sb.ToString();
};


// ============================================================================
// EXTRACT ALL VALUES FOR A JSON STRING PROPERTY
//
// Example:
//     "definitionHash": "abc123"
//
// Returns every occurrence in file order.
// ============================================================================

Func<string, string, List<string>> ExtractJsonStringValues =
    delegate(
        string json,
        string propertyName
    )
{
    List<string> results =
        new List<string>();

    if (
        string.IsNullOrEmpty(json) ||
        string.IsNullOrEmpty(propertyName)
    )
    {
        return results;
    }

    string pattern =
        "\"" +
        Regex.Escape(propertyName) +
        "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"";

    MatchCollection matches =
        Regex.Matches(
            json,
            pattern,
            RegexOptions.Singleline
        );

    foreach (Match match in matches)
    {
        if (
            match.Success &&
            match.Groups.Count > 1
        )
        {
            results.Add(
                JsonUnescape(
                    match.Groups[1].Value
                )
            );
        }
    }

    return results;
};


// ============================================================================
// OFFICE LIBRARY VERSION
// ============================================================================

List<string> libraryVersions =
    ExtractJsonStringValues(
        officeLibraryJson,
        "libraryVersion"
    );

string officeLibraryVersion =
    libraryVersions.Count > 0
    ? libraryVersions[0]
    : "";


// ============================================================================
// EXTRACT CANONICAL FILTER DATA
// ============================================================================

List<string> officeNames =
    ExtractJsonStringValues(
        officeLibraryJson,
        "name"
    );

List<string> officeCategorySignatures =
    ExtractJsonStringValues(
        officeLibraryJson,
        "categorySignature"
    );

List<string> officeRuleSignatures =
    ExtractJsonStringValues(
        officeLibraryJson,
        "ruleSignature"
    );

List<string> officeDefinitionSignatures =
    ExtractJsonStringValues(
        officeLibraryJson,
        "definitionSignature"
    );

List<string> officeDefinitionHashes =
    ExtractJsonStringValues(
        officeLibraryJson,
        "definitionHash"
    );


// ============================================================================
// IMPORTANT:
//
// "name" appears not only at filter level, but also inside categories.
//
// Therefore we DO NOT pair officeNames by list index.
//
// Instead, the authoritative identity comes from:
//
//     definitionSignature
//
// which always starts:
//
//     NAME=<filter name>|CATEGORIES=...
//
// ============================================================================


// ============================================================================
// OFFICE FILTER LOOKUPS
//
// Key:
//
//     officeByName
//         Office filter name -> definition signature
//
//     officeHashByName
//         Office filter name -> definition hash
//
//     officeCategorySignatureByName
//         Office filter name -> canonical category signature
//
//     officeRuleSignatureByName
//         Office filter name -> canonical rule signature
//
//     officeNameByDefinitionHash
//         exact definition hash -> office filter name
//
// ============================================================================

Dictionary<string, string> officeByName =
    new Dictionary<string, string>(
        StringComparer.Ordinal
    );

Dictionary<string, string> officeHashByName =
    new Dictionary<string, string>(
        StringComparer.Ordinal
    );

Dictionary<string, string> officeCategorySignatureByName =
    new Dictionary<string, string>(
        StringComparer.Ordinal
    );

Dictionary<string, string> officeRuleSignatureByName =
    new Dictionary<string, string>(
        StringComparer.Ordinal
    );

Dictionary<string, string> officeNameByDefinitionHash =
    new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    );


// ============================================================================
// VALIDATE CANONICAL ARRAY COUNTS
// ============================================================================

int officeDefinitionCount =
    officeDefinitionSignatures.Count;


if (
    officeDefinitionCount == 0
)
{
    TaskDialog.Show(
        "Filter Audit",
        "No canonical filter definitions were found in the Office " +
        "Filter Library.\n\n" +
        officeLibraryPath
    );

    return;
}


if (
    officeDefinitionHashes.Count !=
    officeDefinitionCount
)
{
    TaskDialog.Show(
        "Filter Audit",
        "Office Filter Library validation failed.\n\n" +

        "Definition Signatures: " +
        officeDefinitionCount +
        "\n" +

        "Definition Hashes: " +
        officeDefinitionHashes.Count +
        "\n\n" +

        "The library appears incomplete or corrupt."
    );

    return;
}


if (
    officeCategorySignatures.Count !=
    officeDefinitionCount
)
{
    TaskDialog.Show(
        "Filter Audit",
        "Office Filter Library validation failed.\n\n" +

        "Definitions: " +
        officeDefinitionCount +
        "\n" +

        "Category Signatures: " +
        officeCategorySignatures.Count
    );

    return;
}


if (
    officeRuleSignatures.Count !=
    officeDefinitionCount
)
{
    TaskDialog.Show(
        "Filter Audit",
        "Office Filter Library validation failed.\n\n" +

        "Definitions: " +
        officeDefinitionCount +
        "\n" +

        "Rule Signatures: " +
        officeRuleSignatures.Count
    );

    return;
}


// ============================================================================
// PARSE FILTER NAME FROM DEFINITION SIGNATURE
//
// Format:
//
// NAME=<name>|CATEGORIES=<categories>|RULES=<rules>
//
// ============================================================================

Func<string, string> GetNameFromDefinitionSignature =
    delegate(string signature)
{
    if (
        string.IsNullOrEmpty(signature)
    )
    {
        return null;
    }

    const string prefix =
        "NAME=";

    const string separator =
        "|CATEGORIES=";

    if (
        !signature.StartsWith(
            prefix,
            StringComparison.Ordinal
        )
    )
    {
        return null;
    }

    int separatorIndex =
        signature.IndexOf(
            separator,
            StringComparison.Ordinal
        );

    if (
        separatorIndex <
        prefix.Length
    )
    {
        return null;
    }

    return
        signature.Substring(
            prefix.Length,
            separatorIndex - prefix.Length
        );
};


// ============================================================================
// BUILD OFFICE LOOKUPS
// ============================================================================

for (
    int i = 0;
    i < officeDefinitionCount;
    i++
)
{
    string definitionSignature =
        officeDefinitionSignatures[i];

    string definitionHash =
        officeDefinitionHashes[i];

    string categorySignature =
        officeCategorySignatures[i];

    string ruleSignature =
        officeRuleSignatures[i];

    string officeFilterName =
        GetNameFromDefinitionSignature(
            definitionSignature
        );


    if (
        string.IsNullOrWhiteSpace(
            officeFilterName
        )
    )
    {
        TaskDialog.Show(
            "Filter Audit",
            "Office Filter Library validation failed.\n\n" +
            "Could not determine the filter name from definition:\n\n" +
            definitionSignature
        );

        return;
    }


    if (
        officeByName.ContainsKey(
            officeFilterName
        )
    )
    {
        TaskDialog.Show(
            "Filter Audit",
            "Office Filter Library validation failed.\n\n" +
            "Duplicate office filter name:\n\n" +
            officeFilterName
        );

        return;
    }


    officeByName.Add(
        officeFilterName,
        definitionSignature
    );


    officeHashByName.Add(
        officeFilterName,
        definitionHash
    );


    officeCategorySignatureByName.Add(
        officeFilterName,
        categorySignature
    );


    officeRuleSignatureByName.Add(
        officeFilterName,
        ruleSignature
    );


    if (
        !officeNameByDefinitionHash.ContainsKey(
            definitionHash
        )
    )
    {
        officeNameByDefinitionHash.Add(
            definitionHash,
            officeFilterName
        );
    }
}


// ============================================================================
// FINAL LIBRARY COUNT CHECK
// ============================================================================

if (
    officeByName.Count !=
    officeDefinitionCount
)
{
    TaskDialog.Show(
        "Filter Audit",
        "Office Filter Library validation failed.\n\n" +

        "Definitions found: " +
        officeDefinitionCount +
        "\n" +

        "Office filters loaded: " +
        officeByName.Count
    );

    return;
}


// ============================================================================
// OPTIONAL EXPECTED COUNT CHECK
//
// The current approved library contains 16 filters.
//
// This is intentionally only informational rather than hard-coded as a
// permanent requirement, because the office library can grow in future.
// ============================================================================

bool officeLibraryCurrentExpectedCount =
    officeByName.Count == 16;


// ============================================================================
// BLOCK 3 COMPLETE
//
// Available to subsequent blocks:
//
//     officeLibraryVersion
//     officeByName
//     officeHashByName
//     officeCategorySignatureByName
//     officeRuleSignatureByName
//     officeNameByDefinitionHash
//
// ============================================================================

// ============================================================================
// BLOCK 4
// REVIT FILTER RULE PARSING + CANONICAL SIGNATURE ENGINE
//
// Requires:
//     using Autodesk.Revit.DB;
//     using System;
//     using System.Collections;
//     using System.Collections.Generic;
//     using System.Globalization;
//     using System.Linq;
//     using System.Reflection;
//     using System.Text;
//
// This block mirrors the canonical logic used by the Office JSON exporter.
// ============================================================================


// ============================================================================
// SAFE REFLECTION HELPERS
// ============================================================================

Func<object, string, object> GetPropertyValue =
    delegate(object obj, string propertyName)
{
    if (obj == null)
        return null;

    try
    {
        PropertyInfo property =
            obj.GetType().GetProperty(
                propertyName,
                BindingFlags.Public |
                BindingFlags.Instance
            );

        if (property == null)
            return null;

        return property.GetValue(
            obj,
            null
        );
    }
    catch
    {
        return null;
    }
};


Func<object, string, object> InvokeParameterlessMethod =
    delegate(object obj, string methodName)
{
    if (obj == null)
        return null;

    try
    {
        MethodInfo method =
            obj.GetType().GetMethod(
                methodName,
                BindingFlags.Public |
                BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null
            );

        if (method == null)
            return null;

        return method.Invoke(
            obj,
            null
        );
    }
    catch
    {
        return null;
    }
};


// ============================================================================
// ELEMENT ID INTEGER VALUE
// ============================================================================

Func<ElementId, long> GetElementIdInteger =
    delegate(ElementId id)
{
    if (id == null)
        return 0;

    try
    {
        object value =
            GetPropertyValue(
                id,
                "Value"
            );

        if (value != null)
        {
            return Convert.ToInt64(
                value,
                CultureInfo.InvariantCulture
            );
        }
    }
    catch
    {
    }

    try
    {
        return id.IntegerValue;
    }
    catch
    {
        return 0;
    }
};


// ============================================================================
// PARAMETER NAME
// ============================================================================

Func<ElementId, string> GetParameterName =
    delegate(ElementId parameterId)
{
    if (
        parameterId == null ||
        parameterId == ElementId.InvalidElementId
    )
    {
        return "";
    }

    long idValue =
        GetElementIdInteger(
            parameterId
        );

    // Built-in parameter
    if (idValue < 0)
    {
        try
        {
            BuiltInParameter bip =
                (BuiltInParameter)idValue;

            string label =
                LabelUtils.GetLabelFor(
                    bip
                );

            if (
                !string.IsNullOrWhiteSpace(
                    label
                )
            )
            {
                return label;
            }
        }
        catch
        {
        }

        return
            "BuiltInParameter " +
            idValue.ToString(
                CultureInfo.InvariantCulture
            );
    }


    Element parameterElement =
        doc.GetElement(
            parameterId
        );

    if (parameterElement != null)
    {
        return
            parameterElement.Name ?? "";
    }

    return "";
};


// ============================================================================
// PARAMETER SOURCE
// ============================================================================

Func<ElementId, string> GetParameterSource =
    delegate(ElementId parameterId)
{
    if (
        parameterId == null ||
        parameterId == ElementId.InvalidElementId
    )
    {
        return "Other";
    }

    long idValue =
        GetElementIdInteger(
            parameterId
        );

    if (idValue < 0)
        return "Built-in";

    Element element =
        doc.GetElement(
            parameterId
        );

    if (
        element is SharedParameterElement
    )
    {
        return "Shared";
    }

    if (
        element is ParameterElement
    )
    {
        return "Project";
    }

    return "Other";
};


// ============================================================================
// SHARED PARAMETER GUID
// ============================================================================

Func<ElementId, string> GetParameterGuid =
    delegate(ElementId parameterId)
{
    if (
        parameterId == null ||
        parameterId == ElementId.InvalidElementId
    )
    {
        return "";
    }

    try
    {
        SharedParameterElement shared =
            doc.GetElement(
                parameterId
            ) as SharedParameterElement;

        if (shared != null)
        {
            return
                shared.GuidValue
                    .ToString()
                    .ToLowerInvariant();
        }
    }
    catch
    {
    }

    return "";
};


// ============================================================================
// PARAMETER TYPE / DATA TYPE
// ============================================================================

Func<ElementId, string> GetParameterType =
    delegate(ElementId parameterId)
{
    if (
        parameterId == null ||
        parameterId == ElementId.InvalidElementId
    )
    {
        return "";
    }


    try
    {
        ParameterElement parameterElement =
            doc.GetElement(
                parameterId
            ) as ParameterElement;


        if (
            parameterElement != null &&
            parameterElement.GetDefinition() != null
        )
        {
            Definition definition =
                parameterElement.GetDefinition();


            // Revit 2022+ / Revit 2024 API:
            // Definition.GetDataType() returns a ForgeTypeId.

            MethodInfo getDataTypeMethod =
                definition
                    .GetType()
                    .GetMethod(
                        "GetDataType",
                        BindingFlags.Instance |
                        BindingFlags.Public,
                        null,
                        Type.EmptyTypes,
                        null
                    );


            if (
                getDataTypeMethod != null
            )
            {
                object dataType =
                    getDataTypeMethod.Invoke(
                        definition,
                        null
                    );


                if (dataType != null)
                {
                    // Try ForgeTypeId.TypeId first.
                    PropertyInfo typeIdProperty =
                        dataType
                            .GetType()
                            .GetProperty(
                                "TypeId",
                                BindingFlags.Instance |
                                BindingFlags.Public
                            );


                    if (
                        typeIdProperty != null
                    )
                    {
                        object typeIdValue =
                            typeIdProperty.GetValue(
                                dataType,
                                null
                            );


                        if (
                            typeIdValue != null
                        )
                        {
                            return
                                Convert.ToString(
                                    typeIdValue,
                                    CultureInfo.InvariantCulture
                                );
                        }
                    }


                    return
                        dataType.ToString();
                }
            }
        }
    }
    catch
    {
    }


    return "";
};


// ============================================================================
// STABLE PARAMETER KEY
//
// Built-in:
//     BIP:-1002001
//
// Shared:
//     GUID:xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
//
// Project:
//     PROJECT:<name>
//
// Other:
//     NAME:<name>
// ============================================================================

Func<ElementId, string> GetStableParameterKey =
    delegate(ElementId parameterId)
{
    if (
        parameterId == null ||
        parameterId == ElementId.InvalidElementId
    )
    {
        return "PARAMETER:UNKNOWN";
    }

    long idValue =
        GetElementIdInteger(
            parameterId
        );

    if (idValue < 0)
    {
        return
            "BIP:" +
            idValue.ToString(
                CultureInfo.InvariantCulture
            );
    }


    string guid =
        GetParameterGuid(
            parameterId
        );

    if (
        !string.IsNullOrWhiteSpace(
            guid
        )
    )
    {
        return
            "GUID:" +
            guid;
    }


    string parameterName =
        GetParameterName(
            parameterId
        );

    Element parameterElement =
        doc.GetElement(
            parameterId
        );


    if (
        parameterElement is ParameterElement
    )
    {
        return
            "PROJECT:" +
            parameterName;
    }


    if (
        !string.IsNullOrWhiteSpace(
            parameterName
        )
    )
    {
        return
            "NAME:" +
            parameterName;
    }


    return
        "LOCAL:" +
        idValue.ToString(
            CultureInfo.InvariantCulture
        );
};


// ============================================================================
// RULE PARAMETER ID
// ============================================================================

Func<FilterRule, ElementId> GetRuleParameterId =
    delegate(FilterRule rule)
{
    if (rule == null)
        return ElementId.InvalidElementId;

    try
    {
        object propertyValue =
            GetPropertyValue(
                rule,
                "RuleParameter"
            );

        ElementId id =
            propertyValue as ElementId;

        if (id != null)
            return id;
    }
    catch
    {
    }

    try
    {
        object methodValue =
            InvokeParameterlessMethod(
                rule,
                "GetRuleParameter"
            );

        ElementId id =
            methodValue as ElementId;

        if (id != null)
            return id;
    }
    catch
    {
    }

    return ElementId.InvalidElementId;
};


// ============================================================================
// RULE EVALUATOR NAME
// ============================================================================

Func<FilterRule, string> GetEvaluatorName =
    delegate(FilterRule rule)
{
    if (rule == null)
        return "";

    try
    {
        object evaluator =
            InvokeParameterlessMethod(
                rule,
                "GetEvaluator"
            );

        if (evaluator != null)
            return evaluator.GetType().Name;
    }
    catch
    {
    }

    return "";
};


// ============================================================================
// INNER RULE
// ============================================================================

Func<FilterRule, FilterRule> GetInnerRule =
    delegate(FilterRule rule)
{
    if (rule == null)
        return null;

    try
    {
        object value =
            InvokeParameterlessMethod(
                rule,
                "GetInnerRule"
            );

        return value as FilterRule;
    }
    catch
    {
        return null;
    }
};


// ============================================================================
// RAW RULE VALUE
// ============================================================================

Func<FilterRule, object> GetRuleValue =
    delegate(FilterRule rule)
{
    if (rule == null)
        return null;


    // Revit 2024 public properties first.
    object value =
        GetPropertyValue(
            rule,
            "RuleString"
        );

    if (value != null)
        return value;


    value =
        GetPropertyValue(
            rule,
            "RuleValue"
        );

    if (value != null)
        return value;


    // Fallback methods.
    string[] methods =
    {
        "GetStringValue",
        "GetDoubleValue",
        "GetIntegerValue",
        "GetElementIdValue"
    };


    foreach (
        string methodName
        in methods
    )
    {
        value =
            InvokeParameterlessMethod(
                rule,
                methodName
            );

        if (value != null)
            return value;
    }


    return null;
};


// ============================================================================
// EPSILON
// ============================================================================

Func<FilterRule, object> GetRuleEpsilon =
    delegate(FilterRule rule)
{
    if (rule == null)
        return null;

    object value =
        GetPropertyValue(
            rule,
            "Epsilon"
        );

    if (value != null)
        return value;

    return
        InvokeParameterlessMethod(
            rule,
            "GetEpsilon"
        );
};


// ============================================================================
// RAW VALUE TO STRING
// ============================================================================

Func<object, string> RawValueToString =
    delegate(object value)
{
    if (value == null)
        return "";

    ElementId elementId =
        value as ElementId;

    if (elementId != null)
    {
        return
            GetElementIdInteger(
                elementId
            )
            .ToString(
                CultureInfo.InvariantCulture
            );
    }

    IFormattable formattable =
        value as IFormattable;

    if (formattable != null)
    {
        return
            formattable.ToString(
                null,
                CultureInfo.InvariantCulture
            );
    }

    return value.ToString();
};


// ============================================================================
// CANONICAL ELEMENT-ID RULE VALUE
//
// Avoid document-local ElementId values where possible.
// ============================================================================

Func<ElementId, string> GetCanonicalElementIdValue =
    delegate(ElementId elementId)
{
    if (
        elementId == null ||
        elementId == ElementId.InvalidElementId
    )
    {
        return "ELEMENT:INVALID";
    }

    Element element =
        doc.GetElement(
            elementId
        );

    if (element == null)
    {
        return
            "LOCAL:" +
            GetElementIdInteger(
                elementId
            )
            .ToString(
                CultureInfo.InvariantCulture
            );
    }


    string className =
        element.GetType().Name;

    string categoryName =
        "";

    string categoryId =
        "";


    try
    {
        if (
            element.Category != null
        )
        {
            categoryName =
                element.Category.Name ?? "";

            categoryId =
                GetElementIdInteger(
                    element.Category.Id
                )
                .ToString(
                    CultureInfo.InvariantCulture
                );
        }
    }
    catch
    {
    }


    string elementName =
        element.Name ?? "";


    return
        "ELEMENT:" +
        className +
        "|CATID:" +
        categoryId +
        "|CAT:" +
        categoryName +
        "|NAME:" +
        elementName;
};


// ============================================================================
// CANONICAL RULE VALUE
// ============================================================================

Func<FilterRule, object, string> GetCanonicalRuleValue =
    delegate(
        FilterRule rule,
        object rawValue
    )
{
    if (rawValue == null)
        return "";

    ElementId elementId =
        rawValue as ElementId;

    if (elementId != null)
    {
        return
            GetCanonicalElementIdValue(
                elementId
            );
    }

    IFormattable formattable =
        rawValue as IFormattable;

    if (formattable != null)
    {
        return
            formattable.ToString(
                null,
                CultureInfo.InvariantCulture
            );
    }

    return rawValue.ToString();
};


// ============================================================================
// NORMALIZED OPERATOR
// ============================================================================

Func<string, bool, string> GetNormalizedOperator =
    delegate(
        string evaluatorName,
        bool isInverted
    )
{
    string op =
        evaluatorName ?? "";


    if (
        op.StartsWith(
            "Filter",
            StringComparison.Ordinal
        )
    )
    {
        op =
            op.Substring(
                "Filter".Length
            );
    }


    op =
        op.Replace(
            "String",
            ""
        );

    op =
        op.Replace(
            "Numeric",
            ""
        );


    if (!isInverted)
    {
        switch (op)
        {
            case "Equals":
                return "Equals";

            case "Contains":
                return "Contains";

            case "BeginsWith":
                return "BeginsWith";

            case "EndsWith":
                return "EndsWith";

            case "Greater":
                return "GreaterThan";

            case "GreaterOrEqual":
                return "GreaterThanOrEqual";

            case "Less":
                return "LessThan";

            case "LessOrEqual":
                return "LessThanOrEqual";
        }

        return
            string.IsNullOrWhiteSpace(op)
            ? "Unknown"
            : op;
    }


    // Inverted evaluator.
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

        case "Greater":
            return "LessThanOrEqual";

        case "GreaterOrEqual":
            return "LessThan";

        case "Less":
            return "GreaterThanOrEqual";

        case "LessOrEqual":
            return "GreaterThan";
    }


    return
        "NOT(" +
        (
            string.IsNullOrWhiteSpace(op)
            ? "Unknown"
            : op
        ) +
        ")";
};


// ============================================================================
// CATEGORY RULE IDS
// ============================================================================

Func<FilterRule, List<ElementId>> GetCategoryRuleIds =
    delegate(FilterRule rule)
{
    List<ElementId> ids =
        new List<ElementId>();

    if (rule == null)
        return ids;


    string[] methodNames =
    {
        "GetCategoryId",
        "GetCategoryIds"
    };


    foreach (
        string methodName
        in methodNames
    )
    {
        try
        {
            object value =
                InvokeParameterlessMethod(
                    rule,
                    methodName
                );

            ElementId single =
                value as ElementId;

            if (single != null)
            {
                ids.Add(single);
                continue;
            }


            IEnumerable enumerable =
                value as IEnumerable;

            if (enumerable != null)
            {
                foreach (
                    object item
                    in enumerable
                )
                {
                    ElementId id =
                        item as ElementId;

                    if (id != null)
                        ids.Add(id);
                }
            }
        }
        catch
        {
        }
    }


    return
        ids
        .GroupBy(
            x =>
                GetElementIdInteger(x)
        )
        .Select(
            x => x.First()
        )
        .OrderBy(
            x =>
                GetElementIdInteger(x)
        )
        .ToList();
};


// ============================================================================
// RULE CANONICAL SIGNATURE
//
// Handles:
//     FilterStringRule
//     FilterIntegerRule
//     FilterDoubleRule
//     FilterElementIdRule
//     FilterCategoryRule
//     FilterInverseRule
//     unknown rules
// ============================================================================

Func<FilterRule, bool, string> BuildRuleSignature = null;

BuildRuleSignature =
    delegate(
        FilterRule rule,
        bool inheritedInverse
    )
{
    if (rule == null)
        return "RULE|NULL";


    string ruleType =
        rule.GetType().Name;


    // ------------------------------------------------------------------------
    // INVERSE WRAPPER
    // ------------------------------------------------------------------------

    if (
        ruleType ==
        "FilterInverseRule"
    )
    {
        FilterRule innerRule =
            GetInnerRule(
                rule
            );

        if (innerRule == null)
        {
            return
                "RULE|FilterInverseRule|INNER=UNKNOWN";
        }

        return
            BuildRuleSignature(
                innerRule,
                !inheritedInverse
            );
    }


    // ------------------------------------------------------------------------
    // CATEGORY RULE
    // ------------------------------------------------------------------------

    if (
        ruleType ==
        "FilterCategoryRule"
    )
    {
        List<ElementId> categoryIds =
            GetCategoryRuleIds(
                rule
            );

        string categories =
            string.Join(
                ",",
                categoryIds
                    .Select(
                        x =>
                            GetElementIdInteger(x)
                            .ToString(
                                CultureInfo.InvariantCulture
                            )
                    )
            );

        return
            "RULE|" +
            ruleType +
            "|CATEGORIES=" +
            categories +
            (
                inheritedInverse
                ? "|INVERTED=TRUE"
                : ""
            );
    }


    // ------------------------------------------------------------------------
    // STANDARD PARAMETER RULE
    // ------------------------------------------------------------------------

    ElementId parameterId =
        GetRuleParameterId(
            rule
        );

    string parameterKey =
        GetStableParameterKey(
            parameterId
        );

    string evaluator =
        GetEvaluatorName(
            rule
        );

    string normalizedOperator =
        GetNormalizedOperator(
            evaluator,
            inheritedInverse
        );

    object rawValue =
        GetRuleValue(
            rule
        );

    string canonicalValue =
        GetCanonicalRuleValue(
            rule,
            rawValue
        );


    StringBuilder signature =
        new StringBuilder();

    signature.Append(
        "RULE|"
    );

    signature.Append(
        ruleType
    );

    signature.Append(
        "|PARAM="
    );

    signature.Append(
        parameterKey
    );

    signature.Append(
        "|OP="
    );

    signature.Append(
        normalizedOperator
    );


    if (rawValue != null)
    {
        signature.Append(
            "|VALUE="
        );

        signature.Append(
            canonicalValue
        );
    }


    object epsilon =
        GetRuleEpsilon(
            rule
        );

    if (epsilon != null)
    {
        signature.Append(
            "|EPSILON="
        );

        signature.Append(
            RawValueToString(
                epsilon
            )
        );
    }


    return
        signature.ToString();
};


// ============================================================================
// GET RULES FROM ElementParameterFilter
// ============================================================================

Func<ElementParameterFilter, List<FilterRule>> GetParameterFilterRules =
    delegate(ElementParameterFilter filter)
{
    List<FilterRule> results =
        new List<FilterRule>();

    if (filter == null)
        return results;

    try
    {
        IList<FilterRule> rules =
            filter.GetRules();

        if (rules != null)
        {
            results.AddRange(
                rules
            );
        }
    }
    catch
    {
    }

    return results;
};


// ============================================================================
// CANONICAL ELEMENT FILTER TREE SIGNATURE
//
// AND / OR children are sorted deliberately.
// This means equivalent sibling ordering generates the same signature.
//
// Single-child logical wrappers are collapsed.
// ============================================================================

Func<ElementFilter, string> BuildElementFilterSignature = null;

BuildElementFilterSignature =
    delegate(ElementFilter elementFilter)
{
    if (elementFilter == null)
        return "<NO RULES>";


    // ------------------------------------------------------------------------
    // PARAMETER FILTER
    // ------------------------------------------------------------------------

    ElementParameterFilter parameterFilter =
        elementFilter as ElementParameterFilter;

    if (parameterFilter != null)
    {
        List<string> ruleSignatures =
            GetParameterFilterRules(
                parameterFilter
            )
            .Select(
                rule =>
                    BuildRuleSignature(
                        rule,
                        false
                    )
            )
            .OrderBy(
                x => x,
                StringComparer.Ordinal
            )
            .ToList();


        if (
            ruleSignatures.Count == 0
        )
        {
            return "<NO RULES>";
        }


        if (
            ruleSignatures.Count == 1
        )
        {
            return
                ruleSignatures[0];
        }


        return
            "AND(" +
            string.Join(
                ",",
                ruleSignatures
            ) +
            ")";
    }


    // ------------------------------------------------------------------------
    // LOGICAL AND
    // ------------------------------------------------------------------------

    LogicalAndFilter andFilter =
        elementFilter as LogicalAndFilter;

    if (andFilter != null)
    {
        List<string> childSignatures =
            andFilter
                .GetFilters()
                .Select(
                    child =>
                        BuildElementFilterSignature(
                            child
                        )
                )
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal
                )
                .ToList();


        if (
            childSignatures.Count == 0
        )
        {
            return "<NO RULES>";
        }


        if (
            childSignatures.Count == 1
        )
        {
            return
                childSignatures[0];
        }


        return
            "AND(" +
            string.Join(
                ",",
                childSignatures
            ) +
            ")";
    }


    // ------------------------------------------------------------------------
    // LOGICAL OR
    // ------------------------------------------------------------------------

    LogicalOrFilter orFilter =
        elementFilter as LogicalOrFilter;

    if (orFilter != null)
    {
        List<string> childSignatures =
            orFilter
                .GetFilters()
                .Select(
                    child =>
                        BuildElementFilterSignature(
                            child
                        )
                )
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal
                )
                .ToList();


        if (
            childSignatures.Count == 0
        )
        {
            return "<NO RULES>";
        }


        if (
            childSignatures.Count == 1
        )
        {
            return
                childSignatures[0];
        }


        return
            "OR(" +
            string.Join(
                ",",
                childSignatures
            ) +
            ")";
    }


    // ------------------------------------------------------------------------
    // UNKNOWN ELEMENT FILTER
    // ------------------------------------------------------------------------

    return
        "UNSUPPORTED_ELEMENT_FILTER:" +
        elementFilter.GetType().Name;
};


// ============================================================================
// CATEGORY SIGNATURE FOR ParameterFilterElement
//
// Numeric ascending IDs, same as the Office Library exporter.
// ============================================================================

Func<ParameterFilterElement, string> BuildCategorySignature =
    delegate(ParameterFilterElement filter)
{
    if (filter == null)
        return "";

    ICollection<ElementId> categoryIds =
        filter.GetCategories();

    if (categoryIds == null)
        return "";


    return
        string.Join(
            ",",
            categoryIds
                .Select(
                    id =>
                        GetElementIdInteger(id)
                )
                .OrderBy(
                    id => id
                )
                .Select(
                    id =>
                        id.ToString(
                            CultureInfo.InvariantCulture
                        )
                )
        );
};


// ============================================================================
// FILTER RULE SIGNATURE
// ============================================================================

Func<ParameterFilterElement, string> BuildFilterRuleSignature =
    delegate(ParameterFilterElement filter)
{
    if (filter == null)
        return "<NO RULES>";

    try
    {
        ElementFilter elementFilter =
            filter.GetElementFilter();

        if (elementFilter == null)
            return "<NO RULES>";

        return
            BuildElementFilterSignature(
                elementFilter
            );
    }
    catch
    {
        return
            "UNABLE_TO_ANALYSE";
    }
};


// ============================================================================
// FULL DEFINITION SIGNATURE
//
// Exact Office match requires:
//
//     Filter Name
//     + Categories Applied
//     + Complete Rules
// ============================================================================

Func<ParameterFilterElement, string> BuildDefinitionSignature =
    delegate(ParameterFilterElement filter)
{
    if (filter == null)
        return "";

    string categorySignature =
        BuildCategorySignature(
            filter
        );

    string ruleSignature =
        BuildFilterRuleSignature(
            filter
        );


    return
        "NAME=" +
        filter.Name +
        "|CATEGORIES=" +
        categorySignature +
        "|RULES=" +
        ruleSignature;
};


// ============================================================================
// SHA256 HASH
// ============================================================================

Func<string, string> Sha256 =
    delegate(string text)
{
    using (
        System.Security.Cryptography.SHA256 sha =
            System.Security.Cryptography.SHA256.Create()
    )
    {
        byte[] input =
            Encoding.UTF8.GetBytes(
                text ?? ""
            );

        byte[] hash =
            sha.ComputeHash(
                input
            );

        StringBuilder result =
            new StringBuilder(
                hash.Length * 2
            );

        foreach (
            byte b
            in hash
        )
        {
            result.Append(
                b.ToString("x2")
            );
        }

        return
            result.ToString();
    }
};


// ============================================================================
// FULL DEFINITION HASH
// ============================================================================

Func<ParameterFilterElement, string> BuildDefinitionHash =
    delegate(ParameterFilterElement filter)
{
    return
        Sha256(
            BuildDefinitionSignature(
                filter
            )
        );
};


// ============================================================================
// BLOCK 4 COMPLETE
//
// Available to subsequent blocks:
//
//     GetElementIdInteger()
//     GetParameterName()
//     GetParameterSource()
//     GetParameterGuid()
//     GetParameterType()
//     GetStableParameterKey()
//
//     GetRuleParameterId()
//     GetEvaluatorName()
//     GetInnerRule()
//     GetRuleValue()
//     GetRuleEpsilon()
//     GetCanonicalElementIdValue()
//     GetCanonicalRuleValue()
//     GetNormalizedOperator()
//     GetCategoryRuleIds()
//
//     BuildRuleSignature()
//     BuildElementFilterSignature()
//     BuildCategorySignature()
//     BuildFilterRuleSignature()
//     BuildDefinitionSignature()
//     BuildDefinitionHash()
//
// ============================================================================

// ============================================================================
// BLOCK 5
// COLLECT + CLASSIFY ALL PARAMETER FILTERS IN CURRENT RVT
//
// Classification:
//
// Office
//     Name + Categories + Complete Rules exactly match office library.
//
// Modified Office
//     Filter has an exact office-library name, but its categories and/or
//     rules differ from the approved definition.
//
// Custom
//     Filter name is not an office-library name and no exact office
//     definition match exists.
//
// Unknown
//     Definition could not be safely analysed.
//
// ============================================================================


// ============================================================================
// PROJECT IDENTIFIER
//
// For the eventual workbook this allows rows from multiple project exports
// to be combined without relying solely on the RVT filename.
// ============================================================================

string projectId =
    doc.ProjectInformation != null &&
    !string.IsNullOrWhiteSpace(
        doc.ProjectInformation.Number
    )
    ? doc.ProjectInformation.Number
    : doc.Title;


// ============================================================================
// PROJECT FILTER RECORDS
//
// We use dictionaries at this stage so the Launchpad script remains
// self-contained. Later blocks will turn these into the Filters,
// Categories, Rules and Usage datasets.
// ============================================================================

List<Dictionary<string, object>> projectFilterRecords =
    new List<Dictionary<string, object>>();


// Fast lookup by Revit Filter ElementId.
Dictionary<long, Dictionary<string, object>> projectFilterById =
    new Dictionary<long, Dictionary<string, object>>();


// ============================================================================
// COLLECT PARAMETER FILTER ELEMENTS
// ============================================================================

List<ParameterFilterElement> projectFilters =
    new FilteredElementCollector(doc)
        .OfClass(
            typeof(ParameterFilterElement)
        )
        .Cast<ParameterFilterElement>()
        .OrderBy(
            f => f.Name,
            StringComparer.OrdinalIgnoreCase
        )
        .ThenBy(
            f => GetElementIdInteger(f.Id)
        )
        .ToList();


// ============================================================================
// CLASSIFY EACH FILTER
// ============================================================================

foreach (
    ParameterFilterElement filter
    in projectFilters
)
{
    long filterId =
        GetElementIdInteger(
            filter.Id
        );

    string filterName =
        filter.Name ?? "";


    string categorySignature =
        "";

    string ruleSignature =
        "";

    string definitionSignature =
        "";

    string definitionHash =
        "";


    string officeClassification =
        "Unknown";

    string officeFilterName =
        "";

    string definitionMatch =
        "Unable to determine";

    string categoryMatch =
        "Unable to determine";

    string ruleMatch =
        "Unable to determine";

    string analysisStatus =
        "Fully Analysed";

    string analysisNote =
        "";


    // ========================================================================
    // BUILD PROJECT DEFINITION
    // ========================================================================

    try
    {
        categorySignature =
            BuildCategorySignature(
                filter
            );

        ruleSignature =
            BuildFilterRuleSignature(
                filter
            );

        definitionSignature =
            BuildDefinitionSignature(
                filter
            );

        definitionHash =
            Sha256(
                definitionSignature
            );


        // Anything explicitly marked unsupported / unable to analyse must not
        // be allowed to produce a false Office match.

        if (
            ruleSignature.IndexOf(
                "UNSUPPORTED_",
                StringComparison.Ordinal
            ) >= 0 ||
            ruleSignature.IndexOf(
                "UNABLE_TO_ANALYSE",
                StringComparison.Ordinal
            ) >= 0 ||
            ruleSignature.IndexOf(
                "INNER=UNKNOWN",
                StringComparison.Ordinal
            ) >= 0
        )
        {
            analysisStatus =
                "Partially Analysed";

            analysisNote =
                "One or more filter rules could not be fully interpreted.";
        }
    }
    catch (Exception ex)
    {
        analysisStatus =
            "Not Analysed";

        analysisNote =
            ex.Message;

        officeClassification =
            "Unknown";

        definitionMatch =
            "Unable to determine";
    }


    // ========================================================================
    // OFFICE CLASSIFICATION
    // ========================================================================

    if (
        analysisStatus ==
        "Fully Analysed"
    )
    {
        string exactOfficeName =
            null;


        // --------------------------------------------------------------------
        // 1. EXACT DEFINITION HASH
        //
        // Because the definition hash contains:
        //
        //     Name + Categories + Rules
        //
        // this is our strongest exact-match test.
        // --------------------------------------------------------------------

        if (
            !string.IsNullOrWhiteSpace(
                definitionHash
            ) &&
            officeNameByDefinitionHash.TryGetValue(
                definitionHash,
                out exactOfficeName
            )
        )
        {
            officeClassification =
                "Office";

            officeFilterName =
                exactOfficeName;

            definitionMatch =
                "Exact";

            categoryMatch =
                "Exact";

            ruleMatch =
                "Exact";
        }

        else
        {
            // ----------------------------------------------------------------
            // 2. SAME OFFICE FILTER NAME
            //
            // If the project filter uses an approved office filter name but
            // its definition differs, classify it as Modified Office.
            // ----------------------------------------------------------------

            string officeDefinitionSignature;

            if (
                officeByName.TryGetValue(
                    filterName,
                    out officeDefinitionSignature
                )
            )
            {
                officeClassification =
                    "Modified Office";

                officeFilterName =
                    filterName;


                string expectedCategorySignature =
                    "";

                string expectedRuleSignature =
                    "";


                officeCategorySignatureByName.TryGetValue(
                    filterName,
                    out expectedCategorySignature
                );

                officeRuleSignatureByName.TryGetValue(
                    filterName,
                    out expectedRuleSignature
                );


                bool categoriesSame =
                    string.Equals(
                        categorySignature,
                        expectedCategorySignature,
                        StringComparison.Ordinal
                    );

                bool rulesSame =
                    string.Equals(
                        ruleSignature,
                        expectedRuleSignature,
                        StringComparison.Ordinal
                    );


                categoryMatch =
                    categoriesSame
                    ? "Exact"
                    : "Different";

                ruleMatch =
                    rulesSame
                    ? "Exact"
                    : "Different";


                if (
                    !categoriesSame &&
                    !rulesSame
                )
                {
                    definitionMatch =
                        "Categories and Rules Modified";
                }
                else if (
                    !categoriesSame
                )
                {
                    definitionMatch =
                        "Categories Modified";
                }
                else if (
                    !rulesSame
                )
                {
                    definitionMatch =
                        "Rules Modified";
                }
                else
                {
                    // Defensive fallback. In normal operation this should have
                    // been caught by the exact hash test above.
                    definitionMatch =
                        "Definition differs";
                }
            }

            else
            {
                // ------------------------------------------------------------
                // 3. DIFFERENT NAME, BUT SAME CATEGORIES + RULES
                //
                // This is deliberately NOT classified as Office because our
                // agreed Office definition requires the name as well.
                //
                // However, recording the matching office reference is useful
                // for audit review.
                // ------------------------------------------------------------

                string sameDefinitionExceptNameOfficeFilter =
                    null;


                foreach (
                    KeyValuePair<string, string> officeEntry
                    in officeRuleSignatureByName
                )
                {
                    string candidateOfficeName =
                        officeEntry.Key;

                    string candidateRuleSignature =
                        officeEntry.Value;

                    string candidateCategorySignature =
                        "";


                    officeCategorySignatureByName.TryGetValue(
                        candidateOfficeName,
                        out candidateCategorySignature
                    );


                    if (
                        string.Equals(
                            ruleSignature,
                            candidateRuleSignature,
                            StringComparison.Ordinal
                        ) &&
                        string.Equals(
                            categorySignature,
                            candidateCategorySignature,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        sameDefinitionExceptNameOfficeFilter =
                            candidateOfficeName;

                        break;
                    }
                }


                if (
                    !string.IsNullOrWhiteSpace(
                        sameDefinitionExceptNameOfficeFilter
                    )
                )
                {
                    officeClassification =
                        "Custom";

                    officeFilterName =
                        sameDefinitionExceptNameOfficeFilter;

                    definitionMatch =
                        "Same Categories and Rules / Different Name";

                    categoryMatch =
                        "Exact";

                    ruleMatch =
                        "Exact";
                }

                else
                {
                    // --------------------------------------------------------
                    // 4. CUSTOM FILTER
                    // --------------------------------------------------------

                    officeClassification =
                        "Custom";

                    officeFilterName =
                        "";

                    definitionMatch =
                        "No Office Definition Match";

                    categoryMatch =
                        "Not Compared";

                    ruleMatch =
                        "Not Compared";
                }
            }
        }
    }


    // ========================================================================
    // PARTIAL / FAILED ANALYSIS
    //
    // We can still recognise an office filter name, but we do not claim a
    // definition match if its rules were not safely analysed.
    // ========================================================================

    else
    {
        string ignoredOfficeDefinition;

        if (
            officeByName.TryGetValue(
                filterName,
                out ignoredOfficeDefinition
            )
        )
        {
            officeFilterName =
                filterName;

            officeClassification =
                "Unknown";

            definitionMatch =
                "Office Name Match / Definition Not Fully Analysed";
        }
        else
        {
            officeClassification =
                "Unknown";

            definitionMatch =
                "Unable to determine";
        }
    }


    // ========================================================================
    // CREATE FILTER RECORD
    // ========================================================================

    Dictionary<string, object> record =
        new Dictionary<string, object>(
            StringComparer.OrdinalIgnoreCase
        );


    record["Project ID"] =
        projectId;

    record["Filter ID"] =
        filterId;

    record["Filter Unique ID"] =
        filter.UniqueId ?? "";

    record["Filter Name"] =
        filterName;


    record["Office Classification"] =
        officeClassification;

    record["Office Filter Name"] =
        officeFilterName;

    record["Office Library Version"] =
        officeLibraryVersion;

    record["Definition Match"] =
        definitionMatch;


    // These two fields are useful internally for analysis/debugging.
    // They do not have to become columns in the final Filters worksheet.

    record["Category Match"] =
        categoryMatch;

    record["Rule Match"] =
        ruleMatch;


    // Usage values are populated by the later Usage block.

    record["Usage Status"] =
        "Unused";

    record["Direct View Count"] =
        0;

    record["View Template Count"] =
        0;

    record["Total Usage Count"] =
        0;


    // Category values are populated by the later Categories block.

    record["Category Count"] =
        0;

    record["Category Groups"] =
        "";


    // Rule values are populated by the later Rules block.

    record["Rule Count"] =
        0;

    record["Rule Analysis Status"] =
        analysisStatus;

    record["Rule Expression"] =
        "";


    // Internal canonical values.
    // These will also drive overlap analysis.

    record["_CategorySignature"] =
        categorySignature;

    record["_RuleSignature"] =
        ruleSignature;

    record["_DefinitionSignature"] =
        definitionSignature;

    record["_DefinitionHash"] =
        definitionHash;

    record["_AnalysisNote"] =
        analysisNote;


    projectFilterRecords.Add(
        record
    );


    if (
        !projectFilterById.ContainsKey(
            filterId
        )
    )
    {
        projectFilterById.Add(
            filterId,
            record
        );
    }
}


// ============================================================================
// BASIC CLASSIFICATION COUNTS
// ============================================================================

int officeFilterCount =
    projectFilterRecords.Count(
        r =>
            Convert.ToString(
                r["Office Classification"]
            ) == "Office"
    );


int modifiedOfficeFilterCount =
    projectFilterRecords.Count(
        r =>
            Convert.ToString(
                r["Office Classification"]
            ) == "Modified Office"
    );


int customFilterCount =
    projectFilterRecords.Count(
        r =>
            Convert.ToString(
                r["Office Classification"]
            ) == "Custom"
    );


int unknownFilterCount =
    projectFilterRecords.Count(
        r =>
            Convert.ToString(
                r["Office Classification"]
            ) == "Unknown"
    );


// ============================================================================
// SANITY CHECK
// ============================================================================

if (
    projectFilterRecords.Count !=
    projectFilters.Count
)
{
    TaskDialog.Show(
        "Filter Audit",
        "Filter collection validation failed.\n\n" +

        "ParameterFilterElements found: " +
        projectFilters.Count +
        "\n" +

        "Audit records created: " +
        projectFilterRecords.Count
    );

    return;
}


// ============================================================================
// BLOCK 5 COMPLETE
//
// We now have:
//
//     projectFilters
//     projectFilterRecords
//     projectFilterById
//
// plus counts:
//
//     officeFilterCount
//     modifiedOfficeFilterCount
//     customFilterCount
//     unknownFilterCount
//
// Each project filter now has its canonical:
//
//     Category Signature
//     Rule Signature
//     Definition Signature
//     Definition Hash
//
// and an initial Office classification.
//
// ============================================================================

// ============================================================================
// BLOCK 6
// CATEGORY EXTRACTION + BUSINESS CLASSIFICATION
//
// Business groups:
//
//     Model
//     Annotation
//     Views / Documentation
//     Other
//
// Classification status:
//
//     Classified
//     Unclassified
//
// IMPORTANT:
//
// "Other" is a legitimate classified business group.
// It does NOT mean that the category is unknown.
//
// Any category not explicitly mapped below becomes:
//
//     Category Group                 = Other
//     Category Classification Status = Unclassified
//
// The mapping uses:
//
// 1. BuiltInCategory enum names resolved at runtime.
//    This avoids compile failures when enum members differ between
//    Revit versions.
//
// 2. Explicit numeric BuiltInCategory IDs for categories found during
//    validation of the real project.
//
// ============================================================================


// ============================================================================
// CATEGORY DATASET
// ============================================================================

List<Dictionary<string, object>> categoryRecords =
    new List<Dictionary<string, object>>();


// ============================================================================
// BUSINESS CATEGORY MAP
//
// Key = BuiltInCategory integer value / Category ElementId
// ============================================================================

Dictionary<long, string> categoryGroupMap =
    new Dictionary<long, string>();


// ============================================================================
// ADD CATEGORY BY BUILTIN ENUM NAME
//
// Runtime resolution means a category name that does not exist in the
// installed Revit version is simply ignored rather than causing compilation
// to fail.
//
// ============================================================================

Action<string, string> AddBuiltInCategoryByName =
    delegate(
        string builtInCategoryName,
        string businessGroup
    )
{
    try
    {
        BuiltInCategory bic;

        if (
            Enum.TryParse<BuiltInCategory>(
                builtInCategoryName,
                out bic
            )
        )
        {
            long id =
                Convert.ToInt64(
                    bic,
                    CultureInfo.InvariantCulture
                );


            categoryGroupMap[id] =
                businessGroup;
        }
    }
    catch
    {
        // Intentionally ignored.
        // Missing enum members are handled by explicit ID mapping where
        // required.
    }
};


// ============================================================================
// ADD CATEGORY BY EXPLICIT ID
// ============================================================================

Action<long, string> AddBuiltInCategoryById =
    delegate(
        long categoryId,
        string businessGroup
    )
{
    categoryGroupMap[categoryId] =
        businessGroup;
};


// ============================================================================
// MODEL
// ============================================================================

string[] modelBuiltInCategoryNames =
{
    // ------------------------------------------------------------------------
    // Architecture
    // ------------------------------------------------------------------------

    "OST_Walls",
    "OST_Floors",
    "OST_Roofs",
    "OST_Ceilings",
    "OST_Doors",
    "OST_Windows",
    "OST_CurtainWallPanels",
    "OST_CurtainWallMullions",
    "OST_Curtain_Systems",
    "OST_GenericModel",
    "OST_Furniture",
    "OST_FurnitureSystems",
    "OST_Casework",
    "OST_SpecialityEquipment",
    "OST_PlumbingFixtures",
    "OST_LightingFixtures",
    "OST_LightingDevices",
    "OST_ElectricalFixtures",
    "OST_ElectricalEquipment",
    "OST_MechanicalEquipment",
    "OST_CommunicationDevices",
    "OST_DataDevices",
    "OST_FireAlarmDevices",
    "OST_NurseCallDevices",
    "OST_SecurityDevices",
    "OST_Sprinklers",
    "OST_TelephoneDevices",

    "OST_Stairs",
    "OST_StairsRuns",
    "OST_StairsLandings",
    "OST_StairsSupports",
    "OST_Ramps",
    "OST_Railings",
    "OST_Handrails",
    "OST_TopRails",

    "OST_Columns",
    "OST_StructuralColumns",
    "OST_StructuralFraming",
    "OST_StructuralFoundation",
    "OST_StructuralTruss",
    "OST_StructuralStiffener",
    "OST_StructuralFramingSystem",

    "OST_Parts",
    "OST_Assemblies",

    "OST_ShaftOpening",
    "OST_FloorOpening",
    "OST_WallOpening",

    "OST_Site",
    "OST_Topography",
    "OST_Toposolid",
    "OST_Planting",
    "OST_Parking",
    "OST_Roads",
    "OST_Property",
    "OST_PropertyLines",

    "OST_Rooms",
    "OST_MEPSpaces",
    "OST_Areas",

    // ------------------------------------------------------------------------
    // Roof / floor accessories
    // ------------------------------------------------------------------------

    "OST_EdgeSlab",
    "OST_Fascia",
    "OST_Gutter",
    "OST_RoofSoffit",
    "OST_WallSweep",

    // ------------------------------------------------------------------------
    // Massing
    // ------------------------------------------------------------------------

    "OST_Mass",
    "OST_MassFloor",
    "OST_MassOpening",
    "OST_MassSkylights",
    "OST_MassGlazing",
    "OST_MassRoof",
    "OST_MassExteriorWall",
    "OST_MassInteriorWall",
    "OST_MassZone",

    // ------------------------------------------------------------------------
    // MEP
    // ------------------------------------------------------------------------

    "OST_DuctCurves",
    "OST_DuctFitting",
    "OST_DuctAccessory",
    "OST_DuctTerminal",
    "OST_FlexDuctCurves",
    "OST_DuctSystem",

    "OST_PipeCurves",
    "OST_PipeFitting",
    "OST_PipeAccessory",
    "OST_FlexPipeCurves",
    "OST_PipingSystem",

    "OST_CableTray",
    "OST_CableTrayFitting",
    "OST_Conduit",
    "OST_ConduitFitting",

    "OST_DuctInsulations",
    "OST_DuctLinings",
    "OST_PipeInsulations",

    "OST_DuctPlaceholders",
    "OST_PipePlaceholders",

    "OST_Wire",

    "OST_HVAC_Zones",

    // ------------------------------------------------------------------------
    // Fabrication
    // ------------------------------------------------------------------------

    "OST_FabricationDuctwork",
    "OST_FabricationPipework",
    "OST_FabricationContainment",
    "OST_FabricationHangers",

    // ------------------------------------------------------------------------
    // Structural reinforcement
    // ------------------------------------------------------------------------

    "OST_Rebar",
    "OST_AreaRein",
    "OST_PathRein",
    "OST_FabricReinforcement",
    "OST_FabricAreas",
    "OST_RebarCoupler",

    // ------------------------------------------------------------------------
    // Structural connections
    // ------------------------------------------------------------------------

    "OST_StructConnections",
    "OST_StructConnectionAnchors",
    "OST_StructConnectionBolts",
    "OST_StructConnectionHoles",
    "OST_StructConnectionOthers",
    "OST_StructConnectionPlates",
    "OST_StructConnectionProfiles",
    "OST_StructConnectionShearStuds",
    "OST_StructConnectionWelds",
    "OST_StructConnectionModifiers",
    "OST_StructConnectionSymbol",

    // ------------------------------------------------------------------------
    // Miscellaneous physical/model categories
    // ------------------------------------------------------------------------

    "OST_Signage",
    "OST_AudioVisualDevices",
    "OST_MedicalEquipment",
    "OST_FoodServiceEquipment",
    "OST_FireProtection",
    "OST_Hardscape",
    "OST_TemporaryStructure"
};


foreach (
    string builtInCategoryName
    in modelBuiltInCategoryNames
)
{
    AddBuiltInCategoryByName(
        builtInCategoryName,
        "Model"
    );
}


// ============================================================================
// ANNOTATION
// ============================================================================

string[] annotationBuiltInCategoryNames =
{
    "OST_DetailComponents",
    "OST_GenericAnnotation",
    "OST_TextNotes",
    "OST_Dimensions",
    "OST_FilledRegion",
    "OST_Lines",

    "OST_Tags",
    "OST_MultiCategoryTags",
    "OST_MaterialTags",

    "OST_WallTags",
    "OST_DoorTags",
    "OST_WindowTags",
    "OST_FloorTags",
    "OST_RoofTags",
    "OST_CeilingTags",
    "OST_FurnitureTags",
    "OST_CaseworkTags",
    "OST_GenericModelTags",
    "OST_StructuralFramingTags",
    "OST_StructuralColumnTags",
    "OST_StructuralFoundationTags",
    "OST_MechanicalEquipmentTags",
    "OST_PlumbingFixtureTags",
    "OST_RoomTags",
    "OST_MEPSpaceTags",
    "OST_AreaTags",

    "OST_SpotElevations",
    "OST_SpotCoordinates",
    "OST_SpotSlopes",

    "OST_KeynoteTags",

    "OST_RevisionCloudTags",

    "OST_InsulationTags",
    "OST_PipeTags",
    "OST_DuctTags",
    "OST_DuctTerminalTags",
    "OST_PipeAccessoryTags",
    "OST_PipeFittingTags",
    "OST_DuctAccessoryTags",
    "OST_DuctFittingTags",

    "OST_ElectricalEquipmentTags",
    "OST_ElectricalFixtureTags",
    "OST_LightingFixtureTags",
    "OST_LightingDeviceTags",

    "OST_SprinklerTags"
};


foreach (
    string builtInCategoryName
    in annotationBuiltInCategoryNames
)
{
    AddBuiltInCategoryByName(
        builtInCategoryName,
        "Annotation"
    );
}


// ============================================================================
// VIEWS / DOCUMENTATION
// ============================================================================

string[] documentationBuiltInCategoryNames =
{
    // ------------------------------------------------------------------------
    // Views
    // ------------------------------------------------------------------------

    "OST_Views",
    "OST_Sections",
    "OST_Elev",
    "OST_Callouts",

    // ------------------------------------------------------------------------
    // Sheets / view placement
    // ------------------------------------------------------------------------

    "OST_Sheets",
    "OST_Viewports",

    // ------------------------------------------------------------------------
    // Schedules
    // ------------------------------------------------------------------------

    "OST_Schedules",
    "OST_ScheduleGraphics",

    // ------------------------------------------------------------------------
    // Datum / documentation controls
    // ------------------------------------------------------------------------

    "OST_Grids",
    "OST_Levels",
    "OST_ReferencePlanes",
    "OST_ReferenceLines",

    // ------------------------------------------------------------------------
    // Revision / reference
    // ------------------------------------------------------------------------

    "OST_RevisionClouds",
    "OST_Revisions",
    "OST_ReferenceViewer",

    // ------------------------------------------------------------------------
    // Navigation / analysis graphics
    // ------------------------------------------------------------------------

    "OST_PathOfTravelLines"
};


foreach (
    string builtInCategoryName
    in documentationBuiltInCategoryNames
)
{
    AddBuiltInCategoryByName(
        builtInCategoryName,
        "Views / Documentation"
    );
}


// ============================================================================
// OTHER
//
// These categories are intentionally classified as Other.
//
// They are generally analytical, calculation-oriented or load-related rather
// than physical model geometry, annotation or view/documentation content.
//
// ============================================================================

string[] otherBuiltInCategoryNames =
{
    // ------------------------------------------------------------------------
    // Structural loads
    // ------------------------------------------------------------------------

    "OST_PointLoads",
    "OST_LineLoads",
    "OST_AreaLoads",

    "OST_InternalPointLoads",
    "OST_InternalLineLoads",
    "OST_InternalAreaLoads",

    // ------------------------------------------------------------------------
    // Analytical model
    // ------------------------------------------------------------------------

    "OST_AnalyticalNodes",
    "OST_AnalyticalLinks",
    "OST_AnalyticalSurfaces",
    "OST_AnalyticalSpaces",

    // ------------------------------------------------------------------------
    // Other analytical/system categories
    // ------------------------------------------------------------------------

    "OST_AnalyticalPipeConnections"
};


foreach (
    string builtInCategoryName
    in otherBuiltInCategoryNames
)
{
    AddBuiltInCategoryByName(
        builtInCategoryName,
        "Other"
    );
}


// ============================================================================
// EXPLICIT REVIT 2024 CATEGORY-ID FALLBACKS
//
// These IDs came directly from the Categories worksheet generated against
// the validated 295-filter project.
//
// They are added explicitly so the business mapping is independent of any
// BuiltInCategory enum naming differences.
//
// ============================================================================


// ============================================================================
// MODEL - EXPLICIT IDS
// ============================================================================

long[] explicitModelCategoryIds =
{
    // Slab Edges
    -2001392,

    // Vertical Circulation
    -2001052,

    // Railings
    -2000126,

    // Structural Rebar Couplers
    -2009060,

    // Structural connection subcategories
    // Modifiers
    -2009047,

    // Welds
    -2009046,

    // Holes
    -2009045,

    // Shear Studs
    -2009044,

    // Others
    -2009042,

    // Bolts
    -2009041,

    // Anchors
    -2009039,

    // Plates
    -2009038,

    // Profiles
    -2009037,

    // Symbol
    -2009033,

    // Structural Connections
    -2009030,

    // Structural Fabric Areas
    -2009017,

    // Structural Fabric Reinforcement
    -2009016,

    // Plumbing Equipment
    -2008234,

    // Mechanical Control Devices
    -2008232,

    // Mass Opening
    -2003417,

    // Mass Skylight
    -2003416,

    // Mass Glazing
    -2003415,

    // Mass Roof
    -2003414,

    // Mass Exterior Wall
    -2003413,

    // Mass Interior Wall
    -2003412,

    // Mass Zone
    -2003411,

    // Mass Floor
    -2003403,

    // Roof Soffits
    -2001393,

    // Gutters
    -2001391,

    // Fascias
    -2001390,

    // Structural Stiffeners
    -2001354,

    // Structural Trusses
    -2001336,

    // Structural Beam Systems
    -2001327,

    // Property Lines
    -2001265,

    // Pads
    -2001263,

    // Roads
    -2001220,

    // Toposolid
    -2001079,

    // Signage
    -2001058,

    // Audio Visual Devices
    -2001055,

    // Fire Protection
    -2001049,

    // Medical Equipment
    -2001046,

    // Food Service Equipment
    -2001043,

    // Temporary Structures
    -2001039,

    // Hardscape
    -2001036,

    // Shaft Openings
    -2000996,

    // Terminations
    -2000949,

    // Supports
    -2000948,

    // Handrails
    -2000947,

    // Top Rails
    -2000946,

    // Curtain Systems
    -2000340,

    // Wall Sweeps
    -2000181,

    // Balusters
    -2000127,

    // MEP Fabrication Pipework
    -2008208,

    // MEP Fabrication Ductwork
    -2008193,

    // Pipe Placeholders
    -2008161,

    // Duct Placeholders
    -2008160,

    // Duct Linings
    -2008124,

    // Duct Insulations
    -2008123,

    // Pipe Insulations
    -2008122,

    // Piping Systems
    -2008043,

    // Duct Systems
    -2008015,

    // Insulation
    -2008221,

    // Lining
    -2008220,

    // MEP Fabrication Containment
    -2008212,

    // MEP Fabrication Hangers
    -2008203,

    // Fabrication Insulation
    -2008198,

    // HVAC Zones
    -2008107,

    // Switch System
    -2008101,

    // Telephone Devices
    -2008075,

    // Wires
    -2008039,

    // System-Zones
    -2001001,
    
    // Entourage
    -2001370
};


foreach (
    long categoryId
    in explicitModelCategoryIds
)
{
    AddBuiltInCategoryById(
        categoryId,
        "Model"
    );
}


// ============================================================================
// VIEWS / DOCUMENTATION - EXPLICIT IDS
// ============================================================================

long[] explicitDocumentationCategoryIds =
{
    // <Path of Travel Lines>
    -2000833,

    // Reference Lines
    -2000083,

    // Reference Planes
    -2000530,

    // Grids
    -2000220,

    // Levels
    -2000240
};


foreach (
    long categoryId
    in explicitDocumentationCategoryIds
)
{
    AddBuiltInCategoryById(
        categoryId,
        "Views / Documentation"
    );
}


// ============================================================================
// OTHER - EXPLICIT IDS
// ============================================================================

long[] explicitOtherCategoryIds =
{
    // ------------------------------------------------------------------------
    // Structural loads
    // ------------------------------------------------------------------------

    // Internal Area Loads
    -2005207,

    // Internal Line Loads
    -2005206,

    // Internal Point Loads
    -2005205,

    // Area Loads
    -2005203,

    // Line Loads
    -2005202,

    // Point Loads
    -2005201,

    // ------------------------------------------------------------------------
    // Analytical model
    // ------------------------------------------------------------------------

    // Analytical Links
    -2009657,

    // Analytical Nodes
    -2009645,

    // Analytical Surfaces
    -2008186,

    // Analytical Spaces
    -2008185,

    // Analytical Pipe Connections
    -2000983
};


foreach (
    long categoryId
    in explicitOtherCategoryIds
)
{
    AddBuiltInCategoryById(
        categoryId,
        "Other"
    );
}


// ============================================================================
// GET BUSINESS CATEGORY GROUP
// ============================================================================

Func<Category, string> GetBusinessCategoryGroup =
    delegate(Category category)
{
    if (category == null)
        return "Other";


    long categoryId =
        GetElementIdInteger(
            category.Id
        );


    string group;


    if (
        categoryGroupMap.TryGetValue(
            categoryId,
            out group
        )
    )
    {
        return group;
    }


    return "Other";
};


// ============================================================================
// GET CLASSIFICATION STATUS
// ============================================================================

Func<Category, string> GetCategoryClassificationStatus =
    delegate(Category category)
{
    if (category == null)
        return "Unclassified";


    long categoryId =
        GetElementIdInteger(
            category.Id
        );


    if (
        categoryGroupMap.ContainsKey(
            categoryId
        )
    )
    {
        return "Classified";
    }


    return "Unclassified";
};


// ============================================================================
// CATEGORY GROUP DISPLAY ORDER
// ============================================================================

Dictionary<string, int> categoryGroupSortOrder =
    new Dictionary<string, int>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        { "Model", 1 },
        { "Annotation", 2 },
        { "Views / Documentation", 3 },
        { "Other", 4 }
    };


// ============================================================================
// EXTRACT CATEGORIES FOR EVERY PARAMETER FILTER
// ============================================================================

foreach (
    ParameterFilterElement filter
    in projectFilters
)
{
    long filterId =
        GetElementIdInteger(
            filter.Id
        );


    Dictionary<string, object> filterRecord =
        projectFilterRecords.FirstOrDefault(
            r =>
                Convert.ToInt64(
                    r["Filter ID"],
                    CultureInfo.InvariantCulture
                ) ==
                filterId
        );


    if (filterRecord == null)
        continue;


    ICollection<ElementId> categoryIds;


    try
    {
        categoryIds =
            filter.GetCategories();
    }
    catch
    {
        categoryIds =
            new List<ElementId>();
    }


    HashSet<string> filterBusinessGroups =
        new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );


    int categoryCount =
        0;


    foreach (
        ElementId categoryId
        in categoryIds
            .OrderBy(
                id =>
                    GetElementIdInteger(id)
            )
    )
    {
        Category category =
            null;


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
            category =
                null;
        }


        long categoryIdValue =
            GetElementIdInteger(
                categoryId
            );


        string categoryName =
            category != null
            ? category.Name
            : "<Unknown Category>";


        string categoryGroup =
            category != null
            ? GetBusinessCategoryGroup(category)
            : "Other";


        string classificationStatus =
            category != null
            ? GetCategoryClassificationStatus(category)
            : "Unclassified";


        Dictionary<string, object> categoryRecord =
            new Dictionary<string, object>(
                StringComparer.OrdinalIgnoreCase
            );


        categoryRecord["Project ID"] =
            projectId;


        categoryRecord["Filter ID"] =
            filterId;


        categoryRecord["Filter Name"] =
            filter.Name;


        categoryRecord["Category ID"] =
            categoryIdValue;


        categoryRecord["Category Name"] =
            categoryName;


        categoryRecord["Category Group"] =
            categoryGroup;


        categoryRecord["Category Classification Status"] =
            classificationStatus;


        categoryRecords.Add(
            categoryRecord
        );


        categoryCount++;


        filterBusinessGroups.Add(
            categoryGroup
        );
    }


    // ========================================================================
    // UPDATE FILTER MASTER RECORD
    // ========================================================================

    filterRecord["Category Count"] =
        categoryCount;


    List<string> orderedBusinessGroups =
        filterBusinessGroups
            .OrderBy(
                g =>
                    categoryGroupSortOrder.ContainsKey(g)
                    ? categoryGroupSortOrder[g]
                    : 99
            )
            .ThenBy(
                g => g,
                StringComparer.OrdinalIgnoreCase
            )
            .ToList();


    filterRecord["Category Groups"] =
        string.Join(
            "; ",
            orderedBusinessGroups
        );
}


// ============================================================================
// SORT CATEGORY TABLE
// ============================================================================

categoryRecords =
    categoryRecords
        .OrderBy(
            r =>
                Convert.ToString(
                    r["Filter Name"]
                ),
            StringComparer.OrdinalIgnoreCase
        )
        .ThenBy(
            r =>
                Convert.ToString(
                    r["Category Group"]
                ),
            StringComparer.OrdinalIgnoreCase
        )
        .ThenBy(
            r =>
                Convert.ToString(
                    r["Category Name"]
                ),
            StringComparer.OrdinalIgnoreCase
        )
        .ToList();


// ============================================================================
// SUMMARY COUNTS
// ============================================================================

int modelCategoryRowCount =
    categoryRecords.Count(
        r =>
            Convert.ToString(
                r["Category Group"]
            ) ==
            "Model"
    );


int annotationCategoryRowCount =
    categoryRecords.Count(
        r =>
            Convert.ToString(
                r["Category Group"]
            ) ==
            "Annotation"
    );


int viewsDocumentationCategoryRowCount =
    categoryRecords.Count(
        r =>
            Convert.ToString(
                r["Category Group"]
            ) ==
            "Views / Documentation"
    );


int otherCategoryRowCount =
    categoryRecords.Count(
        r =>
            Convert.ToString(
                r["Category Group"]
            ) ==
            "Other"
    );


int unclassifiedCategoryRows =
    categoryRecords.Count(
        r =>
            Convert.ToString(
                r["Category Classification Status"]
            ) ==
            "Unclassified"
    );


// ============================================================================
// BLOCK 6 COMPLETE
// ============================================================================

// ============================================================================
// BLOCK 7
// RULES DATASET + COMPLETE RULE TREE EXTRACTION
//
// Creates one row per terminal/user-facing rule condition.
//
// Locked Rules columns:
//
//  1. Project ID
//  2. Filter ID
//  3. Filter Name
//  4. Rule Index
//  5. Tree Path
//  6. Root Logic
//  7. Rule Type
//  8. Inner Rule Type
//  9. Is Inverted
// 10. Parameter ID
// 11. Parameter Name
// 12. Parameter Type
// 13. Parameter Source
// 14. Parameter GUID
// 15. Evaluator
// 16. Operator
// 17. Raw Value
// 18. Display Value
// 19. Unit
// 20. Rule Expression
// 21. Rule Analysis Status
// 22. Raw Rule Data
// 23. Rule Signature
//
// ============================================================================


// ============================================================================
// RULE DATASET
// ============================================================================

List<Dictionary<string, object>> ruleRecords =
    new List<Dictionary<string, object>>();


// ============================================================================
// SAFE CATEGORY NAME
// ============================================================================

Func<ElementId, string> GetCategoryNameById =
    delegate(ElementId categoryId)
{
    if (
        categoryId == null ||
        categoryId == ElementId.InvalidElementId
    )
    {
        return "";
    }

    try
    {
        Category category =
            Category.GetCategory(
                doc,
                categoryId
            );

        if (category != null)
            return category.Name ?? "";
    }
    catch
    {
    }

    return "";
};


// ============================================================================
// HUMAN OPERATOR
// ============================================================================

Func<string, string> GetDisplayOperator =
    delegate(string op)
{
    switch (op)
    {
        case "Equals":
            return "=";

        case "NotEquals":
            return "!=";

        case "Contains":
            return "contains";

        case "DoesNotContain":
            return "does not contain";

        case "BeginsWith":
            return "begins with";

        case "DoesNotBeginWith":
            return "does not begin with";

        case "EndsWith":
            return "ends with";

        case "DoesNotEndWith":
            return "does not end with";

        case "GreaterThan":
            return ">";

        case "GreaterThanOrEqual":
            return ">=";

        case "LessThan":
            return "<";

        case "LessThanOrEqual":
            return "<=";

        case "HasValue":
            return "has value";

        case "HasNoValue":
            return "has no value";

        default:
            return op ?? "";
    }
};


// ============================================================================
// DISPLAY VALUE
//
// V1 deliberately preserves the Revit API value without guessing a unit
// conversion.
//
// Double rule values therefore remain in Revit internal units here.
// A later export layer can format them using project units if required.
//
// ElementId values are shown semantically where possible.
// ============================================================================

Func<object, string> GetDisplayRuleValue =
    delegate(object rawValue)
{
    if (rawValue == null)
        return "";

    ElementId elementId =
        rawValue as ElementId;

    if (elementId != null)
    {
        Element element =
            doc.GetElement(
                elementId
            );

        if (element != null)
        {
            string elementName =
                element.Name ?? "";

            if (
                !string.IsNullOrWhiteSpace(
                    elementName
                )
            )
            {
                return elementName;
            }
        }

        return
            GetElementIdInteger(
                elementId
            )
            .ToString(
                CultureInfo.InvariantCulture
            );
    }


    string stringValue =
        rawValue as string;

    if (stringValue != null)
        return stringValue;


    IFormattable formattable =
        rawValue as IFormattable;

    if (formattable != null)
    {
        return
            formattable.ToString(
                null,
                CultureInfo.InvariantCulture
            );
    }


    return rawValue.ToString();
};


// ============================================================================
// DETERMINE IF A RULE TYPE IS SAFELY UNDERSTOOD
// ============================================================================

Func<string, bool> IsSupportedRuleType =
    delegate(string ruleType)
{
    return
        ruleType == "FilterStringRule" ||
        ruleType == "FilterIntegerRule" ||
        ruleType == "FilterDoubleRule" ||
        ruleType == "FilterElementIdRule" ||
        ruleType == "FilterCategoryRule";
};


// ============================================================================
// BUILD HUMAN RULE EXPRESSION
//
// Handles inverse wrappers by resolving the inner rule and applying the
// inverted operator.
//
// Examples:
//
//     Fire Rating = 60
//     Family Name contains WasteBin
//     Sheet Number does not contain 51
//     Category = Detail Items
//
// ============================================================================

Func<FilterRule, bool, string> BuildHumanRuleExpression = null;

BuildHumanRuleExpression =
    delegate(
        FilterRule inputRule,
        bool inheritedInverse
    )
{
    if (inputRule == null)
        return "<Unknown Rule>";


    string inputRuleType =
        inputRule.GetType().Name;


    // ------------------------------------------------------------------------
    // INVERSE WRAPPER
    // ------------------------------------------------------------------------

    if (
        inputRuleType ==
        "FilterInverseRule"
    )
    {
        FilterRule inner =
            GetInnerRule(
                inputRule
            );

        if (inner == null)
            return "<Unsupported Inverse Rule>";

        return
            BuildHumanRuleExpression(
                inner,
                !inheritedInverse
            );
    }


    // ------------------------------------------------------------------------
    // CATEGORY RULE
    // ------------------------------------------------------------------------

    if (
        inputRuleType ==
        "FilterCategoryRule"
    )
    {
        List<ElementId> categoryIds =
            GetCategoryRuleIds(
                inputRule
            );


        List<string> categoryNames =
            new List<string>();


        foreach (
            ElementId categoryId
            in categoryIds
        )
        {
            string categoryName =
                GetCategoryNameById(
                    categoryId
                );

            if (
                string.IsNullOrWhiteSpace(
                    categoryName
                )
            )
            {
                categoryName =
                    GetElementIdInteger(
                        categoryId
                    )
                    .ToString(
                        CultureInfo.InvariantCulture
                    );
            }


            categoryNames.Add(
                categoryName
            );
        }


        string categoryText =
            string.Join(
                ", ",
                categoryNames
            );


        if (inheritedInverse)
        {
            return
                "Category is not " +
                categoryText;
        }


        return
            "Category = " +
            categoryText;
    }


    // ------------------------------------------------------------------------
    // PARAMETER RULE
    // ------------------------------------------------------------------------

    ElementId parameterId =
        GetRuleParameterId(
            inputRule
        );

    string parameterName =
        GetParameterName(
            parameterId
        );


    if (
        string.IsNullOrWhiteSpace(
            parameterName
        )
    )
    {
        parameterName =
            GetStableParameterKey(
                parameterId
            );
    }


    string evaluatorName =
        GetEvaluatorName(
            inputRule
        );

    string normalizedOperator =
        GetNormalizedOperator(
            evaluatorName,
            inheritedInverse
        );

    string displayOperator =
        GetDisplayOperator(
            normalizedOperator
        );

    object rawValue =
        GetRuleValue(
            inputRule
        );

    string displayValue =
        GetDisplayRuleValue(
            rawValue
        );


    // Quotation marks make string comparisons easier to read.
    if (
        rawValue is string
    )
    {
        displayValue =
            "\"" +
            displayValue +
            "\"";
    }


    if (
        string.IsNullOrWhiteSpace(
            displayValue
        )
    )
    {
        return
            parameterName +
            " " +
            displayOperator;
    }


    return
        parameterName +
        " " +
        displayOperator +
        " " +
        displayValue;
};


// ============================================================================
// BUILD HUMAN EXPRESSION FROM COMPLETE ELEMENT FILTER TREE
// ============================================================================

Func<ElementFilter, string> BuildElementFilterExpression = null;

BuildElementFilterExpression =
    delegate(ElementFilter elementFilter)
{
    if (elementFilter == null)
        return "<No Rules>";


    // ------------------------------------------------------------------------
    // ELEMENT PARAMETER FILTER
    // ------------------------------------------------------------------------

    ElementParameterFilter parameterFilter =
        elementFilter as ElementParameterFilter;

    if (parameterFilter != null)
    {
        List<FilterRule> rules =
            GetParameterFilterRules(
                parameterFilter
            );


        if (
            rules.Count == 0
        )
        {
            return "<No Rules>";
        }


        List<string> expressions =
            rules
            .Select(
                rule =>
                    BuildHumanRuleExpression(
                        rule,
                        false
                    )
            )
            .ToList();


        if (
            expressions.Count == 1
        )
        {
            return expressions[0];
        }


        return
            "(" +
            string.Join(
                " AND ",
                expressions
            ) +
            ")";
    }


    // ------------------------------------------------------------------------
    // LOGICAL AND
    // ------------------------------------------------------------------------

    LogicalAndFilter andFilter =
        elementFilter as LogicalAndFilter;

    if (andFilter != null)
    {
        List<string> expressions =
            andFilter
            .GetFilters()
            .Select(
                child =>
                    BuildElementFilterExpression(
                        child
                    )
            )
            .ToList();


        if (
            expressions.Count == 0
        )
        {
            return "<No Rules>";
        }


        if (
            expressions.Count == 1
        )
        {
            return expressions[0];
        }


        return
            "(" +
            string.Join(
                " AND ",
                expressions
            ) +
            ")";
    }


    // ------------------------------------------------------------------------
    // LOGICAL OR
    // ------------------------------------------------------------------------

    LogicalOrFilter orFilter =
        elementFilter as LogicalOrFilter;

    if (orFilter != null)
    {
        List<string> expressions =
            orFilter
            .GetFilters()
            .Select(
                child =>
                    BuildElementFilterExpression(
                        child
                    )
            )
            .ToList();


        if (
            expressions.Count == 0
        )
        {
            return "<No Rules>";
        }


        if (
            expressions.Count == 1
        )
        {
            return expressions[0];
        }


        return
            "(" +
            string.Join(
                " OR ",
                expressions
            ) +
            ")";
    }


    return
        "<Unsupported Element Filter: " +
        elementFilter.GetType().Name +
        ">";
};


// ============================================================================
// ROOT LOGIC
// ============================================================================

Func<ElementFilter, string> GetRootLogic =
    delegate(ElementFilter elementFilter)
{
    if (elementFilter == null)
        return "NONE";


    if (
        elementFilter is LogicalAndFilter
    )
    {
        return "AND";
    }


    if (
        elementFilter is LogicalOrFilter
    )
    {
        return "OR";
    }


    ElementParameterFilter parameterFilter =
        elementFilter as ElementParameterFilter;


    if (parameterFilter != null)
    {
        List<FilterRule> rules =
            GetParameterFilterRules(
                parameterFilter
            );


        if (
            rules.Count > 1
        )
        {
            // Multiple rules inside ElementParameterFilter are AND conditions.
            return "AND";
        }
    }


    return "NONE";
};


// ============================================================================
// TERMINAL RULE RECORD WRITER
// ============================================================================

Action<
    ParameterFilterElement,
    FilterRule,
    string,
    string,
    int
> AddTerminalRuleRecord =

    delegate(
        ParameterFilterElement filter,
        FilterRule originalRule,
        string treePath,
        string rootLogic,
        int ruleIndex
    )
{
    long filterId =
        GetElementIdInteger(
            filter.Id
        );


    string outerRuleType =
        originalRule != null
        ? originalRule.GetType().Name
        : "Unknown";


    bool isInverted =
        outerRuleType ==
        "FilterInverseRule";


    FilterRule effectiveRule =
        originalRule;


    string innerRuleType =
        "";


    if (isInverted)
    {
        effectiveRule =
            GetInnerRule(
                originalRule
            );


        if (effectiveRule != null)
        {
            innerRuleType =
                effectiveRule.GetType().Name;
        }
    }


    string terminalRuleType =
        effectiveRule != null
        ? effectiveRule.GetType().Name
        : "Unknown";


    ElementId parameterId =
        ElementId.InvalidElementId;


    string parameterName =
        "";

    string parameterType =
        "";

    string parameterSource =
        "Other";

    string parameterGuid =
        "";

    string evaluator =
        "";

    string normalizedOperator =
        "";

    string rawValue =
        "";

    string displayValue =
        "";

    string unit =
        "";

    string expression =
        "";

    string ruleAnalysisStatus =
        "Fully Analysed";

    string rawRuleData =
        "";

    string ruleSignature =
        "";


    // ========================================================================
    // FAILED INVERSE EXTRACTION
    // ========================================================================

    if (
        isInverted &&
        effectiveRule == null
    )
    {
        ruleAnalysisStatus =
            "Not Analysed";

        expression =
            "<Unsupported Inverse Rule>";

        rawRuleData =
            originalRule != null
            ? originalRule.ToString()
            : "";

        ruleSignature =
            "RULE|FilterInverseRule|INNER=UNKNOWN";
    }

    else if (
        effectiveRule != null
    )
    {
        // ====================================================================
        // CATEGORY RULE
        // ====================================================================

        if (
            terminalRuleType ==
            "FilterCategoryRule"
        )
        {
            List<ElementId> categoryIds =
                GetCategoryRuleIds(
                    effectiveRule
                );


            rawValue =
                string.Join(
                    ",",
                    categoryIds
                    .Select(
                        id =>
                            GetElementIdInteger(
                                id
                            )
                            .ToString(
                                CultureInfo.InvariantCulture
                            )
                    )
                );


            displayValue =
                string.Join(
                    "; ",
                    categoryIds
                    .Select(
                        id =>
                        {
                            string name =
                                GetCategoryNameById(
                                    id
                                );

                            return
                                string.IsNullOrWhiteSpace(name)
                                ? GetElementIdInteger(id)
                                    .ToString(
                                        CultureInfo.InvariantCulture
                                    )
                                : name;
                        }
                    )
                );


            evaluator =
                "Category";

            normalizedOperator =
                isInverted
                ? "NotEquals"
                : "Equals";


            expression =
                BuildHumanRuleExpression(
                    originalRule,
                    false
                );


            ruleSignature =
                BuildRuleSignature(
                    originalRule,
                    false
                );
        }

        else
        {
            // =================================================================
            // STANDARD PARAMETER RULE
            // =================================================================

            parameterId =
                GetRuleParameterId(
                    effectiveRule
                );


            parameterName =
                GetParameterName(
                    parameterId
                );

            parameterType =
                GetParameterType(
                    parameterId
                );

            parameterSource =
                GetParameterSource(
                    parameterId
                );

            parameterGuid =
                GetParameterGuid(
                    parameterId
                );


            evaluator =
                GetEvaluatorName(
                    effectiveRule
                );


            normalizedOperator =
                GetNormalizedOperator(
                    evaluator,
                    isInverted
                );


            object rawObject =
                GetRuleValue(
                    effectiveRule
                );


            rawValue =
                RawValueToString(
                    rawObject
                );


            displayValue =
                GetDisplayRuleValue(
                    rawObject
                );


            expression =
                BuildHumanRuleExpression(
                    originalRule,
                    false
                );


            ruleSignature =
                BuildRuleSignature(
                    originalRule,
                    false
                );


            // ---------------------------------------------------------------
            // Preserve epsilon information for numeric double rules.
            // ---------------------------------------------------------------

            object epsilon =
                GetRuleEpsilon(
                    effectiveRule
                );


            StringBuilder technical =
                new StringBuilder();


            technical.Append(
                "RuleType="
            );

            technical.Append(
                outerRuleType
            );


            if (
                !string.IsNullOrWhiteSpace(
                    innerRuleType
                )
            )
            {
                technical.Append(
                    "; InnerRuleType="
                );

                technical.Append(
                    innerRuleType
                );
            }


            technical.Append(
                "; Evaluator="
            );

            technical.Append(
                evaluator
            );


            technical.Append(
                "; ParameterKey="
            );

            technical.Append(
                GetStableParameterKey(
                    parameterId
                )
            );


            technical.Append(
                "; RawValue="
            );

            technical.Append(
                rawValue
            );


            if (epsilon != null)
            {
                technical.Append(
                    "; Epsilon="
                );

                technical.Append(
                    RawValueToString(
                        epsilon
                    )
                );
            }


            rawRuleData =
                technical.ToString();
        }


        // ====================================================================
        // SUPPORTED / UNSUPPORTED TERMINAL RULE STATUS
        // ====================================================================

        if (
            !IsSupportedRuleType(
                terminalRuleType
            )
        )
        {
            ruleAnalysisStatus =
                "Partially Analysed";


            if (
                string.IsNullOrWhiteSpace(
                    rawRuleData
                )
            )
            {
                rawRuleData =
                    effectiveRule.ToString();
            }
        }
    }


    // ========================================================================
    // DEFAULT RAW RULE DATA FOR CATEGORY / SIMPLE RULES
    // ========================================================================

    if (
        string.IsNullOrWhiteSpace(
            rawRuleData
        )
    )
    {
        StringBuilder technical =
            new StringBuilder();


        technical.Append(
            "RuleType="
        );

        technical.Append(
            outerRuleType
        );


        if (
            !string.IsNullOrWhiteSpace(
                innerRuleType
            )
        )
        {
            technical.Append(
                "; InnerRuleType="
            );

            technical.Append(
                innerRuleType
            );
        }


        if (
            !string.IsNullOrWhiteSpace(
                evaluator
            )
        )
        {
            technical.Append(
                "; Evaluator="
            );

            technical.Append(
                evaluator
            );
        }


        if (
            !string.IsNullOrWhiteSpace(
                rawValue
            )
        )
        {
            technical.Append(
                "; RawValue="
            );

            technical.Append(
                rawValue
            );
        }


        rawRuleData =
            technical.ToString();
    }


    // ========================================================================
    // CREATE ROW
    // ========================================================================

    Dictionary<string, object> record =
        new Dictionary<string, object>(
            StringComparer.OrdinalIgnoreCase
        );


    record["Project ID"] =
        projectId;

    record["Filter ID"] =
        filterId;

    record["Filter Name"] =
        filter.Name ?? "";

    record["Rule Index"] =
        ruleIndex;

    record["Tree Path"] =
        treePath;

    record["Root Logic"] =
        rootLogic;

    record["Rule Type"] =
        outerRuleType;

    record["Inner Rule Type"] =
        innerRuleType;

    record["Is Inverted"] =
        isInverted;

    record["Parameter ID"] =
        parameterId != null &&
        parameterId != ElementId.InvalidElementId
        ? GetElementIdInteger(parameterId)
        : 0;

    record["Parameter Name"] =
        parameterName;

    record["Parameter Type"] =
        parameterType;

    record["Parameter Source"] =
        parameterSource;

    record["Parameter GUID"] =
        parameterGuid;

    record["Evaluator"] =
        evaluator;

    record["Operator"] =
        normalizedOperator;

    record["Raw Value"] =
        rawValue;

    record["Display Value"] =
        displayValue;

    record["Unit"] =
        unit;

    record["Rule Expression"] =
        expression;

    record["Rule Analysis Status"] =
        ruleAnalysisStatus;

    record["Raw Rule Data"] =
        rawRuleData;

    record["Rule Signature"] =
        ruleSignature;


    ruleRecords.Add(
        record
    );
};


// ============================================================================
// TREE WALKER
//
// Tree path examples:
//
//     AND[1]
//     AND[2]
//     OR[3]/AND[1]
//     OR[3]/AND[2]
//
// ElementParameterFilter containing multiple rules behaves as an AND node.
// ============================================================================

Action<
    ParameterFilterElement,
    ElementFilter,
    string,
    string,
    int[]
> WalkRuleTree = null;


WalkRuleTree =
    delegate(
        ParameterFilterElement filter,
        ElementFilter elementFilter,
        string currentPath,
        string rootLogic,
        int[] ruleCounter
    )
{
    if (
        filter == null ||
        elementFilter == null
    )
    {
        return;
    }


    // ------------------------------------------------------------------------
    // ELEMENT PARAMETER FILTER
    // ------------------------------------------------------------------------

    ElementParameterFilter parameterFilter =
        elementFilter as ElementParameterFilter;


    if (parameterFilter != null)
    {
        List<FilterRule> rules =
            GetParameterFilterRules(
                parameterFilter
            );


        if (
            rules.Count == 0
        )
        {
            return;
        }


        for (
            int i = 0;
            i < rules.Count;
            i++
        )
        {
            ruleCounter[0]++;


            string rulePath =
                currentPath;


            if (
                rules.Count > 1
            )
            {
                string localSegment =
                    "AND[" +
                    (i + 1)
                    .ToString(
                        CultureInfo.InvariantCulture
                    ) +
                    "]";


                rulePath =
                    string.IsNullOrWhiteSpace(
                        currentPath
                    )
                    ? localSegment
                    : currentPath +
                      "/" +
                      localSegment;
            }
            else if (
                string.IsNullOrWhiteSpace(
                    rulePath
                )
            )
            {
                rulePath =
                    "RULE[1]";
            }


            AddTerminalRuleRecord(
                filter,
                rules[i],
                rulePath,
                rootLogic,
                ruleCounter[0]
            );
        }


        return;
    }


    // ------------------------------------------------------------------------
    // LOGICAL AND
    // ------------------------------------------------------------------------

    LogicalAndFilter andFilter =
        elementFilter as LogicalAndFilter;


    if (andFilter != null)
    {
        IList<ElementFilter> children =
            andFilter.GetFilters();


        for (
            int i = 0;
            i < children.Count;
            i++
        )
        {
            string segment =
                "AND[" +
                (i + 1)
                .ToString(
                    CultureInfo.InvariantCulture
                ) +
                "]";


            string childPath =
                string.IsNullOrWhiteSpace(
                    currentPath
                )
                ? segment
                : currentPath +
                  "/" +
                  segment;


            WalkRuleTree(
                filter,
                children[i],
                childPath,
                rootLogic,
                ruleCounter
            );
        }


        return;
    }


    // ------------------------------------------------------------------------
    // LOGICAL OR
    // ------------------------------------------------------------------------

    LogicalOrFilter orFilter =
        elementFilter as LogicalOrFilter;


    if (orFilter != null)
    {
        IList<ElementFilter> children =
            orFilter.GetFilters();


        for (
            int i = 0;
            i < children.Count;
            i++
        )
        {
            string segment =
                "OR[" +
                (i + 1)
                .ToString(
                    CultureInfo.InvariantCulture
                ) +
                "]";


            string childPath =
                string.IsNullOrWhiteSpace(
                    currentPath
                )
                ? segment
                : currentPath +
                  "/" +
                  segment;


            WalkRuleTree(
                filter,
                children[i],
                childPath,
                rootLogic,
                ruleCounter
            );
        }


        return;
    }


    // Unknown ElementFilter is deliberately preserved at filter level.
};


// ============================================================================
// PROCESS EVERY PARAMETER FILTER
// ============================================================================

foreach (
    ParameterFilterElement filter
    in projectFilters
)
{
    long filterId =
        GetElementIdInteger(
            filter.Id
        );


    Dictionary<string, object> filterRecord;


    if (
        !projectFilterById.TryGetValue(
            filterId,
            out filterRecord
        )
    )
    {
        continue;
    }


    ElementFilter rootFilter =
        null;


    try
    {
        rootFilter =
            filter.GetElementFilter();
    }
    catch
    {
        rootFilter =
            null;
    }


    // ========================================================================
    // NO-RULE FILTER
    //
    // This is legitimate, not an extraction failure.
    // ========================================================================

    if (rootFilter == null)
    {
        filterRecord["Rule Count"] =
            0;

        filterRecord["Rule Analysis Status"] =
            "Fully Analysed";

        filterRecord["Rule Expression"] =
            "<No Rules>";

        continue;
    }


    string rootLogic =
        GetRootLogic(
            rootFilter
        );


    string fullExpression =
        BuildElementFilterExpression(
            rootFilter
        );


    int rulesBefore =
        ruleRecords.Count;


    int[] ruleCounter =
    {
        0
    };


    WalkRuleTree(
        filter,
        rootFilter,
        "",
        rootLogic,
        ruleCounter
    );


    int rulesAfter =
        ruleRecords.Count;


    int terminalRuleCount =
        rulesAfter -
        rulesBefore;


    filterRecord["Rule Count"] =
        terminalRuleCount;

    filterRecord["Rule Expression"] =
        fullExpression;


    // ========================================================================
    // DETERMINE FILTER-LEVEL RULE ANALYSIS STATUS
    // ========================================================================

    List<Dictionary<string, object>> thisFilterRules =
        ruleRecords
        .Skip(
            rulesBefore
        )
        .Take(
            terminalRuleCount
        )
        .ToList();


    bool anyNotAnalysed =
        thisFilterRules.Any(
            r =>
                Convert.ToString(
                    r["Rule Analysis Status"]
                ) ==
                "Not Analysed"
        );


    bool anyPartiallyAnalysed =
        thisFilterRules.Any(
            r =>
                Convert.ToString(
                    r["Rule Analysis Status"]
                ) ==
                "Partially Analysed"
        );


    string canonicalRuleSignature =
        BuildFilterRuleSignature(
            filter
        );


    bool unsupportedTree =
        canonicalRuleSignature.IndexOf(
            "UNSUPPORTED_",
            StringComparison.Ordinal
        ) >= 0 ||
        canonicalRuleSignature.IndexOf(
            "UNABLE_TO_ANALYSE",
            StringComparison.Ordinal
        ) >= 0 ||
        canonicalRuleSignature.IndexOf(
            "INNER=UNKNOWN",
            StringComparison.Ordinal
        ) >= 0;


    if (
        anyNotAnalysed
    )
    {
        filterRecord["Rule Analysis Status"] =
            "Not Analysed";
    }

    else if (
        anyPartiallyAnalysed ||
        unsupportedTree
    )
    {
        filterRecord["Rule Analysis Status"] =
            "Partially Analysed";
    }

    else
    {
        filterRecord["Rule Analysis Status"] =
            "Fully Analysed";
    }
}


// ============================================================================
// RULE SUMMARY COUNTS
// ============================================================================

int totalTerminalRuleCount =
    ruleRecords.Count;


int invertedRuleCount =
    ruleRecords.Count(
        r =>
            Convert.ToBoolean(
                r["Is Inverted"]
            )
    );


int partiallyAnalysedRuleCount =
    ruleRecords.Count(
        r =>
            Convert.ToString(
                r["Rule Analysis Status"]
            ) ==
            "Partially Analysed"
    );


int notAnalysedRuleCount =
    ruleRecords.Count(
        r =>
            Convert.ToString(
                r["Rule Analysis Status"]
            ) ==
            "Not Analysed"
    );


int filtersWithNoRules =
    projectFilterRecords.Count(
        r =>
            Convert.ToInt32(
                r["Rule Count"]
            ) == 0
    );


// ============================================================================
// BLOCK 7 COMPLETE
//
// Available:
//
//     ruleRecords
//
// Each Rules row contains all 23 locked fields.
//
// Filter master records now also contain:
//
//     Rule Count
//     Rule Analysis Status
//     Rule Expression
//
// No-rule filters are represented as:
//
//     Rule Count             = 0
//     Rule Analysis Status   = Fully Analysed
//     Rule Expression        = <No Rules>
//
// ============================================================================

// ============================================================================
// BLOCK 8
// FILTER USAGE ANALYSIS
//
// Creates one row per:
//
//     Filter x Usage Instance
//
// Usage Type:
//
//     Direct
//     View Template
//
// IMPORTANT:
// A filter applied to a template counts once against that template.
// We do NOT create additional inherited rows for every view using the template.
//
// Locked Usage columns:
//
// 1. Project ID
// 2. Filter ID
// 3. Filter Name
// 4. Usage Type
// 5. View ID
// 6. View Unique ID
// 7. View Name
// 8. View Type
// 9. Is View Template
//
// ============================================================================


// ============================================================================
// USAGE DATASET
// ============================================================================

List<Dictionary<string, object>> usageRecords =
    new List<Dictionary<string, object>>();


// ============================================================================
// COLLECT ALL VIEWS
//
// Includes view templates.
// Excludes element types automatically because View is an Element.
// ============================================================================

List<View> allViews =
    new FilteredElementCollector(doc)
        .OfClass(
            typeof(View)
        )
        .Cast<View>()
        .OrderBy(
            v => v.Name,
            StringComparer.OrdinalIgnoreCase
        )
        .ThenBy(
            v => GetElementIdInteger(v.Id)
        )
        .ToList();


// ============================================================================
// PROCESS EACH VIEW / TEMPLATE
// ============================================================================

foreach (
    View view
    in allViews
)
{
    if (view == null)
        continue;


    ICollection<ElementId> viewFilterIds =
        null;


    try
    {
        viewFilterIds =
            view.GetFilters();
    }
    catch
    {
        viewFilterIds =
            null;
    }


    if (
        viewFilterIds == null ||
        viewFilterIds.Count == 0
    )
    {
        continue;
    }


    bool isTemplate =
        false;


    try
    {
        isTemplate =
            view.IsTemplate;
    }
    catch
    {
        isTemplate =
            false;
    }


    string usageType =
        isTemplate
        ? "View Template"
        : "Direct";


    string viewName =
        view.Name ?? "";


    string viewType =
        "";


    try
    {
        viewType =
            view.ViewType.ToString();
    }
    catch
    {
        viewType =
            "";
    }


    long viewId =
        GetElementIdInteger(
            view.Id
        );


    string viewUniqueId =
        view.UniqueId ?? "";


    // ========================================================================
    // ONE ROW PER FILTER USED BY THIS VIEW/TEMPLATE
    // ========================================================================

    foreach (
        ElementId filterElementId
        in viewFilterIds
    )
    {
        if (
            filterElementId == null ||
            filterElementId == ElementId.InvalidElementId
        )
        {
            continue;
        }


        long filterId =
            GetElementIdInteger(
                filterElementId
            );


        Dictionary<string, object> filterRecord;


        // We only care about ParameterFilterElement filters that were already
        // collected in Block 5.
        if (
            !projectFilterById.TryGetValue(
                filterId,
                out filterRecord
            )
        )
        {
            continue;
        }


        Dictionary<string, object> usageRecord =
            new Dictionary<string, object>(
                StringComparer.OrdinalIgnoreCase
            );


        usageRecord["Project ID"] =
            projectId;

        usageRecord["Filter ID"] =
            filterId;

        usageRecord["Filter Name"] =
            Convert.ToString(
                filterRecord["Filter Name"]
            );

        usageRecord["Usage Type"] =
            usageType;

        usageRecord["View ID"] =
            viewId;

        usageRecord["View Unique ID"] =
            viewUniqueId;

        usageRecord["View Name"] =
            viewName;

        usageRecord["View Type"] =
            viewType;

        usageRecord["Is View Template"] =
            isTemplate;


        usageRecords.Add(
            usageRecord
        );
    }
}


// ============================================================================
// POPULATE FILTER MASTER USAGE COUNTS
// ============================================================================

foreach (
    Dictionary<string, object> filterRecord
    in projectFilterRecords
)
{
    long filterId =
        Convert.ToInt64(
            filterRecord["Filter ID"],
            CultureInfo.InvariantCulture
        );


    int directViewCount =
        usageRecords.Count(
            r =>
                Convert.ToInt64(
                    r["Filter ID"],
                    CultureInfo.InvariantCulture
                ) == filterId
                &&
                string.Equals(
                    Convert.ToString(
                        r["Usage Type"]
                    ),
                    "Direct",
                    StringComparison.Ordinal
                )
        );


    int viewTemplateCount =
        usageRecords.Count(
            r =>
                Convert.ToInt64(
                    r["Filter ID"],
                    CultureInfo.InvariantCulture
                ) == filterId
                &&
                string.Equals(
                    Convert.ToString(
                        r["Usage Type"]
                    ),
                    "View Template",
                    StringComparison.Ordinal
                )
        );


    int totalUsageCount =
        directViewCount +
        viewTemplateCount;


    filterRecord["Direct View Count"] =
        directViewCount;

    filterRecord["View Template Count"] =
        viewTemplateCount;

    filterRecord["Total Usage Count"] =
        totalUsageCount;

    filterRecord["Usage Status"] =
        totalUsageCount > 0
        ? "Used"
        : "Unused";
}


// ============================================================================
// USAGE SUMMARY COUNTS
// ============================================================================

int directUsageRowCount =
    usageRecords.Count(
        r =>
            Convert.ToString(
                r["Usage Type"]
            ) == "Direct"
    );


int templateUsageRowCount =
    usageRecords.Count(
        r =>
            Convert.ToString(
                r["Usage Type"]
            ) == "View Template"
    );


int usedFilterCount =
    projectFilterRecords.Count(
        r =>
            Convert.ToString(
                r["Usage Status"]
            ) == "Used"
    );


int unusedFilterCount =
    projectFilterRecords.Count(
        r =>
            Convert.ToString(
                r["Usage Status"]
            ) == "Unused"
    );


// ============================================================================
// OPTIONAL SANITY CHECK
//
// Total Usage Count should equal the number of Usage rows for that filter.
// ============================================================================

foreach (
    Dictionary<string, object> filterRecord
    in projectFilterRecords
)
{
    long filterId =
        Convert.ToInt64(
            filterRecord["Filter ID"],
            CultureInfo.InvariantCulture
        );


    int expectedCount =
        usageRecords.Count(
            r =>
                Convert.ToInt64(
                    r["Filter ID"],
                    CultureInfo.InvariantCulture
                ) == filterId
        );


    int storedCount =
        Convert.ToInt32(
            filterRecord["Total Usage Count"],
            CultureInfo.InvariantCulture
        );


    if (
        expectedCount !=
        storedCount
    )
    {
        TaskDialog.Show(
            "Filter Audit",
            "Usage validation failed for filter:\n\n" +

            Convert.ToString(
                filterRecord["Filter Name"]
            ) +

            "\n\nExpected usage rows: " +
            expectedCount +

            "\nStored usage count: " +
            storedCount
        );

        return;
    }
}


// ============================================================================
// BLOCK 8 COMPLETE
//
// Available:
//
//     usageRecords
//
// Each row contains:
//
//     Project ID
//     Filter ID
//     Filter Name
//     Usage Type
//     View ID
//     View Unique ID
//     View Name
//     View Type
//     Is View Template
//
// Filter master records now contain:
//
//     Usage Status
//     Direct View Count
//     View Template Count
//     Total Usage Count
//
// Usage interpretation:
//
//     Direct
//         Filter applied directly to a non-template view.
//
//     View Template
//         Filter applied to the template itself.
//
// A template used by 100 views still contributes ONE usage instance.
//
// ============================================================================

// ============================================================================
// BLOCK 9 V2
// DUPLICATE + LOGICAL OVERLAP ANALYSIS
//
// Key changes from V1:
//
// 1. Contradictory AND conditions are detected and treated as mutually
//    exclusive.
//
// 2. Routine Office <-> Office relationships are suppressed.
//    The approved office library is treated as the baseline, not as something
//    requiring PM review.
//
// 3. Sharing one terminal rule is NOT enough to claim logical overlap.
//
// 4. "Category Subset" now requires identical complete rules plus a true
//    category subset relationship.
//
// 5. "Related" is deliberately much stricter.
//
// Output classifications:
//
//     Exact Duplicate
//     Same Rules / Different Categories
//     Category Subset
//     Potential Logical Overlap
//     Related
//
// Mutually exclusive pairs are detected internally but NOT written to the
// Overlaps worksheet.
//
// ============================================================================


// ============================================================================
// OVERLAP DATASET
// ============================================================================

List<Dictionary<string, object>> overlapRecords =
    new List<Dictionary<string, object>>();


// ============================================================================
// FILTER CATEGORY SET
// ============================================================================

Func<long, HashSet<long>> GetFilterCategoryIds =
    delegate(long filterId)
{
    return new HashSet<long>(
        categoryRecords
            .Where(
                r =>
                    Convert.ToInt64(
                        r["Filter ID"],
                        CultureInfo.InvariantCulture
                    ) == filterId
            )
            .Select(
                r =>
                    Convert.ToInt64(
                        r["Category ID"],
                        CultureInfo.InvariantCulture
                    )
            )
    );
};


// ============================================================================
// FILTER RULE ROWS
// ============================================================================

Func<long, List<Dictionary<string, object>>> GetFilterRuleRows =
    delegate(long filterId)
{
    return ruleRecords
        .Where(
            r =>
                Convert.ToInt64(
                    r["Filter ID"],
                    CultureInfo.InvariantCulture
                ) == filterId
        )
        .OrderBy(
            r =>
                Convert.ToInt32(
                    r["Rule Index"],
                    CultureInfo.InvariantCulture
                )
        )
        .ToList();
};


// ============================================================================
// PARAMETER KEY FROM CANONICAL RULE SIGNATURE
//
// Example:
//
//     RULE|FilterIntegerRule|PARAM=GUID:...|OP=Equals|VALUE=1
//
// ============================================================================

Func<string, string> GetParameterKeyFromRuleSignature =
    delegate(string signature)
{
    if (
        string.IsNullOrWhiteSpace(
            signature
        )
    )
    {
        return "";
    }


    const string marker =
        "|PARAM=";


    int start =
        signature.IndexOf(
            marker,
            StringComparison.Ordinal
        );


    if (start < 0)
        return "";


    start +=
        marker.Length;


    int end =
        signature.IndexOf(
            "|",
            start,
            StringComparison.Ordinal
        );


    if (end < 0)
    {
        end =
            signature.Length;
    }


    return signature.Substring(
        start,
        end - start
    );
};


// ============================================================================
// PARAMETER SET
// ============================================================================

Func<long, HashSet<string>> GetFilterParameterKeys =
    delegate(long filterId)
{
    HashSet<string> result =
        new HashSet<string>(
            StringComparer.Ordinal
        );


    foreach (
        Dictionary<string, object> row
        in GetFilterRuleRows(filterId)
    )
    {
        string signature =
            Convert.ToString(
                row["Rule Signature"]
            );


        string key =
            GetParameterKeyFromRuleSignature(
                signature
            );


        if (
            !string.IsNullOrWhiteSpace(
                key
            )
        )
        {
            result.Add(
                key
            );
        }
    }


    return result;
};


// ============================================================================
// TERMINAL RULE SIGNATURE SET
// ============================================================================

Func<long, HashSet<string>> GetTerminalRuleSignatureSet =
    delegate(long filterId)
{
    return new HashSet<string>(
        GetFilterRuleRows(filterId)
            .Select(
                r =>
                    Convert.ToString(
                        r["Rule Signature"]
                    )
            )
            .Where(
                s =>
                    !string.IsNullOrWhiteSpace(s)
            ),
        StringComparer.Ordinal
    );
};


// ============================================================================
// CATEGORY RELATIONSHIP
// ============================================================================

Func<HashSet<long>, HashSet<long>, string> GetCategoryRelationship =
    delegate(
        HashSet<long> a,
        HashSet<long> b
    )
{
    if (
        a.SetEquals(b)
    )
    {
        return "Exact";
    }


    if (
        a.Count > 0 &&
        a.IsSubsetOf(b)
    )
    {
        return "A subset of B";
    }


    if (
        b.Count > 0 &&
        b.IsSubsetOf(a)
    )
    {
        return "B subset of A";
    }


    if (
        a.Overlaps(b)
    )
    {
        return "Intersects";
    }


    return "None";
};


// ============================================================================
// PARAMETER RELATIONSHIP
// ============================================================================

Func<HashSet<string>, HashSet<string>, string> GetParameterRelationship =
    delegate(
        HashSet<string> a,
        HashSet<string> b
    )
{
    if (
        a.Count == 0 &&
        b.Count == 0
    )
    {
        return "None";
    }


    if (
        a.SetEquals(b)
    )
    {
        return "Exact";
    }


    if (
        a.Count > 0 &&
        a.IsSubsetOf(b)
    )
    {
        return "A subset of B";
    }


    if (
        b.Count > 0 &&
        b.IsSubsetOf(a)
    )
    {
        return "B subset of A";
    }


    if (
        a.Overlaps(b)
    )
    {
        return "Intersects";
    }


    return "None";
};


// ============================================================================
// SAFE NUMERIC PARSER
// ============================================================================

Func<object, double?> TryGetNumericValue =
    delegate(object value)
{
    if (value == null)
        return null;


    double parsed;


    if (
        double.TryParse(
            Convert.ToString(
                value,
                CultureInfo.InvariantCulture
            ),
            NumberStyles.Float |
            NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out parsed
        )
    )
    {
        return parsed;
    }


    return null;
};


// ============================================================================
// TEXT COMPARISON
//
// Revit string filtering is normally case-insensitive from the user's point
// of view, so overlap compatibility uses OrdinalIgnoreCase.
//
// Canonical signatures remain untouched.
// ============================================================================

StringComparison overlapStringComparison =
    StringComparison.OrdinalIgnoreCase;


// ============================================================================
// DOES A TEXT VALUE SATISFY A CONDITION?
//
// Used when one side has an Equals condition.
// ============================================================================

Func<string, string, string, bool?> DoesTextValueSatisfy =
    delegate(
        string actualValue,
        string op,
        string conditionValue
    )
{
    actualValue =
        actualValue ?? "";

    conditionValue =
        conditionValue ?? "";


    switch (op)
    {
        case "Equals":

            return string.Equals(
                actualValue,
                conditionValue,
                overlapStringComparison
            );


        case "NotEquals":

            return !string.Equals(
                actualValue,
                conditionValue,
                overlapStringComparison
            );


        case "Contains":

            return actualValue.IndexOf(
                conditionValue,
                overlapStringComparison
            ) >= 0;


        case "DoesNotContain":

            return actualValue.IndexOf(
                conditionValue,
                overlapStringComparison
            ) < 0;


        case "BeginsWith":

            return actualValue.StartsWith(
                conditionValue,
                overlapStringComparison
            );


        case "DoesNotBeginWith":

            return !actualValue.StartsWith(
                conditionValue,
                overlapStringComparison
            );


        case "EndsWith":

            return actualValue.EndsWith(
                conditionValue,
                overlapStringComparison
            );


        case "DoesNotEndWith":

            return !actualValue.EndsWith(
                conditionValue,
                overlapStringComparison
            );
    }


    return null;
};


// ============================================================================
// DOES A NUMERIC VALUE SATISFY A CONDITION?
// ============================================================================

Func<double, string, double, bool?> DoesNumericValueSatisfy =
    delegate(
        double actualValue,
        string op,
        double conditionValue
    )
{
    switch (op)
    {
        case "Equals":

            return Math.Abs(
                actualValue -
                conditionValue
            ) < 1e-12;


        case "NotEquals":

            return Math.Abs(
                actualValue -
                conditionValue
            ) >= 1e-12;


        case "GreaterThan":

            return actualValue >
                   conditionValue;


        case "GreaterThanOrEqual":

            return actualValue >=
                   conditionValue;


        case "LessThan":

            return actualValue <
                   conditionValue;


        case "LessThanOrEqual":

            return actualValue <=
                   conditionValue;
    }


    return null;
};


// ============================================================================
// DETECT CONTRADICTION BETWEEN TWO TERMINAL RULES
//
// IMPORTANT:
//
// This function only reports TRUE where the conditions are safely known to
// be incompatible.
//
// It does NOT guess.
//
// Examples:
//
//     RaisedFloor = 0
//     RaisedFloor = 1
//
//         -> contradictory
//
//     Parameter has value
//     Parameter has no value
//
//         -> contradictory
//
//     Height > 5000
//     Height < 3000
//
//         -> contradictory
//
//     Type Name contains FAS_0
//     Type Name contains FAS_1
//
//         -> NOT automatically contradictory.
//            A string could technically contain both.
//
// ============================================================================

Func<
    Dictionary<string, object>,
    Dictionary<string, object>,
    bool
> AreTerminalRulesContradictory =

    delegate(
        Dictionary<string, object> rowA,
        Dictionary<string, object> rowB
    )
{
    string signatureA =
        Convert.ToString(
            rowA["Rule Signature"]
        );

    string signatureB =
        Convert.ToString(
            rowB["Rule Signature"]
        );


    string parameterA =
        GetParameterKeyFromRuleSignature(
            signatureA
        );

    string parameterB =
        GetParameterKeyFromRuleSignature(
            signatureB
        );


    if (
        string.IsNullOrWhiteSpace(
            parameterA
        ) ||
        string.IsNullOrWhiteSpace(
            parameterB
        ) ||
        !string.Equals(
            parameterA,
            parameterB,
            StringComparison.Ordinal
        )
    )
    {
        return false;
    }


    string opA =
        Convert.ToString(
            rowA["Operator"]
        );

    string opB =
        Convert.ToString(
            rowB["Operator"]
        );


    string valueA =
        Convert.ToString(
            rowA["Raw Value"]
        );

    string valueB =
        Convert.ToString(
            rowB["Raw Value"]
        );


    // ========================================================================
    // HAS VALUE / HAS NO VALUE
    // ========================================================================

    if (
        (
            opA == "HasValue" &&
            opB == "HasNoValue"
        )
        ||
        (
            opA == "HasNoValue" &&
            opB == "HasValue"
        )
    )
    {
        return true;
    }


    // ========================================================================
    // EXACT EQUALITY / INEQUALITY
    // ========================================================================

    if (
        opA == "Equals" &&
        opB == "Equals"
    )
    {
        return !string.Equals(
            valueA,
            valueB,
            overlapStringComparison
        );
    }


    if (
        opA == "Equals" &&
        opB == "NotEquals"
    )
    {
        return string.Equals(
            valueA,
            valueB,
            overlapStringComparison
        );
    }


    if (
        opB == "Equals" &&
        opA == "NotEquals"
    )
    {
        return string.Equals(
            valueA,
            valueB,
            overlapStringComparison
        );
    }


    // ========================================================================
    // STRING CONTRADICTIONS
    //
    // Contains X vs DoesNotContain X, etc.
    // ========================================================================

    if (
        opA == "Contains" &&
        opB == "DoesNotContain" &&
        string.Equals(
            valueA,
            valueB,
            overlapStringComparison
        )
    )
    {
        return true;
    }


    if (
        opB == "Contains" &&
        opA == "DoesNotContain" &&
        string.Equals(
            valueA,
            valueB,
            overlapStringComparison
        )
    )
    {
        return true;
    }


    if (
        opA == "BeginsWith" &&
        opB == "DoesNotBeginWith" &&
        string.Equals(
            valueA,
            valueB,
            overlapStringComparison
        )
    )
    {
        return true;
    }


    if (
        opB == "BeginsWith" &&
        opA == "DoesNotBeginWith" &&
        string.Equals(
            valueA,
            valueB,
            overlapStringComparison
        )
    )
    {
        return true;
    }


    if (
        opA == "EndsWith" &&
        opB == "DoesNotEndWith" &&
        string.Equals(
            valueA,
            valueB,
            overlapStringComparison
        )
    )
    {
        return true;
    }


    if (
        opB == "EndsWith" &&
        opA == "DoesNotEndWith" &&
        string.Equals(
            valueA,
            valueB,
            overlapStringComparison
        )
    )
    {
        return true;
    }


    // ========================================================================
    // ONE SIDE EQUALS A STRING
    //
    // If the actual exact value cannot satisfy the other condition, the
    // complete pair is contradictory.
    // ========================================================================

    if (
        opA == "Equals"
    )
    {
        bool? satisfiesB =
            DoesTextValueSatisfy(
                valueA,
                opB,
                valueB
            );


        if (
            satisfiesB.HasValue &&
            !satisfiesB.Value
        )
        {
            return true;
        }
    }


    if (
        opB == "Equals"
    )
    {
        bool? satisfiesA =
            DoesTextValueSatisfy(
                valueB,
                opA,
                valueA
            );


        if (
            satisfiesA.HasValue &&
            !satisfiesA.Value
        )
        {
            return true;
        }
    }


    // ========================================================================
    // NUMERIC CONTRADICTIONS
    // ========================================================================

    double? numericA =
        TryGetNumericValue(
            valueA
        );

    double? numericB =
        TryGetNumericValue(
            valueB
        );


    if (
        numericA.HasValue &&
        numericB.HasValue
    )
    {
        double a =
            numericA.Value;

        double b =
            numericB.Value;


        // --------------------------------------------------------------------
        // Exact value against numeric condition
        // --------------------------------------------------------------------

        if (
            opA == "Equals"
        )
        {
            bool? satisfiesB =
                DoesNumericValueSatisfy(
                    a,
                    opB,
                    b
                );


            if (
                satisfiesB.HasValue &&
                !satisfiesB.Value
            )
            {
                return true;
            }
        }


        if (
            opB == "Equals"
        )
        {
            bool? satisfiesA =
                DoesNumericValueSatisfy(
                    b,
                    opA,
                    a
                );


            if (
                satisfiesA.HasValue &&
                !satisfiesA.Value
            )
            {
                return true;
            }
        }


        // --------------------------------------------------------------------
        // LOWER BOUND vs UPPER BOUND
        // --------------------------------------------------------------------

        bool aLower =
            opA == "GreaterThan" ||
            opA == "GreaterThanOrEqual";

        bool bLower =
            opB == "GreaterThan" ||
            opB == "GreaterThanOrEqual";


        bool aUpper =
            opA == "LessThan" ||
            opA == "LessThanOrEqual";

        bool bUpper =
            opB == "LessThan" ||
            opB == "LessThanOrEqual";


        if (
            aLower &&
            bUpper
        )
        {
            if (a > b)
                return true;


            if (
                Math.Abs(a - b) < 1e-12 &&
                (
                    opA == "GreaterThan" ||
                    opB == "LessThan"
                )
            )
            {
                return true;
            }
        }


        if (
            bLower &&
            aUpper
        )
        {
            if (b > a)
                return true;


            if (
                Math.Abs(a - b) < 1e-12 &&
                (
                    opB == "GreaterThan" ||
                    opA == "LessThan"
                )
            )
            {
                return true;
            }
        }
    }


    return false;
};


// ============================================================================
// FILTER IS SAFE FOR COMPLETE AND-CONDITION COMPARISON
//
// OR trees are excluded from V2 logical compatibility analysis.
// They remain available for exact canonical definition comparison.
//
// Category rules inside the rule tree are also excluded from logical overlap
// inference because they need separate Boolean treatment.
//
// ============================================================================

Func<
    Dictionary<string, object>,
    List<Dictionary<string, object>>,
    bool
> IsSafeAndComparable =

    delegate(
        Dictionary<string, object> filterRecord,
        List<Dictionary<string, object>> rows
    )
{
    if (
        Convert.ToString(
            filterRecord["Rule Analysis Status"]
        ) !=
        "Fully Analysed"
    )
    {
        return false;
    }


    string fullSignature =
        Convert.ToString(
            filterRecord["_RuleSignature"]
        );


    if (
        fullSignature.IndexOf(
            "OR(",
            StringComparison.Ordinal
        ) >= 0
    )
    {
        return false;
    }


    foreach (
        Dictionary<string, object> row
        in rows
    )
    {
        string ruleType =
            Convert.ToString(
                row["Rule Type"]
            );


        string innerType =
            Convert.ToString(
                row["Inner Rule Type"]
            );


        if (
            ruleType ==
            "FilterCategoryRule"
            ||
            innerType ==
            "FilterCategoryRule"
        )
        {
            return false;
        }
    }


    return true;
};


// ============================================================================
// COMPLETE FILTER CONTRADICTION TEST
//
// For two AND-based filters:
//
// If ANY condition in A contradicts ANY condition in B on the same parameter,
// then no element can satisfy both complete filters.
//
// ============================================================================

Func<
    List<Dictionary<string, object>>,
    List<Dictionary<string, object>>,
    bool
> AreFiltersMutuallyExclusive =

    delegate(
        List<Dictionary<string, object>> rowsA,
        List<Dictionary<string, object>> rowsB
    )
{
    foreach (
        Dictionary<string, object> rowA
        in rowsA
    )
    {
        foreach (
            Dictionary<string, object> rowB
            in rowsB
        )
        {
            if (
                AreTerminalRulesContradictory(
                    rowA,
                    rowB
                )
            )
            {
                return true;
            }
        }
    }


    return false;
};


// ============================================================================
// COUNT IDENTICAL TERMINAL CONDITIONS
// ============================================================================

Func<
    HashSet<string>,
    HashSet<string>,
    int
> CountSharedTerminalRules =

    delegate(
        HashSet<string> a,
        HashSet<string> b
    )
{
    int count =
        0;


    foreach (
        string rule
        in a
    )
    {
        if (
            b.Contains(
                rule
            )
        )
        {
            count++;
        }
    }


    return count;
};


// ============================================================================
// RULE RELATIONSHIP
// ============================================================================

Func<
    string,
    string,
    HashSet<string>,
    HashSet<string>,
    bool,
    string
> GetRuleRelationshipV2 =

    delegate(
        string fullRuleSignatureA,
        string fullRuleSignatureB,
        HashSet<string> terminalRulesA,
        HashSet<string> terminalRulesB,
        bool mutuallyExclusive
    )
{
    if (
        string.Equals(
            fullRuleSignatureA,
            fullRuleSignatureB,
            StringComparison.Ordinal
        )
    )
    {
        return "Exact";
    }


    if (
        mutuallyExclusive
    )
    {
        return "Mutually Exclusive";
    }


    if (
        terminalRulesA.Count > 0 &&
        terminalRulesA.IsProperSubsetOf(
            terminalRulesB
        )
    )
    {
        return "A subset of B";
    }


    if (
        terminalRulesB.Count > 0 &&
        terminalRulesB.IsProperSubsetOf(
            terminalRulesA
        )
    )
    {
        return "B subset of A";
    }


    int shared =
        CountSharedTerminalRules(
            terminalRulesA,
            terminalRulesB
        );


    if (shared > 0)
    {
        return "Compatible / Shared Conditions";
    }


    return "Related but not comparable";
};


// ============================================================================
// FRIENDLY REASON
// ============================================================================

Func<
    string,
    string,
    string,
    string,
    string
> BuildOverlapReasonV2 =

    delegate(
        string classification,
        string categoryRelationship,
        string parameterRelationship,
        string ruleRelationship
    )
{
    switch (classification)
    {
        case "Exact Duplicate":

            return
                "Both filters use the same categories and the same " +
                "complete canonical rule definition.";


        case "Same Rules / Different Categories":

            return
                "The complete rule definition is identical, but the " +
                "category sets differ. Category relationship: " +
                categoryRelationship +
                ".";


        case "Category Subset":

            return
                "The complete rule definition is identical and one " +
                "category set is a subset of the other.";


        case "Potential Logical Overlap":

            return
                "The filters apply to intersecting categories, use " +
                "compatible conditions, and no contradictory AND condition " +
                "was detected. Rule relationship: " +
                ruleRelationship +
                ".";


        case "Related":

            return
                "The filters share significant categories or parameters, " +
                "but the available rule structure does not justify a " +
                "stronger logical-overlap classification.";


        default:

            return "";
    }
};


// ============================================================================
// INTERNAL DIAGNOSTIC COUNTERS
//
// Not currently exported to Excel, but useful during validation.
// ============================================================================

int suppressedMutuallyExclusivePairCount =
    0;

int suppressedOfficeOfficePairCount =
    0;

int evaluatedCandidatePairCount =
    0;


// ============================================================================
// COMPARE EACH FILTER PAIR ONCE
// ============================================================================

for (
    int i = 0;
    i < projectFilterRecords.Count;
    i++
)
{
    Dictionary<string, object> a =
        projectFilterRecords[i];


    long filterAId =
        Convert.ToInt64(
            a["Filter ID"],
            CultureInfo.InvariantCulture
        );


    string filterAName =
        Convert.ToString(
            a["Filter Name"]
        );


    string classificationA =
        Convert.ToString(
            a["Office Classification"]
        );


    string fullRuleSignatureA =
        Convert.ToString(
            a["_RuleSignature"]
        );


    string expressionA =
        Convert.ToString(
            a["Rule Expression"]
        );


    HashSet<long> categoriesA =
        GetFilterCategoryIds(
            filterAId
        );


    HashSet<string> parametersA =
        GetFilterParameterKeys(
            filterAId
        );


    HashSet<string> terminalRulesA =
        GetTerminalRuleSignatureSet(
            filterAId
        );


    List<Dictionary<string, object>> rowsA =
        GetFilterRuleRows(
            filterAId
        );


    bool safeA =
        IsSafeAndComparable(
            a,
            rowsA
        );


    for (
        int j = i + 1;
        j < projectFilterRecords.Count;
        j++
    )
    {
        Dictionary<string, object> b =
            projectFilterRecords[j];


        long filterBId =
            Convert.ToInt64(
                b["Filter ID"],
                CultureInfo.InvariantCulture
            );


        string filterBName =
            Convert.ToString(
                b["Filter Name"]
            );


        string classificationB =
            Convert.ToString(
                b["Office Classification"]
            );


        string fullRuleSignatureB =
            Convert.ToString(
                b["_RuleSignature"]
            );


        string expressionB =
            Convert.ToString(
                b["Rule Expression"]
            );


        HashSet<long> categoriesB =
            GetFilterCategoryIds(
                filterBId
            );


        HashSet<string> parametersB =
            GetFilterParameterKeys(
                filterBId
            );


        HashSet<string> terminalRulesB =
            GetTerminalRuleSignatureSet(
                filterBId
            );


        List<Dictionary<string, object>> rowsB =
            GetFilterRuleRows(
                filterBId
            );


        bool safeB =
            IsSafeAndComparable(
                b,
                rowsB
            );


        // ====================================================================
        // BASIC RELATIONSHIPS
        // ====================================================================

        bool sameCategories =
            categoriesA.SetEquals(
                categoriesB
            );


        bool categoryIntersection =
            categoriesA.Overlaps(
                categoriesB
            );


        bool sameParameters =
            parametersA.SetEquals(
                parametersB
            );


        bool parameterIntersection =
            parametersA.Overlaps(
                parametersB
            );


        bool sameFullRules =
            string.Equals(
                fullRuleSignatureA,
                fullRuleSignatureB,
                StringComparison.Ordinal
            );


        string categoryRelationship =
            GetCategoryRelationship(
                categoriesA,
                categoriesB
            );


        string parameterRelationship =
            GetParameterRelationship(
                parametersA,
                parametersB
            );


        // ====================================================================
        // OFFICE REFERENCE RELATIONSHIP
        // ====================================================================

        string officeReferenceA =
            Convert.ToString(
                a["Office Filter Name"]
            );


        string officeReferenceB =
            Convert.ToString(
                b["Office Filter Name"]
            );


        bool officeReferenceRelationship =
            (
                !string.IsNullOrWhiteSpace(
                    officeReferenceA
                )
                &&
                string.Equals(
                    officeReferenceA,
                    filterBName,
                    StringComparison.Ordinal
                )
            )
            ||
            (
                !string.IsNullOrWhiteSpace(
                    officeReferenceB
                )
                &&
                string.Equals(
                    officeReferenceB,
                    filterAName,
                    StringComparison.Ordinal
                )
            );


        // ====================================================================
        // PRESELECTION
        // ====================================================================

        if (
            !sameFullRules &&
            !categoryIntersection &&
            !parameterIntersection &&
            !officeReferenceRelationship
        )
        {
            continue;
        }


        evaluatedCandidatePairCount++;


        // ====================================================================
        // EXACT DUPLICATE
        //
        // Always retain this, even Office <-> Office.
        // ====================================================================

        if (
            sameCategories &&
            sameFullRules
        )
        {
            Dictionary<string, object> exactRecord =
                new Dictionary<string, object>(
                    StringComparer.OrdinalIgnoreCase
                );


            exactRecord["Project ID"] =
                projectId;

            exactRecord["Filter A ID"] =
                filterAId;

            exactRecord["Filter A Name"] =
                filterAName;

            exactRecord["Filter A Office Classification"] =
                classificationA;

            exactRecord["Filter B ID"] =
                filterBId;

            exactRecord["Filter B Name"] =
                filterBName;

            exactRecord["Filter B Office Classification"] =
                classificationB;

            exactRecord["Overlap Classification"] =
                "Exact Duplicate";

            exactRecord["Severity"] =
                "High";

            exactRecord["Confidence"] =
                "High";

            exactRecord["Same Categories"] =
                true;

            exactRecord["Category Relationship"] =
                "Exact";

            exactRecord["Same Parameters"] =
                sameParameters;

            exactRecord["Parameter Relationship"] =
                parameterRelationship;

            exactRecord["Rule Relationship"] =
                "Exact";

            exactRecord["Filter A Rule Expression"] =
                expressionA;

            exactRecord["Filter B Rule Expression"] =
                expressionB;

            exactRecord["Reason"] =
                "Both filters use the same categories and the same " +
                "complete canonical rule definition.";


            overlapRecords.Add(
                exactRecord
            );


            continue;
        }


        // ====================================================================
        // APPROVED OFFICE BASELINE SUPPRESSION
        //
        // Two approved office filters may intentionally operate on the same
        // categories and parameters.
        //
        // Unless they are exact duplicates, these are not PM-facing findings.
        // ====================================================================

        if (
            classificationA == "Office" &&
            classificationB == "Office"
        )
        {
            suppressedOfficeOfficePairCount++;

            continue;
        }


        // ====================================================================
        // IDENTICAL RULES / CATEGORY RELATIONSHIP
        // ====================================================================

        if (
            sameFullRules &&
            !sameCategories
        )
        {
            string overlapClassification;


            if (
                categoryRelationship ==
                "A subset of B"
                ||
                categoryRelationship ==
                "B subset of A"
            )
            {
                overlapClassification =
                    "Category Subset";
            }
            else
            {
                overlapClassification =
                    "Same Rules / Different Categories";
            }


            Dictionary<string, object> sameRulesRecord =
                new Dictionary<string, object>(
                    StringComparer.OrdinalIgnoreCase
                );


            sameRulesRecord["Project ID"] =
                projectId;

            sameRulesRecord["Filter A ID"] =
                filterAId;

            sameRulesRecord["Filter A Name"] =
                filterAName;

            sameRulesRecord["Filter A Office Classification"] =
                classificationA;

            sameRulesRecord["Filter B ID"] =
                filterBId;

            sameRulesRecord["Filter B Name"] =
                filterBName;

            sameRulesRecord["Filter B Office Classification"] =
                classificationB;

            sameRulesRecord["Overlap Classification"] =
                overlapClassification;

            sameRulesRecord["Severity"] =
                "Low";

            sameRulesRecord["Confidence"] =
                "High";

            sameRulesRecord["Same Categories"] =
                sameCategories;

            sameRulesRecord["Category Relationship"] =
                categoryRelationship;

            sameRulesRecord["Same Parameters"] =
                sameParameters;

            sameRulesRecord["Parameter Relationship"] =
                parameterRelationship;

            sameRulesRecord["Rule Relationship"] =
                "Exact";

            sameRulesRecord["Filter A Rule Expression"] =
                expressionA;

            sameRulesRecord["Filter B Rule Expression"] =
                expressionB;

            sameRulesRecord["Reason"] =
                BuildOverlapReasonV2(
                    overlapClassification,
                    categoryRelationship,
                    parameterRelationship,
                    "Exact"
                );


            overlapRecords.Add(
                sameRulesRecord
            );


            continue;
        }


        // ====================================================================
        // COMPLETE AND-CONDITION CONTRADICTION TEST
        // ====================================================================

        bool mutuallyExclusive =
            false;


        if (
            safeA &&
            safeB &&
            categoryIntersection &&
            parameterIntersection
        )
        {
            mutuallyExclusive =
                AreFiltersMutuallyExclusive(
                    rowsA,
                    rowsB
                );
        }


        if (
            mutuallyExclusive
        )
        {
            suppressedMutuallyExclusivePairCount++;

            continue;
        }


        // ====================================================================
        // RULE RELATIONSHIP
        // ====================================================================

        string ruleRelationship =
            GetRuleRelationshipV2(
                fullRuleSignatureA,
                fullRuleSignatureB,
                terminalRulesA,
                terminalRulesB,
                mutuallyExclusive
            );


        int sharedTerminalRuleCount =
            CountSharedTerminalRules(
                terminalRulesA,
                terminalRulesB
            );


        int maxTerminalRuleCount =
            Math.Max(
                terminalRulesA.Count,
                terminalRulesB.Count
            );


        double sharedRuleRatio =
            maxTerminalRuleCount > 0
            ? (double)sharedTerminalRuleCount /
              (double)maxTerminalRuleCount
            : 0.0;


        // ====================================================================
        // POTENTIAL LOGICAL OVERLAP
        //
        // Requirements:
        //
        //     - categories actually intersect
        //     - parameters intersect
        //     - both filters have safely comparable AND structures
        //     - no contradiction exists
        //     - AND there is substantial rule/parameter similarity
        //
        // This is intentionally stricter than V1.
        // ====================================================================

        bool ruleSubsetRelationship =
		    terminalRulesA.Count > 0 &&
		    terminalRulesB.Count > 0 &&
		    (
		        terminalRulesA.IsProperSubsetOf(
		            terminalRulesB
		        )
		        ||
		        terminalRulesB.IsProperSubsetOf(
		            terminalRulesA
		        )
		    );
		
		
		bool substantialSimilarity =
		    ruleSubsetRelationship
		    ||
		    (
		        sharedTerminalRuleCount >= 2 &&
		        sharedRuleRatio >= 0.75
		    );


        if (
            categoryIntersection &&
            parameterIntersection &&
            safeA &&
            safeB &&
            substantialSimilarity
        )
        {
            Dictionary<string, object> overlapRecord =
                new Dictionary<string, object>(
                    StringComparer.OrdinalIgnoreCase
                );


            overlapRecord["Project ID"] =
                projectId;

            overlapRecord["Filter A ID"] =
                filterAId;

            overlapRecord["Filter A Name"] =
                filterAName;

            overlapRecord["Filter A Office Classification"] =
                classificationA;

            overlapRecord["Filter B ID"] =
                filterBId;

            overlapRecord["Filter B Name"] =
                filterBName;

            overlapRecord["Filter B Office Classification"] =
                classificationB;

            overlapRecord["Overlap Classification"] =
                "Potential Logical Overlap";

            overlapRecord["Severity"] =
                "Medium";

            overlapRecord["Confidence"] =
                "Medium";

            overlapRecord["Same Categories"] =
                sameCategories;

            overlapRecord["Category Relationship"] =
                categoryRelationship;

            overlapRecord["Same Parameters"] =
                sameParameters;

            overlapRecord["Parameter Relationship"] =
                parameterRelationship;

            overlapRecord["Rule Relationship"] =
                ruleRelationship;

            overlapRecord["Filter A Rule Expression"] =
                expressionA;

            overlapRecord["Filter B Rule Expression"] =
                expressionB;

            overlapRecord["Reason"] =
                BuildOverlapReasonV2(
                    "Potential Logical Overlap",
                    categoryRelationship,
                    parameterRelationship,
                    ruleRelationship
                );


            overlapRecords.Add(
                overlapRecord
            );


            continue;
        }


        // ====================================================================
        // STRICT RELATED
        //
        // Do not report a pair merely because one category or parameter is
        // shared.
        //
        // Related now requires one of:
        //
        //     - an explicit office-reference relationship
        //     - exact parameter sets + intersecting categories
        //     - at least two identical terminal conditions
        //
        // ====================================================================

        bool strongRelatedEvidence =
		    officeReferenceRelationship
		    ||
		    (
		        categoryIntersection &&
		        sharedTerminalRuleCount >= 2 &&
		        sharedRuleRatio >= 0.75
		    );

        if (
            strongRelatedEvidence
        )
        {
            Dictionary<string, object> relatedRecord =
                new Dictionary<string, object>(
                    StringComparer.OrdinalIgnoreCase
                );


            relatedRecord["Project ID"] =
                projectId;

            relatedRecord["Filter A ID"] =
                filterAId;

            relatedRecord["Filter A Name"] =
                filterAName;

            relatedRecord["Filter A Office Classification"] =
                classificationA;

            relatedRecord["Filter B ID"] =
                filterBId;

            relatedRecord["Filter B Name"] =
                filterBName;

            relatedRecord["Filter B Office Classification"] =
                classificationB;

            relatedRecord["Overlap Classification"] =
                "Related";

            relatedRecord["Severity"] =
                "Info";

            relatedRecord["Confidence"] =
                (
                    safeA &&
                    safeB
                )
                ? "Medium"
                : "Limited";

            relatedRecord["Same Categories"] =
                sameCategories;

            relatedRecord["Category Relationship"] =
                categoryRelationship;

            relatedRecord["Same Parameters"] =
                sameParameters;

            relatedRecord["Parameter Relationship"] =
                parameterRelationship;

            relatedRecord["Rule Relationship"] =
                ruleRelationship;

            relatedRecord["Filter A Rule Expression"] =
                expressionA;

            relatedRecord["Filter B Rule Expression"] =
                expressionB;

            relatedRecord["Reason"] =
                BuildOverlapReasonV2(
                    "Related",
                    categoryRelationship,
                    parameterRelationship,
                    ruleRelationship
                );


            overlapRecords.Add(
                relatedRecord
            );
        }
    }
}


// ============================================================================
// SORT OVERLAPS
// ============================================================================

Func<string, int> GetSeveritySortOrder =
    delegate(string severity)
{
    switch (severity)
    {
        case "High":
            return 1;

        case "Medium":
            return 2;

        case "Low":
            return 3;

        case "Info":
            return 4;

        default:
            return 5;
    }
};


overlapRecords =
    overlapRecords
        .OrderBy(
            r =>
                GetSeveritySortOrder(
                    Convert.ToString(
                        r["Severity"]
                    )
                )
        )
        .ThenBy(
            r =>
                Convert.ToString(
                    r["Overlap Classification"]
                ),
            StringComparer.OrdinalIgnoreCase
        )
        .ThenBy(
            r =>
                Convert.ToString(
                    r["Filter A Name"]
                ),
            StringComparer.OrdinalIgnoreCase
        )
        .ThenBy(
            r =>
                Convert.ToString(
                    r["Filter B Name"]
                ),
            StringComparer.OrdinalIgnoreCase
        )
        .ToList();


// ============================================================================
// SUMMARY COUNTS
//
// Names remain identical to Block 9 V1 so Block 10 does not need changing.
// ============================================================================

int exactDuplicateCount =
    overlapRecords.Count(
        r =>
            Convert.ToString(
                r["Overlap Classification"]
            ) ==
            "Exact Duplicate"
    );


int sameRulesDifferentCategoriesCount =
    overlapRecords.Count(
        r =>
            Convert.ToString(
                r["Overlap Classification"]
            ) ==
            "Same Rules / Different Categories"
    );


int categorySubsetCount =
    overlapRecords.Count(
        r =>
            Convert.ToString(
                r["Overlap Classification"]
            ) ==
            "Category Subset"
    );


int potentialLogicalOverlapCount =
    overlapRecords.Count(
        r =>
            Convert.ToString(
                r["Overlap Classification"]
            ) ==
            "Potential Logical Overlap"
    );


int relatedFilterPairCount =
    overlapRecords.Count(
        r =>
            Convert.ToString(
                r["Overlap Classification"]
            ) ==
            "Related"
    );


int highSeverityOverlapCount =
    overlapRecords.Count(
        r =>
            Convert.ToString(
                r["Severity"]
            ) ==
            "High"
    );


int mediumSeverityOverlapCount =
    overlapRecords.Count(
        r =>
            Convert.ToString(
                r["Severity"]
            ) ==
            "Medium"
    );


// ============================================================================
// BLOCK 9 V2 COMPLETE
//
// Internal validation counters:
//
//     suppressedMutuallyExclusivePairCount
//     suppressedOfficeOfficePairCount
//     evaluatedCandidatePairCount
//
// These are intentionally not yet exported to the workbook.
//
// ============================================================================

// ============================================================================
// BLOCK 10
// SUMMARY + PROJECT DATASETS + EXCEL XLSX EXPORT
//
// No Microsoft.Office.Interop.Excel reference is required.
// Excel is accessed through late binding.
//
// Requirement:
//     Microsoft Excel desktop must be installed.
//
// Workbook sheets:
//
//     Summary
//     Project
//     Filters
//     Categories
//     Rules
//     Usage
//     Overlaps
//
// ============================================================================


// ============================================================================
// PROJECT INFORMATION
// ============================================================================

ProjectInfo projectInfo =
    doc.ProjectInformation;


string projectNameValue =
    doc.Title ?? "";


string projectNumberValue =
    "";


try
{
    if (projectInfo != null)
    {
        projectNumberValue =
            projectInfo.Number ?? "";
    }
}
catch
{
}


string rvtFileName =
    "";


string rvtPath =
    "";


try
{
    rvtPath =
        doc.PathName ?? "";


    if (
        !string.IsNullOrWhiteSpace(
            rvtPath
        )
    )
    {
        rvtFileName =
            Path.GetFileName(
                rvtPath
            );
    }
}
catch
{
}


if (
    string.IsNullOrWhiteSpace(
        rvtFileName
    )
)
{
    rvtFileName =
        projectNameValue;
}


// ============================================================================
// REVIT VERSION
// ============================================================================

string revitVersion =
    "";


string revitBuild =
    "";


try
{
    revitVersion =
        doc.Application.VersionNumber ?? "";

    revitBuild =
        doc.Application.VersionBuild ?? "";
}
catch
{
}


// ============================================================================
// AUDIT DATE
// ============================================================================

DateTime auditDate =
    DateTime.Now;


string auditDateText =
    auditDate.ToString(
        "yyyy-MM-dd HH:mm:ss",
        CultureInfo.InvariantCulture
    );


// ============================================================================
// PROJECT DATASET
// ============================================================================

List<Dictionary<string, object>> projectRecords =
    new List<Dictionary<string, object>>();


Dictionary<string, object> projectRecord =
    new Dictionary<string, object>(
        StringComparer.OrdinalIgnoreCase
    );


projectRecord["Project ID"] =
    projectId;

projectRecord["Project Name"] =
    projectNameValue;

projectRecord["Project Number"] =
    projectNumberValue;

projectRecord["RVT File Name"] =
    rvtFileName;

projectRecord["RVT Path"] =
    rvtPath;

projectRecord["Revit Version"] =
    revitVersion;

projectRecord["Revit Build"] =
    revitBuild;

projectRecord["Audit Date"] =
    auditDateText;

projectRecord["Office Filter Library Version"] =
    officeLibraryVersion;

projectRecord["Office Filter Library Path"] =
    officeLibraryPath;


projectRecords.Add(
    projectRecord
);


// ============================================================================
// SUMMARY DATASET
//
// Summary is intentionally a simple Metric / Value table.
// ============================================================================

List<Dictionary<string, object>> summaryRecords =
    new List<Dictionary<string, object>>();


// Helper for adding summary rows.
Action<string, object> AddSummary =
    delegate(
        string metric,
        object value
    )
{
    Dictionary<string, object> row =
        new Dictionary<string, object>(
            StringComparer.OrdinalIgnoreCase
        );

    row["Metric"] =
        metric;

    row["Value"] =
        value;

    summaryRecords.Add(
        row
    );
};


// ============================================================================
// SUMMARY CONTENT
// ============================================================================

AddSummary(
    "Project ID",
    projectId
);

AddSummary(
    "Project Name",
    projectNameValue
);

AddSummary(
    "Audit Date",
    auditDateText
);

AddSummary(
    "Office Filter Library Version",
    officeLibraryVersion
);

AddSummary(
    "Total Filters",
    projectFilterRecords.Count
);

AddSummary(
    "Office Filters",
    officeFilterCount
);

AddSummary(
    "Modified Office Filters",
    modifiedOfficeFilterCount
);

AddSummary(
    "Custom Filters",
    customFilterCount
);

AddSummary(
    "Unknown Filters",
    unknownFilterCount
);

AddSummary(
    "Used Filters",
    usedFilterCount
);

AddSummary(
    "Unused Filters",
    unusedFilterCount
);

AddSummary(
    "Direct View Usage Instances",
    directUsageRowCount
);

AddSummary(
    "View Template Usage Instances",
    templateUsageRowCount
);

AddSummary(
    "Category Rows",
    categoryRecords.Count
);

AddSummary(
    "Unclassified Category Rows",
    unclassifiedCategoryRows
);

AddSummary(
    "Terminal Rules",
    totalTerminalRuleCount
);

AddSummary(
    "Inverted Rules",
    invertedRuleCount
);

AddSummary(
    "Filters With No Rules",
    filtersWithNoRules
);

AddSummary(
    "Partially Analysed Rules",
    partiallyAnalysedRuleCount
);

AddSummary(
    "Not Analysed Rules",
    notAnalysedRuleCount
);

AddSummary(
    "Exact Duplicate Relationships",
    exactDuplicateCount
);

AddSummary(
    "Same Rules / Different Categories",
    sameRulesDifferentCategoriesCount
);

AddSummary(
    "Category Subset Relationships",
    categorySubsetCount
);

AddSummary(
    "Potential Logical Overlaps",
    potentialLogicalOverlapCount
);

AddSummary(
    "Related Filter Pairs",
    relatedFilterPairCount
);

AddSummary(
    "High Severity Overlap Findings",
    highSeverityOverlapCount
);

AddSummary(
    "Medium Severity Overlap Findings",
    mediumSeverityOverlapCount
);


// ============================================================================
// LOCKED EXCEL COLUMN DEFINITIONS
// ============================================================================

string[] summaryColumns =
{
    "Metric",
    "Value"
};


string[] projectColumns =
{
    "Project ID",
    "Project Name",
    "Project Number",
    "RVT File Name",
    "RVT Path",
    "Revit Version",
    "Revit Build",
    "Audit Date",
    "Office Filter Library Version",
    "Office Filter Library Path"
};


string[] filterColumns =
{
    "Project ID",
    "Filter ID",
    "Filter Unique ID",
    "Filter Name",
    "Office Classification",
    "Office Filter Name",
    "Office Library Version",
    "Definition Match",
    "Usage Status",
    "Direct View Count",
    "View Template Count",
    "Total Usage Count",
    "Category Count",
    "Category Groups",
    "Rule Count",
    "Rule Analysis Status",
    "Rule Expression"
};


string[] categoryColumns =
{
    "Project ID",
    "Filter ID",
    "Filter Name",
    "Category ID",
    "Category Name",
    "Category Group",
    "Category Classification Status"
};


string[] ruleColumns =
{
    "Project ID",
    "Filter ID",
    "Filter Name",
    "Rule Index",
    "Tree Path",
    "Root Logic",
    "Rule Type",
    "Inner Rule Type",
    "Is Inverted",
    "Parameter ID",
    "Parameter Name",
    "Parameter Type",
    "Parameter Source",
    "Parameter GUID",
    "Evaluator",
    "Operator",
    "Raw Value",
    "Display Value",
    "Unit",
    "Rule Expression",
    "Rule Analysis Status",
    "Raw Rule Data",
    "Rule Signature"
};


string[] usageColumns =
{
    "Project ID",
    "Filter ID",
    "Filter Name",
    "Usage Type",
    "View ID",
    "View Unique ID",
    "View Name",
    "View Type",
    "Is View Template"
};


string[] overlapColumns =
{
    "Project ID",
    "Filter A ID",
    "Filter A Name",
    "Filter A Office Classification",
    "Filter B ID",
    "Filter B Name",
    "Filter B Office Classification",
    "Overlap Classification",
    "Severity",
    "Confidence",
    "Same Categories",
    "Category Relationship",
    "Same Parameters",
    "Parameter Relationship",
    "Rule Relationship",
    "Filter A Rule Expression",
    "Filter B Rule Expression",
    "Reason"
};


// ============================================================================
// VALIDATE OUTPUT DIRECTORY
// ============================================================================

if (
    !Directory.Exists(
        programmingFolder
    )
)
{
    TaskDialog.Show(
        "Filter Audit",
        "Output folder does not exist:\n\n" +
        programmingFolder
    );

    return;
}


// ============================================================================
// ENSURE OUTPUT FILE DOES NOT ALREADY EXIST
// ============================================================================

if (
    File.Exists(
        auditOutputPath
    )
)
{
    try
    {
        File.Delete(
            auditOutputPath
        );
    }
    catch (Exception ex)
    {
        TaskDialog.Show(
            "Filter Audit",
            "Existing audit workbook could not be replaced:\n\n" +
            auditOutputPath +
            "\n\n" +
            ex.Message
        );

        return;
    }
}


// ============================================================================
// EXCEL AVAILABILITY CHECK
// ============================================================================

Type excelType =
    Type.GetTypeFromProgID(
        "Excel.Application"
    );


if (excelType == null)
{
    TaskDialog.Show(
        "Filter Audit",
        "Microsoft Excel could not be found on this computer.\n\n" +
        "The audit analysis completed, but the XLSX workbook could " +
        "not be created."
    );

    return;
}


// ============================================================================
// EXCEL OBJECTS
// ============================================================================

object excelObject =
    null;

object workbookObject =
    null;


try
{
    excelObject =
        Activator.CreateInstance(
            excelType
        );


    dynamic excel =
        excelObject;


    excel.Visible =
        false;

    excel.DisplayAlerts =
        false;

    excel.ScreenUpdating =
        false;


    workbookObject =
        excel.Workbooks.Add();


    dynamic workbook =
        workbookObject;


    // ========================================================================
	// KEEP ONE DEFAULT SHEET TEMPORARILY
	//
	// Excel requires at least one visible worksheet to exist at all times.
	// We therefore keep the initial sheet, create all audit worksheets,
	// then delete the temporary sheet afterward.
	// ========================================================================
	
	dynamic temporarySheet =
	    workbook.Worksheets[1];
	
	temporarySheet.Name =
	    "__TEMP__";


    // ========================================================================
    // WORKSHEET WRITER
    //
    // Writes the entire table in one array assignment rather than setting
    // cells individually. This is significantly faster through COM.
    // ========================================================================

    Action<
        string,
        string[],
        List<Dictionary<string, object>>
    > WriteWorksheet =

        delegate(
            string sheetName,
            string[] columns,
            List<Dictionary<string, object>> records
        )
    {
        dynamic sheet =
            workbook.Worksheets.Add();


        sheet.Name =
            sheetName;


        int rowCount =
            records.Count + 1;

        int columnCount =
            columns.Length;


        object[,] values =
            new object[
                rowCount,
                columnCount
            ];


        // --------------------------------------------------------------------
        // HEADERS
        // --------------------------------------------------------------------

        for (
            int columnIndex = 0;
            columnIndex < columnCount;
            columnIndex++
        )
        {
            values[
                0,
                columnIndex
            ] =
                columns[
                    columnIndex
                ];
        }


        // --------------------------------------------------------------------
        // DATA
        // --------------------------------------------------------------------

        for (
            int rowIndex = 0;
            rowIndex < records.Count;
            rowIndex++
        )
        {
            Dictionary<string, object> record =
                records[rowIndex];


            for (
                int columnIndex = 0;
                columnIndex < columnCount;
                columnIndex++
            )
            {
                string columnName =
                    columns[
                        columnIndex
                    ];


                object value =
                    null;


                if (
                    record.TryGetValue(
                        columnName,
                        out value
                    )
                )
                {
                    // Excel COM prefers simple scalar values.
                    if (value == null)
                    {
                        values[
                            rowIndex + 1,
                            columnIndex
                        ] =
                            "";
                    }

                    else if (
                        value is bool
                    )
                    {
                        values[
                            rowIndex + 1,
                            columnIndex
                        ] =
                            (bool)value
                            ? "TRUE"
                            : "FALSE";
                    }

                    else
                    {
                        values[
                            rowIndex + 1,
                            columnIndex
                        ] =
                            value;
                    }
                }
                else
                {
                    values[
                        rowIndex + 1,
                        columnIndex
                    ] =
                        "";
                }
            }
        }


        // --------------------------------------------------------------------
        // WRITE ARRAY
        // --------------------------------------------------------------------

        dynamic firstCell =
            sheet.Cells[
                1,
                1
            ];


        dynamic lastCell =
            sheet.Cells[
                rowCount,
                columnCount
            ];


        dynamic range =
            sheet.Range[
                firstCell,
                lastCell
            ];


        range.Value2 =
            values;


        // --------------------------------------------------------------------
        // HEADER FORMATTING
        // --------------------------------------------------------------------

        dynamic headerStart =
            sheet.Cells[
                1,
                1
            ];


        dynamic headerEnd =
            sheet.Cells[
                1,
                columnCount
            ];


        dynamic headerRange =
            sheet.Range[
                headerStart,
                headerEnd
            ];


        headerRange.Font.Bold =
            true;


        // Dark header background.
        headerRange.Interior.Color =
            0x404040;


        headerRange.Font.Color =
            0xFFFFFF;


        // --------------------------------------------------------------------
        // FREEZE TOP ROW
        // --------------------------------------------------------------------

        sheet.Activate();


        excel.ActiveWindow.SplitRow =
            1;

        excel.ActiveWindow.FreezePanes =
            true;


        // --------------------------------------------------------------------
        // AUTOFILTER
        // --------------------------------------------------------------------

        if (
            rowCount >= 1 &&
            columnCount >= 1
        )
        {
            range.AutoFilter();
        }


        // --------------------------------------------------------------------
        // COLUMN WIDTH
        // --------------------------------------------------------------------

        range.Columns.AutoFit();


        // Cap extremely wide columns such as signatures and expressions.
        for (
            int c = 1;
            c <= columnCount;
            c++
        )
        {
            dynamic column =
                sheet.Columns[c];


            try
            {
                if (
                    Convert.ToDouble(
                        column.ColumnWidth,
                        CultureInfo.InvariantCulture
                    ) > 55.0
                )
                {
                    column.ColumnWidth =
                        55.0;
                }
            }
            catch
            {
            }


            try
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                    column
                );
            }
            catch
            {
            }
        }


        // --------------------------------------------------------------------
        // TEXT WRAPPING
        //
        // Useful for expressions, signatures, paths and reasons.
        // --------------------------------------------------------------------

        range.VerticalAlignment =
            -4160; // xlTop


        foreach (
            string wrapColumnName
            in new string[]
            {
                "Rule Expression",
                "Raw Rule Data",
                "Rule Signature",
                "Filter A Rule Expression",
                "Filter B Rule Expression",
                "Reason",
                "Definition Match",
                "RVT Path",
                "Office Filter Library Path"
            }
        )
        {
            int index =
                Array.IndexOf(
                    columns,
                    wrapColumnName
                );


            if (index >= 0)
            {
                dynamic wrapColumn =
                    sheet.Columns[
                        index + 1
                    ];

                wrapColumn.WrapText =
                    true;


                try
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                        wrapColumn
                    );
                }
                catch
                {
                }
            }
        }


        // --------------------------------------------------------------------
        // RELEASE LOCAL COM OBJECTS
        // --------------------------------------------------------------------

        try
        {
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                headerRange
            );

            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                headerEnd
            );

            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                headerStart
            );

            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                range
            );

            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                lastCell
            );

            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                firstCell
            );

            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                sheet
            );
        }
        catch
        {
        }
    };


    // ========================================================================
    // CREATE WORKSHEETS
    //
    // Because Excel adds each new worksheet before the active sheet, create
    // these in reverse order so the final workbook appears in our preferred
    // order.
    // ========================================================================

    WriteWorksheet(
        "Overlaps",
        overlapColumns,
        overlapRecords
    );


    WriteWorksheet(
        "Usage",
        usageColumns,
        usageRecords
    );


    WriteWorksheet(
        "Rules",
        ruleColumns,
        ruleRecords
    );


    WriteWorksheet(
        "Categories",
        categoryColumns,
        categoryRecords
    );


    WriteWorksheet(
        "Filters",
        filterColumns,
        projectFilterRecords
    );


    WriteWorksheet(
        "Project",
        projectColumns,
        projectRecords
    );


    WriteWorksheet(
        "Summary",
        summaryColumns,
        summaryRecords
    );
    
    
    // ========================================================================
	// DELETE TEMPORARY DEFAULT SHEET
	// ========================================================================
	
	dynamic tempSheetToDelete =
	    workbook.Worksheets[
	        "__TEMP__"
	    ];
	
	tempSheetToDelete.Delete();
	
	try
	{
	    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
	        tempSheetToDelete
	    );
	}
	catch
	{
	}


    // ========================================================================
    // SELECT SUMMARY SHEET
    // ========================================================================

    dynamic summarySheet =
        workbook.Worksheets[
            "Summary"
        ];


    summarySheet.Activate();


    try
    {
        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
            summarySheet
        );
    }
    catch
    {
    }


    // ========================================================================
    // SAVE XLSX
    //
    // 51 = xlOpenXMLWorkbook (.xlsx)
    // ========================================================================

    const int xlOpenXMLWorkbook =
        51;


    workbook.SaveAs(
        auditOutputPath,
        xlOpenXMLWorkbook
    );


    workbook.Close(
        false
    );


    excel.Quit();


    // ========================================================================
    // SUCCESS
    // ========================================================================

    TaskDialog.Show(
        "Filter Audit Complete",

        "Filter audit completed successfully.\n\n" +

        "Project:\n" +
        projectNameValue +

        "\n\nFilters analysed: " +
        projectFilterRecords.Count +

        "\nOffice: " +
        officeFilterCount +

        "\nModified Office: " +
        modifiedOfficeFilterCount +

        "\nCustom: " +
        customFilterCount +

        "\nUnknown: " +
        unknownFilterCount +

        "\n\nUsed: " +
        usedFilterCount +

        "\nUnused: " +
        unusedFilterCount +

        "\n\nExact duplicates: " +
        exactDuplicateCount +

        "\nPotential logical overlaps: " +
        potentialLogicalOverlapCount +

        "\n\nWorkbook:\n" +
        auditOutputPath
    );
}


// ============================================================================
// FAILURE
// ============================================================================

catch (Exception ex)
{
    TaskDialog.Show(
        "Filter Audit",

        "The audit analysis encountered an error while creating the " +
        "Excel workbook.\n\n" +

        ex.GetType().Name +
        "\n\n" +

        ex.Message
    );
}


// ============================================================================
// CLEAN UP EXCEL COM
// ============================================================================

finally
{
    if (
        workbookObject != null
    )
    {
        try
        {
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                workbookObject
            );
        }
        catch
        {
        }

        workbookObject =
            null;
    }


    if (
        excelObject != null
    )
    {
        try
        {
            dynamic excelCleanup =
                excelObject;

            try
            {
                excelCleanup.Quit();
            }
            catch
            {
            }


            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(
                excelObject
            );
        }
        catch
        {
        }

        excelObject =
            null;
    }


    GC.Collect();
    GC.WaitForPendingFinalizers();

    GC.Collect();
    GC.WaitForPendingFinalizers();
}


// ============================================================================
// BLOCK 10 COMPLETE
// ============================================================================