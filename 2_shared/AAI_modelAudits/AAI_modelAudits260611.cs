// ══════════ PURPOSE

/* 
The purpose of this script is to automate the extraction and analysis of data from RVT model audits.

The script is structured in two parts:
- The first part collects the data, either via user input or from the RVT model.
- The second part provides a text-based output listing all the non-conforming items and, where relevant, a score for each section, which is then used to calculate a final score for the model.
*/

// ══════════ DATA EXTRACTION AND PROCESSING

// ═══════ 1 GENERAL INFORMATION

// ═════ 1.1 AUDITOR

ProjectInfo projectInfo = doc.ProjectInformation; // The Document object

Parameter modelAuditorParam = projectInfo.LookupParameter("AAI_ModelAuditor");

string modelAuditor;

if (modelAuditorParam == null)
{
    modelAuditor = "Not Defined (Missing Shared Parameter AAI_ModelAuditor)";
}
else
{
    modelAuditor = modelAuditorParam.AsString();
}

// ═════ 1.2 PROJECT NUMBER

string projectNumber = projectInfo.Number;

if (string.IsNullOrWhiteSpace(projectNumber))
{
	projectNumber = "Not Defined (Project Number is Empty)";
}

// ═════ 1.3 PROJECT NAME

string projectName = projectInfo.Name;

if (string.IsNullOrWhiteSpace(projectName))
{
	projectNumber = "Not Defined (Project Name is Empty)";
}

// ═════ 1.4 BIM LEAD

Parameter BIMLeadParam = projectInfo.LookupParameter("AAI_BIMLead");

string BIMLead;

if (BIMLeadParam == null)
{
    BIMLead = "Not Defined (Missing Shared Parameter AAI_BIMLead)";
}
else
{
    BIMLead = BIMLeadParam.AsString();
}

// ═════ 1.5 PROJECT LEAD
Parameter projectLeadParam = projectInfo.LookupParameter("AAI_projectLead");

string projectLead;

if (projectLeadParam == null)
{
    projectLead = "Not Defined (Missing Shared Parameter AAI_projectLead)";
}
else
{
    projectLead = projectLeadParam.AsString();
}

// ═════ 1.6 MODEL DESCRIPTION

Parameter modelDescriptionParam = projectInfo.LookupParameter("AAI_ModelDescription");

string modelDescription;

if (modelDescriptionParam == null)
{
    modelDescription = "Not Defined (Missing Shared Parameter AAI_ModelDescription)";
}
else
{
    modelDescription = modelDescriptionParam.AsString();
}

// ═════ 1.7 FILE NAME

string modelName = doc.Title;

// ═════ 1.8 FILE SIZE

Parameter fileSizeParam = projectInfo.LookupParameter("AAI_fileSize");

string fileSize;

if (fileSizeParam == null)
{
    fileSize = "Not Defined (Missing Shared Parameter AAI_fileSize)";
}
else
{
    fileSize = fileSizeParam.AsDouble().ToString();
}

// ═════ 1.9 ISSUE DATE

DateTime today = DateTime.Today; // Returns the current date
string YYYY = today.ToString("yyyy");
string MM = today.ToString("MM");
string DD = today.ToString("dd");

// ═══════ 2 INFORMATION

// ═════ 2.1 SPLASH SCREEN & MODEL INFORMATION

// ═════ 2.2 REVIT VERSION

// ═════ 2.3 AUTODESK DESKTOP CONNECTOR VERSION

// ═════ 2.4 COORDINATES

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

Console.WriteLine($"\n1 GENERAL INFORMATION");

Console.WriteLine($"\n1.1 Model Auditor: {modelAuditor}" + 
				  $"\n1.2 Project Number: {projectNumber}" +
				  $"\n1.3 Project Name: {projectName}" +
				  $"\n1.4 BIM Lead: {BIMLead}" +
				  $"\n1.5 Project Lead: {projectLead}" +
				  $"\n1.6 Model Description: {modelDescription}" +
				  $"\n1.7 Model Name: {modelName}" +
				  $"\n1.8 File Size: {fileSize} (MB)" +
				  $"\n1.9 Issue Date: {YYYY}-{MM}-{DD}" +
				 );
