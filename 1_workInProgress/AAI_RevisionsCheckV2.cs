using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

// ------------------------------------------------------------
// 1. Get model information
// ------------------------------------------------------------

string modelName = doc.Title;

string checkTimestamp =
    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

// ------------------------------------------------------------
// 2. Find qualifying sheets
// ------------------------------------------------------------

// Sheet number must consist of exactly five digits
Regex sheetNumberRegex =
    new Regex(@"^[0-9]{5}$");

List<ViewSheet> qualifyingSheets =
    new FilteredElementCollector(doc)
        .OfClass(typeof(ViewSheet))
        .Cast<ViewSheet>()
        .Where(sheet =>
            sheetNumberRegex.IsMatch(
                sheet.SheetNumber ?? ""))
        .OrderBy(sheet => sheet.SheetNumber)
        .ToList();

// ------------------------------------------------------------
// 3. Find revisions used on qualifying sheets
// ------------------------------------------------------------

// Revision ElementId -> List of qualifying sheets
// using that revision
Dictionary<ElementId, List<ViewSheet>> revisionUsage =
    new Dictionary<ElementId, List<ViewSheet>>();

foreach (ViewSheet sheet in qualifyingSheets)
{
    ICollection<ElementId> revisionIds =
        sheet.GetAllRevisionIds();

    foreach (ElementId revisionId in revisionIds)
    {
        if (!revisionUsage.ContainsKey(revisionId))
        {
            revisionUsage.Add(
                revisionId,
                new List<ViewSheet>());
        }

        revisionUsage[revisionId].Add(sheet);
    }
}

// ------------------------------------------------------------
// 4. Get all revisions in the model
// ------------------------------------------------------------

List<Revision> allRevisions =
    new FilteredElementCollector(doc)
        .OfClass(typeof(Revision))
        .Cast<Revision>()
        .OrderBy(revision => revision.SequenceNumber)
        .ToList();

// ------------------------------------------------------------
// 5. Identify unused revisions
// ------------------------------------------------------------

List<Revision> unusedRevisions =
    allRevisions
        .Where(revision =>
            !revisionUsage.ContainsKey(revision.Id))
        .ToList();

// ------------------------------------------------------------
// 6. Create CSV
// ------------------------------------------------------------

StringBuilder csv =
    new StringBuilder();

// ------------------------------------------------------------
// Metadata
// ------------------------------------------------------------

csv.AppendLine(
    $"{CsvEscape("Model Name")};" +
    $"{CsvEscape(modelName)}");

csv.AppendLine(
    $"{CsvEscape("Check Timestamp")};" +
    $"{CsvEscape(checkTimestamp)}");

csv.AppendLine();

// ------------------------------------------------------------
// Column headers
// ------------------------------------------------------------

csv.AppendLine(
    "Revision Number;" +
    "Revision Date;" +
    "Description;" +
    "Sheet Number;" +
    "Sheet Name;" +
    "Status");

// ------------------------------------------------------------
// Revision data
// ------------------------------------------------------------

foreach (Revision revision in allRevisions)
{
    string revisionNumber =
        revision.SequenceNumber.ToString();

    string revisionDate =
        revision.RevisionDate ?? "";

    string description =
        revision.Description ?? "";

    // --------------------------------------------------------
    // Unused revision
    // --------------------------------------------------------

    if (!revisionUsage.ContainsKey(revision.Id))
    {
        csv.AppendLine(
            $"{CsvEscape(revisionNumber)};" +
            $"{CsvEscape(revisionDate)};" +
            $"{CsvEscape(description)};;;" +
            $"{CsvEscape("Unused")}");
    }

    // --------------------------------------------------------
    // Used revision
    // --------------------------------------------------------

    else
    {
        List<ViewSheet> sheetsUsingRevision =
            revisionUsage[revision.Id]
                .OrderBy(sheet => sheet.SheetNumber)
                .ToList();

        foreach (ViewSheet sheet in sheetsUsingRevision)
        {
            csv.AppendLine(
                $"{CsvEscape(revisionNumber)};" +
                $"{CsvEscape(revisionDate)};" +
                $"{CsvEscape(description)};" +
                $"{CsvEscape(sheet.SheetNumber)};" +
                $"{CsvEscape(sheet.Name)};" +
                $"{CsvEscape("Used")}");
        }
    }
}

// ------------------------------------------------------------
// 7. Save CSV to Downloads folder
// ------------------------------------------------------------

string downloadsFolder =
    Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile),
        "Downloads");

string timestampForFileName =
    DateTime.Now.ToString(
        "yyyy-MM-dd_HHmmss");

string fileName =
    $"Revision_Check_{timestampForFileName}.csv";

string filePath =
    Path.Combine(
        downloadsFolder,
        fileName);

File.WriteAllText(
    filePath,
    csv.ToString(),
    Encoding.UTF8);

// ------------------------------------------------------------
// 8. Display summary
// ------------------------------------------------------------

StringBuilder report =
    new StringBuilder();

report.AppendLine(
    "REVISION CHECK");

report.AppendLine(
    "==============");

report.AppendLine();

report.AppendLine(
    $"Model: {modelName}");

report.AppendLine(
    $"Check: {checkTimestamp}");

report.AppendLine();

report.AppendLine(
    $"Qualifying sheets: {qualifyingSheets.Count}");

report.AppendLine(
    $"Revisions in model: {allRevisions.Count}");

report.AppendLine(
    $"Used revisions: " +
    $"{allRevisions.Count - unusedRevisions.Count}");

report.AppendLine(
    $"Unused revisions: " +
    $"{unusedRevisions.Count}");

report.AppendLine();

if (unusedRevisions.Count > 0)
{
    report.AppendLine(
        "UNUSED REVISIONS");

    report.AppendLine(
        "----------------");

    foreach (Revision revision in unusedRevisions)
    {
        report.AppendLine(
            $"Revision {revision.SequenceNumber} | " +
            $"{revision.RevisionDate} | " +
            $"{revision.Description}");
    }
}
else
{
    report.AppendLine(
        "No unused revisions found.");
}

report.AppendLine();

report.AppendLine(
    "CSV REPORT");

report.AppendLine(
    "----------");

report.AppendLine(
    filePath);

TaskDialog.Show(
    "Revision Check",
    report.ToString());


// ------------------------------------------------------------
// 9. CSV escaping helper
// ------------------------------------------------------------

string CsvEscape(string value)
{
    if (value == null)
        return "";

    // A semicolon is the CSV delimiter.
    // Quotes are escaped by doubling them.
    // Fields containing semicolons, quotes, or line breaks
    // are enclosed in double quotes.

    bool requiresQuotes =
        value.Contains(";") ||
        value.Contains("\"") ||
        value.Contains("\r") ||
        value.Contains("\n");

    if (value.Contains("\""))
    {
        value = value.Replace(
            "\"",
            "\"\"");
    }

    if (requiresQuotes)
    {
        value = "\"" + value + "\"";
    }

    return value;
}