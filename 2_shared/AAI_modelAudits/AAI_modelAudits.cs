// PURPOSE

//The purpose of this script is to automate the extraction and analysis of data from RVT model audits.

// StringBuilder

StringBuilder report = new StringBuilder();

// Section Numbering Variables

int sectionNumber = 0;
int subsectionNumber = 0;

// HTML code starts

report.AppendLine("<html>");

report.AppendLine("<body>");

report.AppendLine("<style>");
report.AppendLine();
report.AppendLine("body {");
report.AppendLine();
report.AppendLine("font-family: Cascadia Code;");
report.AppendLine();
report.AppendLine("}");
report.AppendLine();
report.AppendLine("</style>");
report.AppendLine();

// Header 1 method

private static void Header1(
	StringBuilder report,
	ref int sectionNumber,
	string text)
{
	sectionNumber++;
	report.AppendLine($"<h1>{sectionNumber} {text}</h1>");
	report.AppendLine();	
}

// Header 2 method

private static void Header2(
	StringBuilder report,
	ref int sectionNumber,
	ref int subsectionNumber,
	string text)
{
	subsectionNumber++;
	report.AppendLine($"<h2>{sectionNumber}.{subsectionNumber} {text}</h2>");
	report.AppendLine();	
}

// Audit code starts

ProjectInfo projectInfo = doc.ProjectInformation; // The Document object

// SECTION: GENERAL INFORMATION

Header1(report, ref sectionNumber, "GENERAL INFORMATION");

// Subsection: Model Auditor
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Model Auditor");

Parameter modelAuditorParam = projectInfo.LookupParameter("AAI_ModelAuditor");

private static void ConfirmParameterValue(
	StringBuilder report,
	string parameter,
	string actionRequired)
{
	if (string.IsNullOrWhiteSpace(parameter))
	{
		report.AppendLine($"Action Required: {actionRequired}");
	}
	else
	{
		report.AppendLine($"{parameter}");
	}
}

ConfirmParameterValue(
	report,
	modelAuditorParam?.AsString(),
	"Shared parameter AAI_ModelAuditor to be set up and/or populated.");
	
// Subsection: Project Number
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Project Number");

ConfirmParameterValue(
	report,
	projectInfo.Number,
	"Project Number to be populated.");
	
// Subsection: Project Name
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Project Name");

ConfirmParameterValue(
	report,
	projectInfo.Name,
	"Project Name to be populated.");
	
// Subsection: BIM Lead
	
Header2(report, ref sectionNumber, ref subsectionNumber, "BIM Lead");

Parameter BIMLeadParam = projectInfo.LookupParameter("AAI_BIMLead");

ConfirmParameterValue(
	report,
	BIMLeadParam?.AsString(),
	"Shared parameter AAI_BIMLead to be set up and/or populated.");
	
// Subsection: Project Lead
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Project Lead");

Parameter projectLeadParam = projectInfo.LookupParameter("AAI_ProjectLead");

ConfirmParameterValue(
	report,
	projectLeadParam?.AsString(),
	"Shared parameter AAI_ProjectLead to be set up and/or populated.");

// Subsection: Model Description
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Model Description");

Parameter modelDescriptionParam = projectInfo.LookupParameter("AAI_ModelDescription");

ConfirmParameterValue(
	report,
	modelDescriptionParam?.AsString(),
	"Shared parameter AAI_ModelDescription to be set up and/or populated.");

// Subsection: File Name
	
Header2(report, ref sectionNumber, ref subsectionNumber, "File Name");

ConfirmParameterValue(
	report,
	doc.Title,
	" ");

// Subsection: File Size
	
Header2(report, ref sectionNumber, ref subsectionNumber, "File Size");

Parameter fileSizeParam = projectInfo.LookupParameter("AAI_FileSize");

ConfirmParameterValue(
	report,
	fileSizeParam?.AsString(),
	"Shared parameter AAI_FileSize to be set up and/or populated.");

// Subsection: Issue Date
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Issue Date");

DateTime today = DateTime.Today; // Returns the current date
string YYYY = today.ToString("yyyy");
string MM = today.ToString("MM");
string DD = today.ToString("dd");
string issueDate = YYYY + "-" + MM + "-" + DD;

ConfirmParameterValue(
	report,
	issueDate,
	" ");

subsectionNumber = 0;

// SECTION: INFORMATION

Header1(report, ref sectionNumber, "INFORMATION");

// Subsection: Splash Screen & Model Information
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Splash Screen & Model Information");

ViewSheet splashScreen = new FilteredElementCollector(doc)
    .OfClass(typeof(ViewSheet))
    .Cast<ViewSheet>()
    .FirstOrDefault(s =>
        s.Name == "ProjectSplashScreen" &&
        s.SheetNumber == "XXXXX");

var hasSplashTitleBlock = false;

if (splashScreen != null)
{
    hasSplashTitleBlock =
        new FilteredElementCollector(doc, splashScreen.Id)
            .OfCategory(BuiltInCategory.OST_TitleBlocks)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>()
            .Any(tb =>
                tb.Symbol.Family.Name ==
                "AAI_TBK_ProjectSplashScreen");
}

string splashScreenSummary =
    hasSplashTitleBlock ? "Splash screen is set up." : null;
    
ConfirmParameterValue(
	report,
	splashScreenSummary,
	"Review splash screen setup.");

// Subsection: Revit Version
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Revit Version");

Parameter revitVersionParam = projectInfo.LookupParameter("AAI_RevitVersion");

ConfirmParameterValue(
	report,
	revitVersionParam?.AsString(),
	"Shared parameter AAI_RevitVersion to be set up and/or populated.");

// Subsection: Autodesk Desktop Connector Version
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Autodesk Desktop Connector Version");

Parameter AutodeskDesktopConnectorVersionParam = projectInfo.LookupParameter("AAI_AutodeskDesktopConnectorVersion");

ConfirmParameterValue(
	report,
	AutodeskDesktopConnectorVersionParam?.AsString(),
	"Shared parameter AAI_AutodeskDesktopConnectorVersion to be set up and/or populated.");

// Subsection: Coordinates
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Coordinates");

// Subsection: Copy Monitor
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Copy Monitor");

// Subsection: Published Sets
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Published Sets");

subsectionNumber = 0;

// SECTION: AAI STANDARDS

Header1(report,	ref sectionNumber, "AAI STANDARDS");

Header2(report,	ref sectionNumber, ref subsectionNumber, "DWG Linked");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Filled Regions");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Line Patterns");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Line Styles");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Materials");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Naming Convention");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Object Styles");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Phases");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Project Browser");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Revit Links");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Revisions");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Sheet Issued Revisions");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Worksets");

subsectionNumber = 0;

// SECTION: FAMILIES

Header1(report,	ref sectionNumber, "FAMILIES");

Header2(report,	ref sectionNumber, ref subsectionNumber, "FT Content");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Generic Models");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Model In-Place");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Shared Parameters");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Largest Family Size");

subsectionNumber = 0;

// SECTION: PERFORMANCE

Header1(report,	ref sectionNumber, "PERFORMANCE");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Areas");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Design Options");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Design Options Set");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Detail Groups");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Detail Items");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Detail Lines");

Header2(report,	ref sectionNumber, ref subsectionNumber, "DWG Imported");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Filters");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Grids");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Images");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Levels");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Loadable Families");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Model Groups");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Model Health");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Model Lines");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Purge Elements");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Reference Planes");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Rooms");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Schedules");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Scope Boxes");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Sheets");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Tags");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Text Notes");

Header2(report,	ref sectionNumber, ref subsectionNumber, "View Templates");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Views");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Views on Sheets");

Header2(report,	ref sectionNumber, ref subsectionNumber, "Warnings");

// Audit code ends

report.AppendLine("</body>");

report.AppendLine("</html>");

// HTML code ends

// Output section starts below

Console.WriteLine(report.ToString());
Console.WriteLine(report.Length);

string path = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    "modelAudit.html");

File.WriteAllText(path, report.ToString());

Console.WriteLine(path);