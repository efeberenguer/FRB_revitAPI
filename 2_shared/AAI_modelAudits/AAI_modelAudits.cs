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
report.AppendLine("@page {");
report.AppendLine("	size: A4;");
report.AppendLine("	margin: 20mm;");
report.AppendLine("	}");
report.AppendLine();
report.AppendLine("body {");
report.AppendLine();
report.AppendLine("font-family: Cascadia Code;");
report.AppendLine();
report.AppendLine("}");
report.AppendLine();
report.AppendLine("table {");
report.AppendLine("    width: 100%;");
report.AppendLine("    border-collapse: collapse;");
report.AppendLine("}");
report.AppendLine("");
report.AppendLine("th, td {");
report.AppendLine("    padding: 6px, 8px;");
report.AppendLine("    text-align: left;");
report.AppendLine("");
report.AppendLine("    border-left: none;");
report.AppendLine("    border-right: none;");
report.AppendLine("");
report.AppendLine("    border-bottom: 1px solid black;");
report.AppendLine("}");
report.AppendLine("");
report.AppendLine("th {");
report.AppendLine("    border-bottom: 2px solid black;");
report.AppendLine("}");
report.AppendLine("tr:hover {background-color: coral;}");


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
/*



// Subsection: Coordinates
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Coordinates");

// Subsection: Copy Monitor
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Copy Monitor");

// Subsection: Published Sets
	
Header2(report, ref sectionNumber, ref subsectionNumber, "Published Sets");

subsectionNumber = 0;

*/

Header1(report, ref sectionNumber, "GENERAL INFORMATION");

private static void CStoHTMLTableTypeAHeader(
	StringBuilder report)
{
	report.AppendLine("<tr>");
	report.AppendLine("		<th colspan=\"2\">Reference</th>");
	report.AppendLine("		<th >Output</th>");
	report.AppendLine("</tr>");
}

report.AppendLine("<table>");
report.AppendLine("<colgroup>");
report.AppendLine("		<col style=\"width: 7.7%\">");
report.AppendLine("		<col style=\"width: 46.2%\">");
report.AppendLine("		<col style=\"width: 46.2%\">");
report.AppendLine("</colgroup>");

CStoHTMLTableTypeAHeader(report);

private static void WriteValueCheck(
	StringBuilder report,
	ref int sectionNumber,
	ref int subsectionNumber,
	string reference,
	string value,
	string actionRequired)
{
	string output = string.IsNullOrWhiteSpace(value)
		? $"Action Required → {actionRequired}"
		: value;
		
	subsectionNumber++;
	
	report.AppendLine("<tr>");
	report.AppendLine($"		<td>{sectionNumber}.{subsectionNumber}</td>");
	report.AppendLine($"		<td>{reference}</td>");
	report.AppendLine($"		<td>{output}</td>");
	report.AppendLine("</tr>");
}

private static void WriteParameterCheck(
	StringBuilder report,
	ref int sectionNumber,
	ref int subsectionNumber,
	Element element,
	string parameterName,
	string reference,
	string actionRequired)
{
	Parameter p = element.LookupParameter(parameterName);
	
	WriteValueCheck(
		report,
		ref sectionNumber,
		ref subsectionNumber,
		reference,
		p?.AsString(),
		actionRequired);
}

// Model Auditor

WriteParameterCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	projectInfo,
	"AAI_ModelAuditor",
	"Model Auditor",
	"Confirm that the shared parameter AAI_ModelAuditor is set up and populated.");

// Project Number

WriteValueCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	"Project Number",
	projectInfo.Number,
	"Project Number to be populated.");
	
// Project Name

WriteValueCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	"Project Name",
	projectInfo.Name,
	"Project Name to be populated.");
	
// BIM Lead

WriteParameterCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	projectInfo,
	"AAI_BIMLead",
	"BIM Lead",
	"Confirm that the shared parameter AAI_BIMLead is set up and populated.");

// Project Manager

WriteParameterCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	projectInfo,
	"AAI_ProjectManager",
	"Project Manager",
	"Confirm that the shared parameter AAI_ProjectManager is set up and populated.");
	
// Model Description

WriteParameterCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	projectInfo,
	"AAI_ModelDescription",
	"Model Description",
	"Confirm that the shared parameter AAI_ModelDescription is set up and populated.");
	
// File Size

WriteParameterCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	projectInfo,
	"AAI_FileSize",
	"File Size (Mb)",
	"Confirm that the shared parameter AAI_FileSize is set up and populated.");
	
// Issue Date

private static void DateCheck(
	StringBuilder report,
	ref int sectionNumber,
	ref int subsectionNumber,
	string reference)
{
	DateTime today = DateTime.Today; // Returns the current date
	string YYYY = today.ToString("yyyy");
	string MM = today.ToString("MM");
	string DD = today.ToString("dd");
	string output = YYYY + "-" + MM + "-" + DD;
			
	subsectionNumber++;
	
	report.AppendLine("<tr>");
	report.AppendLine($"		<td>{sectionNumber}.{subsectionNumber}</td>");
	report.AppendLine($"		<td>{reference}</td>");
	report.AppendLine($"		<td>{output}</td>");
	report.AppendLine("</tr>");
}

DateCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	"Issue Date");

report.AppendLine("</table>");
report.AppendLine("<p style=\"page-break-after: always;\">&nbsp;</p>");

subsectionNumber = 0;

Header1(report, ref sectionNumber, "INFORMATION");

report.AppendLine("<table>");
report.AppendLine("<colgroup>");
report.AppendLine("		<col style=\"width: 7.7%\">");
report.AppendLine("		<col style=\"width: 46.2%\">");
report.AppendLine("		<col style=\"width: 46.2%\">");
report.AppendLine("</colgroup>");
report.AppendLine("<tr>");
report.AppendLine("		<th colspan=\"2\">Reference</th>");
report.AppendLine("		<th >Output</th>");
report.AppendLine("</tr>");

// Splash Screen

private static void SplashScreenCheck(
	Document doc,
	StringBuilder report,
	ref int sectionNumber,
	ref int subsectionNumber,
	string reference)
{
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
	
	string output =
	    hasSplashTitleBlock ? "Set Up" : "Action Required → Review Splash Screen setup.";
	
	subsectionNumber++;
	
	report.AppendLine("<tr>");
	report.AppendLine($"		<td>{sectionNumber}.{subsectionNumber}</td>");
	report.AppendLine($"		<td>{reference}</td>");
	report.AppendLine($"		<td>{output}</td>");
	report.AppendLine("</tr>");
}

SplashScreenCheck(
	doc,
	report,
	ref sectionNumber,
	ref subsectionNumber,
	"Splash Screen & Model Information");
	
// Revit version

WriteParameterCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	projectInfo,
	"AAI_RevitVersion",
	"Revit Version",
	"Confirm that the shared parameter AAI_RevitVersion is set up and populated.");
	
// Autodesk Desktop Connector version

WriteParameterCheck(
	report,
	ref sectionNumber,
	ref subsectionNumber,
	projectInfo,
	"AAI_AutodeskDesktopConnectorVersion",
	"Dekstop Connector Version",
	"Confirm that the shared parameter AAI_AutodeskDesktopConnectorVersion is set up and populated.");

report.AppendLine("	<tr>");
report.AppendLine("		<td>2.4</td>");
report.AppendLine("		<td>Project Base Point Coordinates (N/S, E/W)(mm)</td>");
report.AppendLine("		<td>181705000.0, 532725000.0</td>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>2.5</td>");
report.AppendLine("		<td>Copy Monitor</td>");
report.AppendLine("		<td>Set up</td>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>2.6</td>");
report.AppendLine("		<td>Published Sets</td>");
report.AppendLine("		<td>Set up</td>");
report.AppendLine("	</tr>");
report.AppendLine("</table>");

Header1(report, ref sectionNumber, "AAI STANDARDS");

report.AppendLine("<table>");
report.AppendLine("	<tr>");
report.AppendLine("		<th style=\"width:7.7%\">Reference</th>");
report.AppendLine("		<th style=\"width:30.8%\"> </th>");
report.AppendLine("		<th style=\"width:30.8%\">Output</th>");
report.AppendLine("		<th style=\"width:30.8%\">Score (%)</th>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>3.1</td>");
report.AppendLine("		<td>Linked DWGs</td>");
report.AppendLine("		<td>1</td>");
report.AppendLine("		<td>74.0</td>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>3.2</td>");
report.AppendLine("		<td>Filled Regions</td>");
report.AppendLine("		<td>7480</td>");
report.AppendLine("		<td>0.1</td>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>3.3</td>");
report.AppendLine("		<td>Line Patterns</td>");
report.AppendLine("		<td>87</td>");
report.AppendLine("		<td>4.2</td>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>3.3</td>");
report.AppendLine("		<td>Line Styles</td>");
report.AppendLine("		<td>121</td>");
report.AppendLine("		<td>3.1</td>");
report.AppendLine("	</tr>");
report.AppendLine("</table>");

Header1(report, ref sectionNumber, "FAMILIES");

report.AppendLine("<table>");
report.AppendLine("	<tr>");
report.AppendLine("		<th style=\"width:7.7%\">Reference</th>");
report.AppendLine("		<th style=\"width:30.8%\"> </th>");
report.AppendLine("		<th style=\"width:30.8%\">Output</th>");
report.AppendLine("		<th style=\"width:30.8%\">Score (%)</th>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>4.1</td>");
report.AppendLine("		<td>FT Content</td>");
report.AppendLine("		<td align=right>99</td>");
report.AppendLine("		<td align=right>3.7</td>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>4.2</td>");
report.AppendLine("		<td>Generic Models</td>");
report.AppendLine("		<td align=right>4587</td>");
report.AppendLine("		<td align=right>0.1</td>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>4.3</td>");
report.AppendLine("		<td>Model In-Place</td>");
report.AppendLine("		<td align=right>6</td>");
report.AppendLine("		<td align=right>35.9</td>");
report.AppendLine("	</tr>");
report.AppendLine("</table>");

Header1(report, ref sectionNumber, "PERFORMANCE");

report.AppendLine("<table>");
report.AppendLine("	<tr>");
report.AppendLine("		<th style=\"width:7.7%\">Reference</th>");
report.AppendLine("		<th style=\"width:30.8%\"> </th>");
report.AppendLine("		<th style=\"width:30.8%\">Output</th>");
report.AppendLine("		<th style=\"width:30.8%\">Score (%)</th>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>4.1</td>");
report.AppendLine("		<td>Detail Groups</td>");
report.AppendLine("		<td align=right>681</td>");
report.AppendLine("		<td align=right>0.6</td>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>4.2</td>");
report.AppendLine("		<td>Detail Items</td>");
report.AppendLine("		<td align=right>9963</td>");
report.AppendLine("		<td align=right>0.0</td>");
report.AppendLine("	</tr>");
report.AppendLine("	<tr>");
report.AppendLine("		<td>4.3</td>");
report.AppendLine("		<td>Detail Lines</td>");
report.AppendLine("		<td align=right>34529</td>");
report.AppendLine("		<td align=right>0.0</td>");
report.AppendLine("	</tr>");
report.AppendLine("</table>");

Header1(report, ref sectionNumber, "OUTSTANDING ACTIONS");

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