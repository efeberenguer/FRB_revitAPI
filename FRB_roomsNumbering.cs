// ═════ PURPOSE ═════

/* 
The purpose of this script is to automate the process of numbering rooms in Revit models following the Adamson Associates (International) Ltd (AAI) standards as described in document AAIUK-AAI-DB-XX-DR-A-00010_P02.

This script works by adding the rooms to different filtered element collectors using the parameter "Occupancy". This is a text parameter applied to rooms by instance. Below there is a list of the values required for each room category to be renamed as per AAI standards:
- Risers: 					RISER
- Stairs: 					STAIR
- Lifts
    - Bicycle Lift: 		CL
    - Dumbwaiter: 			DW
    - Evacuation Lift: 		EL
    - Firefighter's Lift: 	FF
    - Goods Lift: 			GL
    - Lifting Platform: 	LP
    - Passenger Lift: 		PL
    - Stair Lift: 			SL
- All other room types: 	ROOM
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

// 2.1 - Creation of a filtered element collector grouping the rooms by Occupancy input

var liftRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Where(r => 
        {
            var pattern = r.LookupParameter("Occupancy")?.AsString();
            return pattern == "CL" || pattern == "DW" || pattern == "EL" || pattern == "FF" || pattern == "GL" || pattern == "LP" || pattern == "PL" || pattern == "SL";
        })
    .GroupBy(r => r.LookupParameter("Occupancy").AsString());

foreach (var group in liftRooms)
{
	string groupKey = group.Key; // returns the variable for naming each lift type group
	
	int groupCount = group.Count(); // returns the variable to provide a list of sequential room numbers
	
	Console.WriteLine($"\n═══ {groupKey} LIFTS ═══");
	
	// 2.2 - Gets the source (unprocessed) list of rooms from the model for each type of lift
	
	List<Room> sourceRooms = new List<Room>();
	
	Console.WriteLine($"\n═══ SOURCE ROOMS FROM MODEL ═══\n");
	foreach (Room r in group)
	{
		string s = r.Number.ToString();
		Console.Write($"{s}, ");
		sourceRooms.Add(r);
	}
	
	// 2.3 - Sorts the previous list of rooms by number
	
	List<Room> sourceRoomsSorted = sourceRooms
    .OrderBy(r => r.Number)
    .ToList();
    
	Console.WriteLine($"\n\n═══ SORTED ROOMS FROM MODEL ═══\n");
	
	foreach (Room r in sourceRoomsSorted)
	{
		string s = r.Number.ToString();
		Console.Write($"{s}, ");
	}
	
	// 2.4 - Empty string list that will hold all the correct sequential room numbers
	List<string> sequentialRoomNumbers = new List<string>(); 	
	
	Console.WriteLine($"\n\n═══ SEQUENTIAL ROOM NUMBERS ═══\n");
	
	for (int i = 1; i <= groupCount; i++)
	{
		string s = $"{groupKey}-{i.ToString("D2")}";
		Console.Write($"{s}, ");
		sequentialRoomNumbers.Add(s);
	}
	
	// 2.5 - Empty list where the rooms with non-compliant room numbers will be sent for processing
	List<Room> nonCompliantRooms = new List<Room>(); 
	
	// 2.6 - Checks for either non-compliant room numbers or duplicate room numbers in the sorted room numbers list
	// this criteria is based on whether the room numbers have a match in the sequential list or not
	
	Console.WriteLine($"\n\n═══ ROOM NUMBERS EVALUATION ═══\n");
	
	// The hash set is for checking for duplicates
	
	HashSet<string> seenRoomNumbers = new HashSet<string>();	
	
	for (int i = groupCount - 1; i >= 0; i--)
	{
		Room r = sourceRoomsSorted[i];
		string roomNumber = r.Number;
		
		// If Add() returns false, the number already exists
		if (!seenRoomNumbers.Add(roomNumber))
		{
			Console.WriteLine($"Duplicate room - Number: {roomNumber}");
			nonCompliantRooms.Add(r);
			sourceRoomsSorted.RemoveAt(i);
		}
		else if(!sequentialRoomNumbers.Contains(roomNumber))
		{
			Console.WriteLine($"Non-compliant room - Number: {roomNumber}");
			nonCompliantRooms.Add(r);
			sourceRoomsSorted.Remove(r);
		}
		else
		{
			Console.WriteLine($"Retained room - Number: {roomNumber}");
		}
	}
	
	// 2.7 - Modifies the sequential room numbers list based on the retained rooms	
	
	foreach (string s in sequentialRoomNumbers.ToList())
	{
		foreach (Room r in sourceRoomsSorted)
		{
			string roomNumber = r.Number;
			
			if(s == roomNumber)
			{
				Console.WriteLine($"Redundant sequential room number: {s}");
				sequentialRoomNumbers.Remove(s);
			}
			else
			{
				Console.WriteLine($"Required sequential room number: {s}");
			}
		}
	}
	
	// 2.8 - Renumbers the non-compliant rooms
	
	foreach (Room r in nonCompliantRooms)
	{
		Parameter roomNumber = r.LookupParameter("Number");
		string s = sequentialRoomNumbers[0];
		sequentialRoomNumbers.RemoveAt(0);
		roomNumber.Set(s);
	}
	
	// 2.9 - Confirs the renumbered rooms
	
	Console.WriteLine($"\n\n═══ RENUMBERED ROOMS ═══\n");
	
	foreach (Room r in nonCompliantRooms)
	{
		string roomNumber = r.Number;
		Console.WriteLine($"Renumbered room: {roomNumber}");
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
