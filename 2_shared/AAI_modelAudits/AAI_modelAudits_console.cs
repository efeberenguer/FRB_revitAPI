using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

// =====================================================================
// AAI MODEL AUDIT - console version
//
//   1. Config      : names and settings you will fine-tune
//   2. Messages    : reusable text templates
//   3. AuditReport : numbering, rows, rendering (no Revit code here)
//   4. Helpers     : reusable Revit queries and checks
//   5. Sections    : one method per audit section
//   6. Run         : calls the sections and prints the report
// =====================================================================


// ---------- 1. CONFIG ----------

static class Cfg
{
    public const string SplashSheetName   = "ProjectSplashScreen";
    public const string SplashSheetNumber = "XXXXX";
    public const string SplashTitleBlock  = "AAI_TBK_ProjectSplashScreen";

    public const int RefWidth   = 7;
    public const int LabelWidth = 34;
}


// ---------- 2. MESSAGES ----------

static class Msg
{
    public const string ParamNotFound  = "Shared parameter {0} not found. Add it to Project Information.";
    public const string ParamEmpty     = "Shared parameter {0} exists but is empty. Populate it.";
    public const string ValueEmpty     = "{0} to be populated.";
    public const string SplashNoSheet  = "Sheet {0} - {1} not found. Create the splash screen sheet.";
    public const string SplashNoTB     = "Sheet found, but title block {0} is not placed on it.";
    public const string PbpUnreadable  = "Could not read Project Base Point coordinates.";
    public const string SetUp          = "Set up";
    public const string NotImplemented = "Not implemented yet";
    public const string SectionFailed  = "Section failed: {0}";
}


// ---------- 3. REPORT MODEL & RENDERING ----------

enum Status { OK, Action, Info, Todo, Error }

class Row
{
    public string Ref;
    public string Label;
    public string Text;
    public Status Status;
}

class Section
{
    public string Ref;
    public string Title;
    public List<Row> Rows = new List<Row>();
}

class AuditReport
{
    readonly List<Section> sections = new List<Section>();
    Section current;

    // Runs one section. Numbering is automatic, and an exception
    // only breaks its own section, not the whole audit.
    public void Run(string title, Action<AuditReport> body)
    {
        current = new Section { Ref = (sections.Count + 1).ToString(), Title = title };
        sections.Add(current);

        try { body(this); }
        catch (Exception ex)
        {
            Add("Section error", Status.Error, string.Format(Msg.SectionFailed, ex.Message));
        }
    }

    public void Add(string label, Status status, string text)
    {
        current.Rows.Add(new Row
        {
            Ref    = $"{current.Ref}.{current.Rows.Count + 1}",
            Label  = label,
            Text   = text,
            Status = status
        });
    }

    public void Ok(string label, string value)          => Add(label, Status.OK, value);
    public void Info(string label, string value)        => Add(label, Status.Info, value);
    public void NeedsAction(string label, string text)  => Add(label, Status.Action, text);
    public void Todo(string label)                      => Add(label, Status.Todo, Msg.NotImplemented);
    public void Metric(string label, int count)         => Add(label, Status.Info, count.ToString("N0"));

    static string Tag(Status s)
    {
        switch (s)
        {
            case Status.OK:     return "[ OK ]";
            case Status.Action: return "[ !! ]";
            case Status.Todo:   return "[TODO]";
            case Status.Error:  return "[ERR ]";
            default:            return "[    ]";
        }
    }

    static string Line(Row r) =>
        r.Ref.PadRight(Cfg.RefWidth) + r.Label.PadRight(Cfg.LabelWidth) + Tag(r.Status) + " " + r.Text;

    static void Header(StringBuilder sb, string number, string title)
    {
        sb.AppendLine();
        sb.AppendLine($"{number}  {title}");
        sb.AppendLine(new string('=', 90));
    }

    public string Render()
    {
        var sb = new StringBuilder();
        var allRows = sections.SelectMany(s => s.Rows).ToList();

        foreach (Section s in sections)
        {
            Header(sb, s.Ref, s.Title);
            foreach (Row r in s.Rows) sb.AppendLine(Line(r));
        }

        // Outstanding actions are collected automatically from every section.
        var actions = allRows.Where(r => r.Status == Status.Action || r.Status == Status.Error).ToList();

        Header(sb, (sections.Count + 1).ToString(), "OUTSTANDING ACTIONS");
        if (actions.Count == 0) sb.AppendLine("None.");
        foreach (Row r in actions) sb.AppendLine(Line(r));

        sb.AppendLine();
        sb.AppendLine(
            $"Summary: {allRows.Count(r => r.Status == Status.OK)} OK, " +
            $"{allRows.Count(r => r.Status == Status.Action)} action required, " +
            $"{allRows.Count(r => r.Status == Status.Todo)} not implemented, " +
            $"{allRows.Count(r => r.Status == Status.Error)} errors.");

        return sb.ToString();
    }
}


// ---------- 4. HELPERS ----------

// Reads any parameter as text. Strings come back as-is; numbers, lengths,
// etc. come back formatted in project units (as shown in the Revit UI).
static string ReadParam(Element e, string name, out bool exists)
{
    Parameter p = e.LookupParameter(name);
    exists = p != null;
    if (p == null || !p.HasValue) return null;
    return p.StorageType == StorageType.String ? p.AsString() : p.AsValueString();
}

// Distinguishes "parameter missing from the project" from "parameter empty".
static void ParamCheck(AuditReport r, Element e, string paramName, string label)
{
    string value = ReadParam(e, paramName, out bool exists);

    if (!exists)                               r.NeedsAction(label, string.Format(Msg.ParamNotFound, paramName));
    else if (string.IsNullOrWhiteSpace(value)) r.NeedsAction(label, string.Format(Msg.ParamEmpty, paramName));
    else                                       r.Ok(label, value);
}

static void ValueCheck(AuditReport r, string label, string value)
{
    if (string.IsNullOrWhiteSpace(value)) r.NeedsAction(label, string.Format(Msg.ValueEmpty, label));
    else                                  r.Ok(label, value);
}

static int CountOfClass(Document doc, Type t) =>
    new FilteredElementCollector(doc).OfClass(t).GetElementCount();

static int CountInstances(Document doc, BuiltInCategory cat) =>
    new FilteredElementCollector(doc).OfCategory(cat).WhereElementIsNotElementType().GetElementCount();


// ---------- 5. SECTIONS ----------

static void GeneralInformation(AuditReport r, Document doc)
{
    ProjectInfo pi = doc.ProjectInformation;

    ParamCheck(r, pi, "AAI_ModelAuditor",     "Model Auditor");
    ValueCheck(r, "Project Number", pi.Number);
    ValueCheck(r, "Project Name",   pi.Name);
    ParamCheck(r, pi, "AAI_BIMLead",          "BIM Lead");
    ParamCheck(r, pi, "AAI_ProjectManager",   "Project Manager");
    ParamCheck(r, pi, "AAI_ModelDescription", "Model Description");
    ParamCheck(r, pi, "AAI_FileSize",         "File Size (MB)");
    r.Info("Issue Date", DateTime.Today.ToString("yyyy-MM-dd"));
}

static void ModelInformation(AuditReport r, Document doc)
{
    SplashScreenCheck(r, doc);
    ParamCheck(r, doc.ProjectInformation, "AAI_RevitVersion", "Revit Version");
    ParamCheck(r, doc.ProjectInformation, "AAI_AutodeskDesktopConnectorVersion", "Desktop Connector Version");
    ProjectBasePointCheck(r, doc);
    r.Todo("Copy Monitor");
    r.Todo("Published Sets");
}

static void SplashScreenCheck(AuditReport r, Document doc)
{
    const string label = "Splash Screen & Model Information";

    ViewSheet sheet = new FilteredElementCollector(doc)
        .OfClass(typeof(ViewSheet))
        .Cast<ViewSheet>()
        .FirstOrDefault(s => s.Name == Cfg.SplashSheetName && s.SheetNumber == Cfg.SplashSheetNumber);

    if (sheet == null)
    {
        r.NeedsAction(label, string.Format(Msg.SplashNoSheet, Cfg.SplashSheetNumber, Cfg.SplashSheetName));
        return;
    }

    bool hasTitleBlock = new FilteredElementCollector(doc, sheet.Id)
        .OfCategory(BuiltInCategory.OST_TitleBlocks)
        .OfClass(typeof(FamilyInstance))
        .Cast<FamilyInstance>()
        .Any(tb => tb.Symbol.Family.Name == Cfg.SplashTitleBlock);

    if (hasTitleBlock) r.Ok(label, Msg.SetUp);
    else               r.NeedsAction(label, string.Format(Msg.SplashNoTB, Cfg.SplashTitleBlock));
}

static void ProjectBasePointCheck(AuditReport r, Document doc)
{
    const string label = "Project Base Point (N/S, E/W)";

    BasePoint pbp = BasePoint.GetProjectBasePoint(doc);
    string ns = pbp?.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM)?.AsValueString();
    string ew = pbp?.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM)?.AsValueString();

    if (ns == null || ew == null) r.NeedsAction(label, Msg.PbpUnreadable);
    else                          r.Info(label, $"{ns}, {ew}");
}

static void AaiStandards(AuditReport r, Document doc)
{
    int linkedDwgs = new FilteredElementCollector(doc)
        .OfClass(typeof(ImportInstance))
        .Cast<ImportInstance>()
        .Count(i => i.IsLinked);

    r.Metric("Linked DWGs",    linkedDwgs);
    r.Metric("Filled Regions", CountOfClass(doc, typeof(FilledRegion)));
    r.Metric("Line Patterns",  CountOfClass(doc, typeof(LinePatternElement)));
    r.Metric("Line Styles",    doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines).SubCategories.Size);
}

static void Families(AuditReport r, Document doc)
{
    int inPlace = new FilteredElementCollector(doc)
        .OfClass(typeof(FamilyInstance))
        .Cast<FamilyInstance>()
        .Count(fi => fi.Symbol.Family.IsInPlace);

    r.Todo("FT Content");
    r.Metric("Generic Models (instances)", CountInstances(doc, BuiltInCategory.OST_GenericModel));
    r.Metric("Model In-Place (instances)", inPlace);
}

static void Performance(AuditReport r, Document doc)
{
    int detailLines = new FilteredElementCollector(doc)
        .OfClass(typeof(CurveElement))
        .Cast<CurveElement>()
        .Count(c => c.CurveElementType == CurveElementType.DetailCurve);

    r.Metric("Detail Groups (instances)", CountInstances(doc, BuiltInCategory.OST_IOSDetailGroups));
    r.Metric("Detail Items (instances)",  CountInstances(doc, BuiltInCategory.OST_DetailComponents));
    r.Metric("Detail Lines",              detailLines);
}


// ---------- 6. RUN ----------

var report = new AuditReport();

report.Run("GENERAL INFORMATION", r => GeneralInformation(r, doc));
report.Run("MODEL INFORMATION",   r => ModelInformation(r, doc));
report.Run("AAI STANDARDS",       r => AaiStandards(r, doc));
report.Run("FAMILIES",            r => Families(r, doc));
report.Run("PERFORMANCE",         r => Performance(r, doc));

Console.WriteLine(report.Render());
