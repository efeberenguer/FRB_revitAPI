// Revit 2024 / ArchSmarter Launchpad
// Checks for Revisions that are not included in the revision schedule
// of any sheet whose Sheet Number matches ^[0-9]{5}$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

// Sheet number pattern: exactly five digits
Regex sheetNumberRegex = new Regex(@"^[0-9]{5}$");

// ------------------------------------------------------------
// 1. Find qualifying sheets
// ------------------------------------------------------------

List<ViewSheet> qualifyingSheets =
    new FilteredElementCollector(doc)
        .OfClass(typeof(ViewSheet))
        .Cast<ViewSheet>()
        .Where(sheet => sheetNumberRegex.IsMatch(sheet.SheetNumber ?? ""))
        .ToList();

// ------------------------------------------------------------
// 2. Find all revisions used on qualifying sheets
// ------------------------------------------------------------

HashSet<ElementId> usedRevisionIds = new HashSet<ElementId>();

foreach (ViewSheet sheet in qualifyingSheets)
{
    ICollection<ElementId> revisionIds = sheet.GetAllRevisionIds();

    foreach (ElementId revisionId in revisionIds)
    {
        usedRevisionIds.Add(revisionId);
    }
}

// ------------------------------------------------------------
// 3. Get all revisions in the model
// ------------------------------------------------------------

List<Revision> allRevisions =
    new FilteredElementCollector(doc)
        .OfClass(typeof(Revision))
        .Cast<Revision>()
        .ToList();
		
// ------------------------------------------------------------
// 4. Find unused revisions
// ------------------------------------------------------------

List<Revision> unusedRevisions =
    allRevisions
        .Where(revision => !usedRevisionIds.Contains(revision.Id))
        .OrderBy(revision => revision.SequenceNumber)
        .ToList();
		
// ------------------------------------------------------------
// 5. Build report
// ------------------------------------------------------------

StringBuilder report = new StringBuilder();

report.AppendLine("REVISION CHECK");
report.AppendLine("==============");
report.AppendLine();

report.AppendLine(
    $"Qualifying sheets: {qualifyingSheets.Count}");
	
report.AppendLine(
    $"Revisions in model: {allRevisions.Count}");
	
report.AppendLine(
    $"Revisions used on qualifying sheets: {usedRevisionIds.Count}");
	
report.AppendLine();

if (unusedRevisions.Count == 0)
{
    report.AppendLine("UNUSED REVISIONS");
    report.AppendLine("================");
    report.AppendLine("None.");
}
else
{
    report.AppendLine("UNUSED REVISIONS");
    report.AppendLine("================");

    foreach (Revision revision in unusedRevisions)
    {
        string description = revision.Description ?? "";

        report.AppendLine(
        $"Revision {revision.SequenceNumber} | {description}");
    }
    
    report.AppendLine();
    report.AppendLine(
        $"RESULT: {unusedRevisions.Count} unused revision(s) found.");
}

// ------------------------------------------------------------
// 6. Display result
// ------------------------------------------------------------

TaskDialog.Show(
    "Revision Check",
    report.ToString());