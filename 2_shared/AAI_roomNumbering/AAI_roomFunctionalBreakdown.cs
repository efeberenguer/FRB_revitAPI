// Stage 1 - Check whether AAI_03_FunctionalBreakdown is available on Rooms.

const string roomParameterName = "AAI_03_FunctionalBreakdown";

bool parameterExists = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Any(room => room.LookupParameter(roomParameterName) != null);
	
if (!parameterExists)
{
	TaskDialog.Show(
		"Functional Breakdown",
		$"Parameter '{roomParameterName}' was not found on Rooms.\n\n" +
		"The Functional Breakdown process will be skipped.");
		
	return Result.Succeeded;
}

// Parameter exists - continue with the rest of the process.
TaskDialog.Show(
	"Functional Breakdown",
	$"Parameter '{roomParameterName}' was found on Rooms.\n\n" +
	"The Functional Breakdown process can continue.");
	
// Stage 2 - Find Functional Breakdown working plan views 

const string viewNamePrefix = "FRB_WorkingPlan_LEVEL";

List<ViewPlan> workingPlanViews = new FilteredElementCollector(doc)
	.OfClass(typeof(ViewPlan))
	.Cast<ViewPlan>()
	.Where(v =>
		!v.IsTemplate &&
		v.Name.StartsWith(
			viewNamePrefix,
			StringComparison.OrdinalIgnoreCase))
	.ToList();
	
// Report results

if (workingPlanViews.Count == 0)
{
	TaskDialog.Show(
		"Functional Breakdown",
		$"No plan views starting with '{viewNamePrefix}' were found.");
		
	return Result.Succeeded;
}

string viewList = string.Join(
	Environment.NewLine,
	workingPlanViews.Select(v =>
		$"{v.Name}    |    Level: {v.GenLevel.Name ?? "No Level"}"));
		
TaskDialog.Show(
	"Functional Breakdown",
	$"Found {workingPlanViews.Count} working plan view(s): \n\n{viewList}");