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

Parameter AAI_ModelAuditorParam = projectInfo.LookupParameter("AAI_ModelAuditor");

string modelAuditor;

if (AAI_ModelAuditorParam == null)
{
    modelAuditor = "Not Defined (Missing Shared Parameter AAI_ModelAuditor)";
}
else
{
    modelAuditor = AAI_ModelAuditorParam.AsString();
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
// SUGGESTION: Turn into shared project parameter

string BIMLead = "";

FormResult BIMLeadForm = UI.CreateCustomForm("AAI RVT Model Audit", 400, 250, form =>
                                           {
                                             form.AddHeader("General Information");
                                             form.AddTextInput("Enter BIM Lead name and surname:", "");
                                           });
                                           
if (BIMLeadForm.Success)
{
	BIMLead = BIMLeadForm.GetStringResult("Enter BIM Lead name and surname:");
	Console.WriteLine($"BIM LEAD: {BIMLead}");
}
else
{
	Console.WriteLine("BIM Lead input cancelled by the user");
}
string BIMLeadOutput = $"\n1.4 BIM Lead: {BIMLead}";

// ═════ 1.5 PROJECT LEAD
// SUGGESTION: Turn into shared project parameter

string projectLead = "";

FormResult projectLeadForm = UI.CreateCustomForm("AAI RVT Model Audit", 400, 250, form =>
                                           {
                                             form.AddHeader("General Information");
                                             form.AddTextInput("Enter Project Lead name and surname:", "");
                                           });
                                           
if (projectLeadForm.Success)
{
	projectLead = projectLeadForm.GetStringResult("Enter Project Lead name and surname:");
	Console.WriteLine($"PROJECT LEAD: {projectLead}");
}
else
{
	Console.WriteLine("Project Lead input cancelled by the user");
}

string projectLeadOutput = $"\n1.5 Project Lead: {projectLead}";

// ═════ 1.6 MODEL DESCRIPTION

Parameter AAI_ModelDescriptionParam = projectInfo.LookupParameter("AAI_ModelDescription");
string AAI_ModelDescription = AAI_ModelDescriptionParam.AsString();
string AAI_ModelDescriptionOutput = $"\n1.6 Model Description: {AAI_ModelDescription}";

// ═════ 1.7 FILE NAME

string modelName = doc.Title;
string modelNameOutput = $"\n1.7 Model Name: {modelName}";

// ═════ 1.8 FILE SIZE

string fileSizeText = "";
double fileSize = 0;

FormResult fileSizeForm = UI.CreateCustomForm("AAI RVT Model Audit", 400, 250, form =>
                                           {
                                             form.AddHeader("General Information");
                                             form.AddTextInput("Enter file size (MB):", "");
                                           });
                                           
if (fileSizeForm.Success)
{
	fileSizeText = fileSizeForm.GetStringResult("Enter file size (MB):");
	fileSize = double.Parse(fileSizeText);
	Console.WriteLine($"FILE SIZE: {fileSize}");
}
else
{
	Console.WriteLine("File size input cancelled by the user");
}

string fileSizeOutput = $"\n1.8 File Size: {fileSize:N1}";

// ═════ 1.9 ISSUE DATE

DateTime today = DateTime.Today; // Returns the current date
string YYYY = today.ToString("yyyy");
string MM = today.ToString("MM");
string DD = today.ToString("dd");
string auditDateOutput = $"\n1.9 Issue Date: {YYYY}-{MM}-{DD}";

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
				 );

Console.WriteLine($"{modelAuditorOutput}" +
				 $"{projectNumberOutput}" +
				  $"{projectNameOutput}" +
				  $"{BIMLeadOutput}" +
				  $"{projectLeadOutput}" +
				  $"{AAI_ModelDescriptionOutput}" +
				  $"{fileSizeOutput}" +
				  $"{auditDateOutput}");
