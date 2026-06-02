// ═════ PURPOSE ═════

/* 
The purpose of this script is to automate the process of populating the parameter AAI_LevelElevationSort which is used to provide a numerical value for sorting elements from linked models based on the elevation value of the level where they are hosted.

The parameter AAI_LevelElevationSort is a shared parameter to be applied to the following categories:

- Areas
- Doors
- Rooms
- Specialty Equipment (smoke and fire curtains, shutters, garage doors, ...)

*/

// ═══ CLADDING DOORS ═══

int missingParamExtDoors = 0;
int readOnlyExtDoors = 0;
int changedExtDoors = 0;
int populatedExtDoors = 0;

// Filtered element collector to output all door instances that belong to a type that begins with "AAI_DOR"

using (Transaction tExtDoors = new Transaction (doc, "Set external doors elevation parameter"))
{
	tExtDoors.Start();
	
	List<FamilyInstance> extDoors = new FilteredElementCollector(doc)
		.OfCategory(BuiltInCategory.OST_Doors)
		.WhereElementIsNotElementType()
		.Cast<FamilyInstance>()
		.Where (d =>
			d.Symbol.Family.Name.StartsWith("AAI_DOR"))
		.ToList();
		
	TaskDialog.Show(
		"DEBUG",
		$"External Doors Found: {extDoors.Count}");
		
	// The outputs of the filtered element collector are grouped by level
	
	var groupedExtDoors = extDoors.GroupBy(d => d.LevelId);
	
	// The doors are processed level by level
	
	foreach (var group in groupedExtDoors)
	{
		Level extDoorLevel = doc.GetElement(group.Key) as Level;
		
		if (extDoorLevel == null)
			continue;
			
		double extDoorsLevelElevationMetric = Math.Round((extDoorLevel.Elevation)*304.8);
		string extDoorsTargetValue = extDoorsLevelElevationMetric.ToString();		

		foreach (FamilyInstance door in group)
		{
			Parameter extDoorsP = door.LookupParameter("AAI_LevelElevation");
			
			if (extDoorsP == null)
			{
				missingParamExtDoors++;
				continue;			
			}			
			else if (extDoorsP.IsReadOnly)
			{
				readOnlyExtDoors++;
				continue;
			}
			else if (string.Equals(extDoorsP.AsString(), extDoorsTargetValue))
			{
				populatedExtDoors++;
				continue;
			}
			else
			{
				extDoorsP.Set(extDoorsLevelElevationMetric.ToString());
				changedExtDoors++;
			}	
		}
	}
	tExtDoors.Commit();
}

TaskDialog.Show(
	"External Doors Level Elevation Update Summary",
	$"Missing Parameter: {missingParamExtDoors}\n" +
	$"Read Only: {readOnlyExtDoors}\n" +	
	$"Already Populated: {populatedExtDoors}\n" +
	$"Changed: {changedExtDoors}");
