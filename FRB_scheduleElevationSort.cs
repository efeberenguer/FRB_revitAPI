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
int doorsPopulated = 0;

using (Transaction t2 = new Transaction (doc, "Set door elevation parameter"))
{
	t2.Start();
	
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
			
		// returns the elevation in mm
		double levelElevationMetric = Math.Round((level.Elevation)*304.8);
		
		foreach (FamilyInstance door in group)
		{
			Parameter p = door.LookupParameter("AAI_LevelElevation");
			
			if (p == null)
			{
				doorsMissingParam++;
				continue;			
			}
			
			else if (p.IsReadOnly)
			{
				doorsReadOnly++;
				continue;
			}
			
			else if (p.AsString() == levelElevationMetric.ToString())
			{
				doorsPopulated++;
				continue;
			}
			else
			{
				p.Set(levelElevationMetric.ToString());
				doorsChanged++;
			}				
		}
	}
	t2.Commit();
}

TaskDialog.Show(
	"Door Level Elevation Update Summary",
	$"Missing Parameter: {doorsMissingParam}\n" +
	$"Read Only: {doorsReadOnly}\n" +
	$"Already Populated: {doorsPopulated}\n" +
	$"Updated: {doorsChanged}");

// ═══ STEP 3 - CLADDING DOORS ═══

int extDoorsMissingParam = 0;
int extDoorsReadOnly = 0;
int extDoorsChanged = 0;
int extDoorsPopulated = 0;

// Filtered element collector to output all door instances that belong to a type that begins with "AAI_DOR_Int"

using (Transaction t3 = new Transaction (doc, "Set door elevation parameter"))
{
	t3.Start();
	
	List<FamilyInstance> extDoors = new FilteredElementCollector(doc)
		.OfCategory(BuiltInCategory.OST_Doors)
		.WhereElementIsNotElementType()
		.Cast<FamilyInstance>()
		.Where (d =>
			d.Symbol.Family.Name.StartsWith("AAI_DOR"))
		.ToList();
		
	TaskDialog.Show(
		"DEBUG",
		$"Doors Found: {extDoors.Count}");
		
	// The outputs of the filtered element collector are grouped by level
	
	var groupedExtDoors = extDoors.GroupBy(d => d.LevelId);
	
	// The doors are processed level by level
	
	foreach (var group in groupedExtDoors)
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
				extDoorsMissingParam++;
				continue;			
			}			
			else if (p.IsReadOnly)
			{
				extDoorsReadOnly++;
				continue;
			}
			else if (p.AsString() == levelElevationMetric.ToString())
			{
				extDoorsPopulated++;
				continue;
			}
			else
			{
				p.Set(levelElevationMetric.ToString());
				extDoorsChanged++;
			}	
		}
	}
	t3.Commit();
}

TaskDialog.Show(
	"External Doors Level Elevation Update Summary",
	$"Missing Parameter: {extDoorsMissingParam}\n" +
	$"Read Only: {extDoorsReadOnly}\n" +	
	$"Already Populated: {extDoorsPopulated}\n" +
	$"Updated: {extDoorsChanged}");

// ═══ STEP 5 - CLADDING WINDOWS ═══

// ═══ STEP 5 - ROOMS ═══

// ═══ STEP 6 - SPECIALTY EQUIPMENT ═══
