// ═════ PURPOSE ═════

/* 
The purpose of this script is to automate the process of numbering rooms in Revit models following the Adamson Associates (International) Ltd (AAI) standards as described in document AAIUK-AAI-DB-XX-DR-A-00010_P02.

This script numbers the room based on the order in which they were created. If specific rooms are required to follow a sequential numbering they also have to be placed in a specific order. 

This script works by adding the rooms to different filtered element collectors using the parameter "AAI_RoomNumberingPattern". This is a text parameter applied to rooms by instance. Below there is a list of the values required for each room category to be renamed as per AAI standards:
- Risers: RISER
- Stairs: STAIR
- Lifts
    - Bicycle Lift: CL
    - Dumbwaiter: DW
    - Evacuation Lift: EL
    - Firefighter's Lift: FF
    - Goods Lift: GL
    - Lifting Platform: LP
    - Passenger Lift: PL
    - Stair Lift: SL
- All other room types: ROOM
*/

// ═══ STEP 1 - RISERS ═══

// 1.1 - Creation of a filtered element collector 

List<Room> riserRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Where(r => r.LookupParameter("Occupancy")?.AsString() == "RISER")
    .ToList();

// 1.2 - Renumber rooms from filtered element collector 

int roomIndex = 1;
foreach (Room riserRoom in riserRooms)
{
    string roomIndexString = roomIndex.ToString("D2");
    Parameter riserRoomsNumber = riserRoom.LookupParameter("Number");
    riserRoomsNumber.Set($"R{roomIndexString}");
    roomIndex++;
}

// ═══ STEP 2 - LIFTS ═══

// 2.1 - Creation of a filtered element collector 

var liftRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Where(r => 
        {
            var pattern = r.LookupParameter("Occupancy")?.AsString();
            return pattern == "CL" || pattern == "DW" || pattern == "EL" || pattern == "FF" || pattern == "GL" || pattern == "LP" || pattern == "PL" || pattern == "SL";
        })
    .GroupBy(r => r.LookupParameter("AAI_RoomNumberingPattern").AsString());

// 2.2 - Renumber rooms from filtered element collector 

foreach (var group in liftRooms)
{
		int roomIndex = 1;
	foreach (Room liftRooms in group)
	{		
		Parameter liftRoomType = liftRooms.LookupParameter("AAI_RoomNumberingPattern");
        string liftRoomTypeString = liftRoomType.AsString();
        string roomIndexString = roomIndex.ToString("D2");
        Parameter liftRoomsNumber = liftRooms.LookupParameter("Number");
		liftRoomsNumber.Set($"{liftRoomTypeString}-{roomIndexString}");
		roomIndex++;        
	}
}

// ═══ STEP 3 - STAIRS ═══

// 3.1 - Creation of a filtered element collector 

var stairRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Where(r => r.LookupParameter("Occupancy")?.AsString() == "STAIR")
	.GroupBy(r => r.Level.Name);

// 3.2 - Renumber rooms from filtered element collector 

Console.WriteLine("\nSTEP 3.2 TEST PRINT\n");

Console.WriteLine("Rooms grouped by level:");
foreach (var group in stairRooms)
{
	Console.WriteLine($"\n{group.Key}: {group.Count()} stair rooms");
	int roomIndex = 1;
	foreach (Room stairRooms in group)
	{
		Level roomLevel = stairRooms.Level;
		string roomLevelString = roomLevel.Name.ToString();
		string roomIndexString = roomIndex.ToString("D2");		
		Parameter stairRoomsNumber = stairRooms.LookupParameter("Number");
		stairRoomsNumber.Set($"{roomLevelString}-ST{roomIndexString}");
        roomIndex++;
	}
}

// ═══ STEP 4 - ROOMS ═══

// 4.1 - Creation of a filtered element collector 

var allOtherRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Where(r => r.LookupParameter("Occupancy")?.AsString() == "ROOM")
	.GroupBy(r => r.Level.Name);

// 4.2 - Renumber rooms from filtered element collector 

foreach (var group in allOtherRooms)
{
	
	int roomIndex = 1;
	foreach (Room allOtherRooms in group)
	{
		Level roomLevel = allOtherRooms.Level;
		string roomLevelString = roomLevel.Name.ToString();
		string roomIndexString = roomIndex.ToString("D2");
		roomIndex++;
		Parameter otherRoomsNumber = allOtherRooms.LookupParameter("Number");
		otherRoomsNumber.Set($"{roomLevelString}-{roomIndexString}");
	}
}
