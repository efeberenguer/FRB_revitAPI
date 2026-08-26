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

int usedRevisionCount =
    allRevisions.Count - unusedRevisions.Count;

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
// Summary
// ------------------------------------------------------------

csv.AppendLine("SUMMARY");

csv.AppendLine(
    $"{CsvEscape("Qualifying Sheets")};" +
    $"{qualifyingSheets.Count}");

csv.AppendLine(
    $"{CsvEscape("Revisions in Model")};" +
    $"{allRevisions.Count}");

csv.AppendLine(
    $"{CsvEscape("Used Revisions")};" +
    $"{usedRevisionCount}");

csv.AppendLine(
    $"{CsvEscape("Unused Revisions")};" +
    $"{unusedRevisions.Count}");

csv.AppendLine();

// ------------------------------------------------------------
// Unused revisions
// ------------------------------------------------------------

csv.AppendLine("UNUSED REVISIONS");

csv.AppendLine(
    "Revision Number;" +
    "Revision Date;" +
    "Description");

if (unusedRevisions.Count == 0)
{
    csv.AppendLine(
        "None;;");
}
else
{
    foreach (Revision revision in unusedRevisions)
    {
        string revisionNumber =
            revision.SequenceNumber.ToString();

        string revisionDate =
            revision.RevisionDate ?? "";

        string description =
            revision.Description ?? "";

        csv.AppendLine(
            $"{CsvEscape(revisionNumber)};" +
            $"{CsvEscape(revisionDate)};" +
            $"{CsvEscape(description)}");
    }
}

csv.AppendLine();

// ------------------------------------------------------------
// Full revision usage
// ------------------------------------------------------------

csv.AppendLine("REVISION USAGE");

csv.AppendLine(
    "Revision Number;" +
    "Revision Date;" +
    "Description;" +
    "Sheet Number;" +
    "Sheet Name;" +
    "Status");

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
// 8. Display completion dialog
// ------------------------------------------------------------

StringBuilder report =
    new StringBuilder();

report.AppendLine(
    "REVISION CHECK COMPLETE");

report.AppendLine();

report.AppendLine(
    $"Model: {modelName}");

report.AppendLine(
    $"Unused revisions: {unusedRevisions.Count}");

report.AppendLine();

report.AppendLine(
    "CSV report saved to:");

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

    // The CSV delimiter is a semicolon.
    // Fields containing semicolons, quotes, or line breaks
    // must be enclosed in double quotes.

    bool requiresQuotes =
        value.Contains(";") ||
        value.Contains("\"") ||
        value.Contains("\r") ||
        value.Contains("\n");

    // Escape quotation marks by doubling them.
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