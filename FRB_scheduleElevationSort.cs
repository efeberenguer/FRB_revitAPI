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

// Filtered element collector to output all door instances that belong to a type that begins with "AAI_DOR_Int"

List<FamilyInstance> doors = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Doors)
	.WhereElementIsNotElementType()
	.Cast<FamilyInstance>()
	.Where (d =>
		d.Symbol.FamilyName.StartsWith("AAI_DOR_Int"))
	.ToList();
	
// The outputs of the filtered element collector are grouped by level

var groupedDoors = doors.GroupBy(d => d.LevelId);

// The elevation value of each level is extracted

foreach (var group in groupedDoors)
{
	Level level = doc.GetElement(group.Key) as Level;
	string levelName = level.Name;
	double levelElevationMetric = Math.Round((level.Elevation)*304.8);
	string levelElevation = levelElevationMetric.ToString();
	foreach (FamilyInstance door in group)
	
	Console.WriteLine($"{levelName} - Elevation: {levelElevation}"); // test code to see that output values match expectations
}

	

// ═══ STEP 3 - CLADDING DOORS ═══

// ═══ STEP 5 - CLADDING WINDOWS ═══

// ═══ STEP 5 - ROOMS ═══

// ═══ STEP 6 - SPECIALTY EQUIPMENT ═══
