/*
═══════════════════════════════════ PURPOSE ════════════════════════════════════

The purpose of this script is to:
- Extract the audit data from the working models
- Evaluate the performance of the model based on the data
- Provide a list of actionable directions to improve the performance
*/

/*
════════════════════════════ 1. GENERAL INFORMATION ════════════════════════════

═════════════════════════════════ 1.1 AUDITOR ══════════════════════════════════

Parameter required to differentiate between auditor and BIM Lead

*/

string modelAuditor = "Francisco Berenguer";

/*
══════════════════════════════ 1.2 PROJECT NUMBER ══════════════════════════════
*/

ProjectInfo projectInfo = doc.ProjectInformation;

string projectNumber = projectInfo.Number;

if (projectNumber == null)
	{
		return null;
	}
	
/*
═══════════════════════════════ 1.3 PROJECT NAME ═══════════════════════════════
*/

string projectName = projectInfo.Name;

if (projectNumber == null)
	{
		return null;
	}

/*
═════════════════════════════════ 1.4 BIM LEAD ═════════════════════════════════

*/

Parameter AAI_BIMLeadParam = projectInfo.LookupParameter("AAI_BIMLead");

string AAI_BIMLead;

if (AAI_BIMLeadParam == null)
	{
		return null;
	}
else
	{
		AAI_BIMLead = AAI_BIMLeadParam.AsString();
	}

/*
═════════════════════════════ 1.5 PROJECT MANAGER ══════════════════════════════

Separate parameter required to differentiate between auditor and BIM Lead

*/

string projectLead = "Nora Ceaki";

/*
══════════════════════════════ 1.6 MODEL ANALYSED ══════════════════════════════
*/

DateTime today = DateTime.Today; 

string currentYear = today.ToString("yyyy");
string currentMonth = today.ToString("MM");
string currentDay = today.ToString("dd");

string currentDate = currentYear + "-" + currentMonth + "-" + currentDay;

/*
═══   1.7 MODEL NAME   ═══
*/

string fullPath = doc.PathName;

string modelName = System.IO.Path.GetFileNameWithoutExtension(doc.PathName);

/*
═══   1.8 FILE SIZE   ═══
*/

FileInfo fi = new FileInfo(fullPath);

long bytes = fi.Length;
double mb = bytes/(1024*1024);

/*
═══   1.9 ISSUE DATE   ═══
*/

/*
═══   1.10 SCORE   ═══

*/

/*
═════     2. INFORMATION     ═════

═══   2.1 SPLASH SCREEN / MODEL INFORMATION   ═══

═══   2.2 REVIT VERSION   ═══

═══   3.3 AUTODESK DESKTOP CONNECTOR VERSION   ═══

═══   3.4 COORDINATES   ═══

═══   3.5 COPY MONITOR   ═══

═══   3.6 PUBLISHED SETS   ═══

*/

/*
═════     3. KNOWN ISSUES     ═════

*/

/*
═════     4. AAI STANDARDS     ═════

═══   4.1 LINKED DWG   ═══

═══   4.2 FILLED REGIONS   ═══

═══   4.3 LINE PATTERNS   ═══

═══   4.4 LINE STYLES   ═══

═══   4.5 MATERIALS   ═══

═══   4.6 NAMING CONVENTION   ═══

═══   4.7 OBJECT STYLES   ═══

═══   4.8 PHASES   ═══

═══   4.9 PROJECT BROWSER   ═══

═══   4.10 LINKED RVT   ═══

═══   4.11 REVISIONS   ═══

═══   4.12 SHEET ISSUED REVISIONS   ═══

═══   4.13 WORKSETS   ═══

*/

/*
═════     5. FAMILIES     ═════

═══   5.1 FT CONTENT   ═══

═══   5.2 GENERIC MODELS   ═══

═══   5.3 MODEL IN-PLACE   ═══

═══   5.4 SHARED PARAMETERS   ═══

═══   5.5 LARGEST FAMILY SIZE   ═══

*/

/*
═════     6. PERFORMANCE     ═════

═══   6.1 AREAS   ═══

═══   6.2 DESIGN OPTIONS   ═══

═══   6.3 DESIGN OPTION SETS   ═══

═══   6.4 DETAIL GROUPS   ═══

═══   6.5 DETAIL ITEMS   ═══

═══   6.6 DETAIL LINES   ═══

═══   6.7 DWG IMPORTED   ═══

═══   6.8 FILTERS   ═══

═══   6.9 GRIDS   ═══

═══   6.10 IMAGES   ═══

═══   6.11 LEVELS   ═══

═══   6.12 LOADABLE FAMILIES   ═══

═══   6.13 MODEL GROUPS   ═══

═══   6.14 MODEL HEALTH   ═══

═══   6.15 MODEL LINES   ═══

═══   6.16 PURGE ELEMENTS   ═══

═══   6.17 REFERENCE PLANES   ═══

═══   6.18 ROOMS   ═══

═══   6.19 SCHEDULES   ═══

═══   6.20 SCOPE BOXES   ═══

═══   6.21 SHEETS   ═══

═══   6.22 TAGS   ═══

═══   6.23 TEXT NOTES   ═══

═══   6.24 VIEW TEMPLATES   ═══

═══   6.25 VIEWS   ═══

═══   6.26 VIEWS ON SHEETS   ═══

═══   6.27 WARNINGS   ═══

*/

/*
═════     7. CONCLUSION     ═════
*/

/*
═════     8. NAMING CONVENTION     ═════
*/

/*
═════     9. OUTPUT     ═════
*/

Console.WriteLine($"═════     1. CURRENT GENERAL INFORMATION     ═════");

Console.Write($"\n1.1    Auditor:           {modelAuditor}");
Console.Write($"\n1.2    Project Number:    {projectNumber}");
Console.Write($"\n1.3    Project Name:      {projectName}");
Console.Write($"\n1.4    BIM Lead:          {AAI_BIMLead}");
Console.Write($"\n1.5    Project Lead:      {projectLead}");
Console.Write($"\n1.6    Model Analysed:    {currentDate}");
Console.Write($"\n1.7    Model Name:        {modelName}");
Console.Write($"\n1.8    File Size:         {mb:F2} MB");
Console.Write($"\n1.9    Issue Date:        {currentDate}");
Console.Write($"\n1.10   Audit Score: ");

