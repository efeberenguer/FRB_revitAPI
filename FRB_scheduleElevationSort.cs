// ═════ PURPOSE ═════

/* 
The purpose of this script is to automate the process of populating the parameter AAI_LevelElevationSort which is used to provide a numerical value for sorting elements from linked models based on the elevation value of the level where they are hosted.

The parameter AAI_LevelElevationSort is a shared parameter to be applied to the following categories:

- Areas
- Doors
- Rooms
- Specialty Equipment (smoke and fire curtains, shutters, garage doors, ...)

*/

// ═══ STEP 1 - AREAS ═══

// ═══ STEP 2 - INTERNAL DOORS ═══


int doorsMissingParam = 0;
int doorsReadOnly = 0;
int doorsChanged = 0;


// Filtered element collector to output all door instances that belong to a type that begins with "AAI_DOR_Int"

using (Transaction t = new Transaction (doc, "Set door elevation parameter"))
{
	t.Start();
	
	List<FamilyInstance> doors = new FilteredElementCollector(doc)
		.OfCategory(BuiltInCategory.OST_Doors)
		.WhereElementIsNotElementType()
		.Cast<FamilyInstance>()
		.Where (d =>
			d.Symbol.Family.Name.StartsWith("AAI_DOR_Int"))
		.ToList();
		
	TaskDialog.Show(
		"DEBUG",
		$"Doors Found: {doors.Count}");
		
	// The outputs of the filtered element collector are grouped by level
	
	var groupedDoors = doors.GroupBy(d => d.LevelId);
	
	// The doors are processed level by level
	
	foreach (var group in groupedDoors)
	{
		Level level = doc.GetElement(group.Key) as Level;
		
		if (level == null)
			continue;
			
		double levelElevationMetric = Math.Round((level.Elevation)*304.8);
		//Console.WriteLine($"{levelName} - Elevation: {levelElevation}"); // test code to see that output values match expectations
		foreach (FamilyInstance door in group)
		{
			Parameter p = door.LookupParameter("AAI_LevelElevation");
			
			if (p == null)
			{
				doorsMissingParam++;
				continue;			
			}
			
			if (p.IsReadOnly)
			{
				doorsReadOnly++;
				continue;
			}
			
			bool result = p.Set(levelElevationMetric.ToString());
			
			if (result)
				doorsChanged++;
		}
	}
	t.Commit();
}

TaskDialog.Show(
	"Door Level Elevation Update Summary",
	$"Missing Parameter: {doorsMissingParam}\n" +
	$"Read Only: {doorsReadOnly}\n" +
	$"Updated: {doorsChanged}");
	

// ═══ STEP 3 - CLADDING DOORS ═══

// ═══ STEP 5 - CLADDING WINDOWS ═══

// ═══ STEP 5 - ROOMS ═══

// ═══ STEP 6 - SPECIALTY EQUIPMENT ═══
