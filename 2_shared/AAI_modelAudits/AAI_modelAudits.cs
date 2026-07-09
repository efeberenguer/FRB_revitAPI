// ══════════ PURPOSE

/* 
The purpose of this script is to automate the extraction and analysis of data from RVT model audits.

The script is structured in two parts:
- The first part collects the data, either via user input or from the RVT model.
- The second part provides a text-based output listing all the non-conforming items and, where relevant, a score for each section, which is then used to calculate a final score for the model.
*/

// ══════════ METHODS

// ═══════ HELPER METHOD

private static void AddInformation(
	List<string> list,
	int sectionNumber,
	ref int subsectionNumber,
	string title,
	string value,
	string actionRequired)
{
	string prefix = $"{sectionNumber}.{subsectionNumber}";
	
	string message;
	
	if (string.IsNullOrWhiteSpace(value))
	{
		message = $"═══ {prefix} {title} ═══\nAction Required - {actionRequired}\n";
	}
	else
	{
		message = $"═══ {prefix} {title} ═══\n{value}\n";
	}
	list.Add(message);
	
	subsectionNumber++;
}

// ══════════ DATA EXTRACTION AND PROCESSING

ProjectInfo projectInfo = doc.ProjectInformation; // The Document object

// ═══════ GENERAL INFORMATION

List<string> generalInformation = new List<string>();

// ═════ AUDITOR

Parameter modelAuditorParam = projectInfo.LookupParameter("AAI_ModelAuditor");

int sectionNumber = 1;
int subsectionNumber = 1;

AddInformation(
	generalInformation,
	sectionNumber,
	ref subsectionNumber,
	"Model Auditor",
	modelAuditorParam?.AsString(),
	"Shared parameter AAI_ModelAuditor to be set up and/or populated.");
	
// ═════ PROJECT NUMBER

AddInformation(
	generalInformation,
	sectionNumber,
	ref subsectionNumber,
	"Project Number",
	projectInfo.Number,
	"Project Number to be populated.");

// ═════ PROJECT NAME

AddInformation(
	generalInformation,
	sectionNumber,
	ref subsectionNumber,
	"Project Name",
	projectInfo.Name,
	"Project Name to be populated.");

// ═════ BIM LEAD

Parameter BIMLeadParam = projectInfo.LookupParameter("AAI_BIMLead");

AddInformation(
	generalInformation,
	sectionNumber,
	ref subsectionNumber,
	"BIM Lead",
	BIMLeadParam?.AsString(),
	"Shared parameter AAI_BIMLead to be set up and/or populated.");

// ═════ PROJECT LEAD

Parameter projectLeadParam = projectInfo.LookupParameter("AAI_ProjectLead");

AddInformation(
	generalInformation,
	sectionNumber,
	ref subsectionNumber,
	"Project Lead",
	projectLeadParam?.AsString(),
	"Shared parameter AAI_ProjectLead to be set up and/or populated.");

// ═════ MODEL DESCRIPTION

Parameter modelDescriptionParam = projectInfo.LookupParameter("AAI_ModelDescription");

AddInformation(
	generalInformation,
	sectionNumber,
	ref subsectionNumber,
	"Model Description",
	modelDescriptionParam?.AsString(),
	"Shared parameter AAI_ModelDescription to be set up and/or populated.");

// ═════ FILE NAME

AddInformation(
	generalInformation,
	sectionNumber,
	ref subsectionNumber,
	"File Name",
	doc.Title,
	"N/A");

// ═════ FILE SIZE

Parameter fileSizeParam = projectInfo.LookupParameter("AAI_FileSize");

AddInformation(
	generalInformation,
	sectionNumber,
	ref subsectionNumber,
	"File Size",
	fileSizeParam?.AsString(),
	"Shared parameter AAI_FileSize to be set up and/or populated.");

// ═════ 1.9 ISSUE DATE

DateTime today = DateTime.Today; // Returns the current date
string YYYY = today.ToString("yyyy");
string MM = today.ToString("MM");
string DD = today.ToString("dd");
string issueDate = YYYY + "-" + MM + "-" + DD;

AddInformation(
	generalInformation,
	sectionNumber,
	ref subsectionNumber,
	"Issue Date",
	issueDate,
	"N/A");

// ═══════ INFORMATION

subsectionNumber = 1;
sectionNumber++;

List<string> information = new List<string>();

// ═════ SPLASH SCREEN & MODEL INFORMATION

try
{
ViewSheet splashScreen = new FilteredElementCollector(doc)
    .OfClass(typeof(ViewSheet))
    .Cast<ViewSheet>()
    .FirstOrDefault(s =>
        s.Name == "ProjectSplashScreen" &&
        s.SheetNumber == "XXXXX");
        
string splashScreenSummary =
	splashScreen != null &&
	new FilteredElementCollector(doc, splashScreen.Id)
	.OfCategory(BuiltInCategory.OST_TitleBlocks)
	.OfClass(typeof(FamilyInstance))
	.Cast<FamilyInstance>()
	.Any(tb => tb.Symbol.Family.Name == "AAI_TBK_ProjectSplashScreen")
		? "YES"
		: null;

AddInformation(
	information,
	sectionNumber,
	ref subsectionNumber,
	"Splash Screen",
	splashScreenSummary,
	"Review Splash Screen Setup");
}
catch (Exception ex)
{
	TaskDialog.Show("Splash Screen", ex.ToString());
}
// ═════ REVIT VERSION

try
{
Parameter revitVersionParam = projectInfo.LookupParameter("AAI_RevitVersion");

AddInformation(
	information,
	sectionNumber,
	ref subsectionNumber,
	"Revit Version",
	revitVersionParam?.AsString(),
	"Shared parameter AAI_RevitVersion to be set up and/or populated.");
}
catch (Exception ex)
{
	TaskDialog.Show("Revit Version", ex.ToString());
}

// ═════ AUTODESK DESKTOP CONNECTOR VERSION

Parameter AutodeskDesktopConnectorVersionParam = projectInfo.LookupParameter("AAI_AutodeskDesktopConnectorVersion");

AddInformation(
	information,
	sectionNumber,
	ref subsectionNumber,
	"Autodesk Desktop Connector Version",
	AutodeskDesktopConnectorVersionParam?.AsString(),
	"Shared parameter AAI_AutodeskDesktopConnectorVersion to be set up and/or populated.");

// ═════ COORDINATES

/*
BasePoint projectBasePoint = BasePoint.GetProjectBasePoint(doc);

double northSouth = projectBasePoint
	.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM)
	.AsDouble();
	
double eastWest = projectBasePoint
	.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM)
	.AsDouble();
	
double northSouthMetric =
	UnitUtils.ConvertFromInternalUnits(
		northSouth,
		UnitTypeId.Meters);
		
double eastWestMetric =
	UnitUtils.ConvertFromInternalUnits(
		eastWest,
		UnitTypeId.Meters);
	
Parameter northSouthParam = projectInfo.LookupParameter("AAI_Coordinates_North");
Parameter eastWestParam = projectInfo.LookupParameter("AAI_Coordinates_East");

double northSouthParamDouble = northSouthParam.ToDouble();
double eastWestParamDouble = northSouthParam.ToDouble();

bool northSouthCheck;
bool eastWestCheck;

northSouthMetric == northSouthParamDouble ? northSouthCheck = true : northSouthCheck = false;
eastWestMetric == eastWestParamDouble ? eastWestCheck = true : eastWestCheck = false;

string coordinates = $"{northSouthMetric:F3} m, {eastWestMetric:F3} m"

AddInformation(
	information,
	sectionNumber,
	ref subsectionNumber,
	"Coordinates",
	coordinates,
	"Shared parameter AAI_Coordinates_North and AAI_Coordinates_East to be reviewed against Project Base Point information.");

// ═════ ELEVATION

double elevation = projectBasePoint
	.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM)
	.AsDouble();
	
double elevationMetric =
	UnitUtils.ConvertFromInternalUnits(
		elevation,
		UnitTypeId.Meters);

Parameter elevationParam = projectInfo.LookupParameter("AAI_Coordinates_Elevation");

double elevationParamDouble = northSouthParam.ToDouble();

bool elevationCheck;

elevationMetric == elevationDouble ? elevationCheck = true : elevationCheck = false

// ═════ ANGLE TO TRUE NORTH


double angle = projectBasePoint
	.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM)
	.AsDouble();
		
double angleDegrees =
	UnitUtils.ConvertFromInternalUnits(
		angle,
		UnitTypeId.Degrees);	

Parameter angleParam = projectInfo.LookupParameter("AAI_Coordinates_RotationTrueNorth");
*/
// ═════ 2.5 COPY MONITOR

// ═════ 2.6 PUBLISHED SETS

// ═══════ 3 KNOWN ISSUES

// ═══════ 4 AAI STANDARDS

// ═════ 4.1 DWG LINKED

// ═════ 4.2 FILLED REGIONS

// ═════ 4.3 LINE PATTERNS

// ═════ 4.4 LINE STYLES

// ═════ 4.5 MATERIALS

// ═════ 4.6 NAMING CONVENTION

// ═════ 4.7 OBJECT STYLES

// ═════ 4.8 PHASES

// ═════ 4.9 PROJECT BROWSER

// ═════ 4.10 REVIT LINKS

// ═════ 4.11 REVISIONS

// ═════ 4.12 SHEET ISSUED REVISIONS

// ═════ 4.13 WORKSETS

// ═══════ 5 FAMILIES

// ═════ 5.1 FT CONTENT

// ═════ 5.2 GENERIC MODELS

// ═════ 5.3 MODEL IN-PLACE

// ═════ 5.4 SHARED PARAMETERS

// ═════ 5.5 LARGEST FAMILY SIZE

// ═══════ 6 PERFORMANCE

// ═════ 6.1 AREAS

// ═════ 6.2 DESIGN OPTIONS

// ═════ 6.3 DESIGN OPTION SETS

// ═════ 6.4 DETAIL GROUPS

// ═════ 6.5 DETAIL ITEMS

// ═════ 6.6 DETAIL LINES

// ═════ 6.7 DWG IMPORTED

// ═════ 6.7 FILTERS

// ═════ 6.7 GRIDS

// ═════ 6.7 IMAGES

// ═════ 6.7 LEVELS

// ═════ 6.7 LOADABLE FAMILIES

// ═════ 6.7 MODEL GROUPS

// ═════ 6.7 MODEL HEALTH

// ═════ 6.7 MODEL LINES

// ═════ 6.7 PURGE ELEMENTS

// ═════ 6.7 REFERENCE PLANES

// ═════ 6.7 ROOMS

// ═════ 6.7 SCHEDULES

// ═════ 6.7 SCOPE BOXES

// ═════ 6.7 SHEETS

// ═════ 6.7 TAGS

// ═════ 6.7 TEXT NOTES

// ═════ 6.7 VIEW TEMPLATES

// ═════ 6.7 VIEWS

// ═════ 6.7 VIEWS ON SHEETS

// ═════ 6.7 WARNINGS

// ═══════ 7 CONCLUSION

// ═══════ 8 NAMING CONVENTION

// ══════════ AUDIT OUTPUT

Console.WriteLine($"AAI MODEL AUDIT");

Console.WriteLine($"\n1 GENERAL INFORMATION\n");

foreach (string message in generalInformation)
{
	Console.WriteLine(message);
}

Console.WriteLine($"\n2 INFORMATION\n");

foreach (string message in information)
{
	Console.WriteLine(message);
}

/*
Console.WriteLine($"\n2 INFORMATION");

Console.WriteLine($"\n2.1 Splash Screen/Model Information:       {splashScreenSummary}" + 
				  $"\n2.2 Revit Version:                         {revitVersion}" +
				  $"\n2.3 Autodesk Desktop Connector Version:    " +
				  $"\n2.4 Coordinates:                           " +
				  $"\n2.5 Copy Monitor:                          " +
				  $"\n2.6 Published Sets:                        ");

Console.WriteLine($"\n3 KNOWN ISSUES");

Console.WriteLine($"\n4 AAI STANDARDS");

Console.WriteLine($"\n4.1 DWG Linked: " + 
				  $"\n4.2 Filled Regions:  " +
				  $"\n4.3 Line Patterns: " +
				  $"\n4.4 Line Styles: " +
				  $"\n4.5 Materials: " +
				  $"\n4.6 Naming Convention: " +
				  $"\n4.7 Object Styles: " +
				  $"\n4.8 Phases: " +
				  $"\n4.9 Project Browser: " +
				  $"\n4.10 Revit Links: " +
				  $"\n4.11 Revisions: " +
				  $"\n4.12 Sheet Issued Revisions: " +
				  $"\n4.13 Worksets: ");

Console.WriteLine($"\n5 FAMILIES");

Console.WriteLine($"\n5.1 FT Content: " +
				  $"\n5.2 Generic Models:  " +
				  $"\n5.3 Model In-Place:  " +
				  $"\n5.4 Shared Parameters:  " +
				  $"\n5.5 Largest Family Size: ");

Console.WriteLine($"\n6 PERFORMANCE");

Console.WriteLine($"\n6.1 Areas: " +
				  $"\n6.2 Design Options:  " +
				  $"\n6.3 Design Option Sets:  " +
				  $"\n6.4 Detail Groups:  " +
				  $"\n6.5 Detail Items:  " +
				  $"\n6.6 Detail Lines:  " +
				  $"\n6.7 DWG Imported:  " +
				  $"\n6.8 Filters:  " +
				  $"\n6.9 Grids:  " +
				  $"\n6.10 Images:  " +
				  $"\n6.11 Levels:  " +
				  $"\n6.12 Loadable Families:  " +
				  $"\n6.13 Model Groups:  " +
				  $"\n6.14 Model Health:  " +
				  $"\n6.15 Model Lines:  " +
				  $"\n6.16 Purge Elements:  " +
				  $"\n6.17 Reference Planes:  " +
				  $"\n6.18 Rooms:  " +
				  $"\n6.19 Schedules:  " +
				  $"\n6.20 Scope Boxes:  " +
				  $"\n6.21 Sheets:  " +
				  $"\n6.22 Tags:  " +
				  $"\n6.23 Text Notes:  " +
				  $"\n6.24 View Templates:  " +
				  $"\n6.25 Views:  " +
				  $"\n6.26 Views on sheets:  " +
				  $"\n6.27 Warnings: ");

Console.WriteLine($"\n7 CONCLUSION");
*/
