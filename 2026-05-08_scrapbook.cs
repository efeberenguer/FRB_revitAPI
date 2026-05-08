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
	
	// the unprocessed list of rooms from the model for each type of lift
	
	List<Room> roomsFromModel = new List<Room>();
	
	Console.WriteLine($"\n═══ ROOMS FROM MODEL ═══\n");
	foreach (Room r in group)
	{
		string s = r.Number.ToString();
		Console.WriteLine($"{s}");
		roomsFromModel.Add(r);
	}
	
	// the previous list sorted by room number
	
	List<Room> sortedRoomsFromModel = roomsFromModel
    .OrderBy(r => r.Number)
    .ToList();
    
    List<string> sortedRoomsFromModelString = new List<string>();
    
    Console.WriteLine($"\n═══ SORTED ROOMS FROM MODEL ═══\n");
    foreach (Room r in sortedRoomsFromModel)
	{
		string s = r.Number.ToString();
		Console.WriteLine($"{s}");
		roomsFromModel.Add(r);
		sortedRoomsFromModelString.Add($"{s}");
	}
	
	
    
    // the list of sequential room numbers (i.e. how the list should look like)
	
	Console.WriteLine($"\n═ SEQUENTIAL ROOM NUMBERS ═\n");
	
	List<string> sequentialRoomNumbers = new List<string>();
	
	for(int i = 1; i <= groupCount; i++)
	{
	 	string s = $"{groupKey}-{i.ToString("D2")}";
	 	Console.WriteLine($"{s}");
	 	sequentialRoomNumbers.Add(s);
	}
	
	// move rooms that don't match the sequential room number to a separate list
	
	List<Room> roomsToRenumber = new List<Room>();
	
	Console.WriteLine($"\n═ ROOMS TO RENUMBER (NON-COMPLIANT ROOM NUMBER) ═\n");
	
	foreach (Room r in sortedRoomsFromModel.ToList())
	{
		string s = r.Number.ToString();
		
		if (!sequentialRoomNumbers.Contains(s))
		{
			Console.WriteLine($"{s}");
			roomsToRenumber.Add(r);
			sortedRoomsFromModel.Remove(r);
		}
		
	}
	
	// remove rooms with redundant room numbers to a separate list
	
	List<Room> duplicateRooms = sortedRoomsFromModel
	    .GroupBy(r => r.Number)
	    .Where(g => g.Count() > 1)
	    .SelectMany(g => g.Skip(1))
	    .ToList();
	    
	Console.WriteLine($"\n═ ROOMS TO RENUMBER (NON-COMPLIANT & REDUNDANT ROOM NUMBER) ═\n");
	
	roomsToRenumber.AddRange(duplicateRooms);
	
	foreach (Room r in roomsToRenumber)
	{
		string s = r.Number.ToString();
		Console.WriteLine($"{s}");		
	}
	
	// split list of sequential room number strings into a separate list for the rooms to renumber
	
	List<string> availableRoomNumbers = new List<string>();
	
	foreach (Room r in  sortedRoomsFromModel)
	{
		string s = r.Number.ToString();
		Console.WriteLine($"{s}");
	}
	

}
    
// 2.2 - groups?
/*
foreach (var group in liftRooms)
{
	string groupKey = group.Key; // returns the variable for naming each lift type group
	
	Console.WriteLine($"\n═══ {groupKey} LIFTS ═══");
		
	// creates a list of rooms for querying and changing parameters information
	List<Room> L1r = new List<Room>();
	
	foreach (Room r in group)
	{
		L1r.Add(r);
	}
	
	List<Room> L1r = collector
    .Cast<Room>()
    .OrderBy(r => r.Number)
    .ToList();
	
	Console.WriteLine($"\n═ ROOMS FROM MODEL (L1s) ═\n");	
	
	foreach (Room r in L1r)
	{
		string s = r.Number.ToString();
		Console.WriteLine($"{s}");
	}

	// returns list of sequential room numbers
	
	List<string> L1s = new List<string>();
	
	Console.WriteLine($"\n═ SEQUENTIAL ROOM NUMBERS (L1s) ═\n");
	
	int groupCount = group.Count(); // returns the variable to provide a list of sequential room numbers
	
	for(int i = 1; i <= groupCount; i++)
	{
	 	string s = $"{groupKey}-{i.ToString("D2")}";
	 	Console.WriteLine($"{s}");
	 	L1s.Add(s);
	}
	
	// moves rooms with non-compliant room numbers to roomsToRenumberList
	
	Console.WriteLine($"\n═ ROOMS TO RENUMBER (NON-COMPLIANT ROOM NUMBER) ═\n");
	
	foreach (Room r in L2r.ToList())
	{
		string s = r.Number.ToString();
		if (!L1s.Contains(s))
		{
			Console.WriteLine($"{s}");
			roomsToRenumber.Add(r);
			L2r.Remove(r);
		}
		
		Console.WriteLine($"{s}");
	}
	
	// moves rooms with duplicate room numbers to roomsToRenumberList
	
	Console.WriteLine($"\n═ ROOMS TO RENUMBER (DUPLICATE ROOM NUMBER) ═\n");

}
	
	
	// Generate L2 (list of actual room numbers)
	
	List<string> L2 = new List<string>();
	
	
	Console.WriteLine($"\n═ ACTUAL ROOM NUMBERS (L2) ═\n");
	
	foreach (Room r in roomList)
	{
		string s = r.Number.ToString();
		Console.WriteLine($"{s}");
		L2.Add(s);
	}		
	
	// Remove items from L2 that don't exist in L1
	
	Console.WriteLine($"\n═ INCORRECT ROOM NUMBERS (L2) ═\n");
	
	foreach (string item in L2.ToList())
	{
	    if (!L1.Contains(item))
	    {
	        Console.WriteLine($"{item}");
	        L2.Remove(item);
	    }
	}
	
	// Remove items from L1 that are in L2
	
	Console.WriteLine($"\n═ REDUNDANT SEQUENTIAL ROOM NUMBERS (L1) ═\n");
	
	foreach (string item in L1.ToList())
	{
	    if (L2.Contains(item))
	    {
	        Console.WriteLine($"{item}");
	        L1.Remove(item);
	    }
	    if (!L2.Contains(item))
	    {
	        L2.Add(item);
	    }
	}
	
	// Confirm revised L2
	
	Console.WriteLine($"\n═ CORRECT ROOM NUMBERS (L2) ═\n");
	
	L2.Sort();
	
	foreach (string item in L2.ToList())
	{
	    Console.WriteLine($"{item}");
	}
	
	// Rewrite room numbers
	
	foreach (Room r in roomList)
	{
		Parameter p = r.LookupParameter("Number");
		string s = p.AsString();
		if (L2.Contains(s))
		{
			Console.WriteLine($"Retained Room Number: {s}");
			//L2.RemoveAt(0);
		}
		if (!L2.Contains(s))
		{
			Console.WriteLine($"Modified Room Number: {s}");
			//p.Set(L2.First());
			L2.RemoveAt(0);
		}
	}
}
*/

// Examples from ChatGPT

// ==========================================
// RECAP: ALL ROOM SORTING + DUPLICATE LOGIC
// Revit API C# snippets from today's session
// ==========================================


// ------------------------------------------
// 1. GET ROOMS FROM PROJECT
// ------------------------------------------
List<Room> rooms = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Rooms)
    .WhereElementIsNotElementType()
    .Cast<Room>()
    .Where(r => r.Area > 0)
    .ToList();


// ------------------------------------------
// 2. SORT ROOMS BY ROOM NUMBER (STRING SORT)
// ------------------------------------------
List<Room> sortedRooms = rooms
    .OrderBy(r => r.Number)
    .ToList();


// DESCENDING SORT
List<Room> sortedRoomsDesc = rooms
    .OrderByDescending(r => r.Number)
    .ToList();


// ------------------------------------------
// 3. NATURAL SORT (NUMERIC ROOM NUMBERS)
// ------------------------------------------
using System.Text.RegularExpressions;

List<Room> naturalSortedRooms = rooms
    .OrderBy(r =>
    {
        Match m = Regex.Match(r.Number ?? "", @"\d+");
        return m.Success ? int.Parse(m.Value) : int.MaxValue;
    })
    .ThenBy(r => r.Number)
    .ToList();


// ------------------------------------------
// 4. GROUP + FIND DUPLICATE ROOM NUMBERS
// ------------------------------------------

// Get all rooms with duplicate numbers (includes ALL duplicates)
List<Room> duplicateRooms = rooms
    .GroupBy(r => r.Number)
    .Where(g => g.Count() > 1)
    .SelectMany(g => g)
    .ToList();


// ------------------------------------------
// 5. GET ONLY DUPLICATE ROOM NUMBERS (STRINGS)
// ------------------------------------------
List<string> duplicateNumbers = rooms
    .GroupBy(r => r.Number)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key)
    .ToList();


// ------------------------------------------
// 6. MOVE DUPLICATES TO SEPARATE LIST (KEEP ALL)
// ------------------------------------------
List<Room> duplicateRoomsList = new List<Room>();

duplicateRoomsList.AddRange(
    rooms
        .GroupBy(r => r.Number)
        .Where(g => g.Count() > 1)
        .SelectMany(g => g)
);


// ------------------------------------------
// 7. REMOVE ALL DUPLICATES FROM ORIGINAL LIST
// ------------------------------------------
var duplicates = rooms
    .GroupBy(r => r.Number)
    .Where(g => g.Count() > 1)
    .SelectMany(g => g)
    .ToList();

rooms.RemoveAll(r =>
    duplicates.Any(d => d.Id == r.Id));


// ------------------------------------------
// 8. KEEP ONLY FIRST INSTANCE, MOVE REST
// ------------------------------------------

// redundant rooms = everything except first in each group
List<Room> redundantRooms = rooms
    .GroupBy(r => r.Number)
    .SelectMany(g => g.Skip(1))
    .ToList();

// separate list
List<Room> duplicateRoomsOnlySecondPlus = new List<Room>();
duplicateRoomsOnlySecondPlus.AddRange(redundantRooms);

// remove from original list
rooms.RemoveAll(r =>
    redundantRooms.Any(rr => rr.Id == r.Id));


// ------------------------------------------
// 9. OPTIMIZED VERSION (FASTER REMOVAL)
// ------------------------------------------
HashSet<ElementId> redundantIds = rooms
    .GroupBy(r => r.Number)
    .SelectMany(g => g.Skip(1))
    .Select(r => r.Id)
    .ToHashSet();

List<Room> duplicateRoomsOptimized = rooms
    .Where(r => redundantIds.Contains(r.Id))
    .ToList();

rooms.RemoveAll(r =>
    redundantIds.Contains(r.Id));
