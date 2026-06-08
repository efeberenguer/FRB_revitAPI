
// 2026-05-08 - Ptential new structure for room numbering script

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
    
// 2.2 - groups?

foreach (var group in liftRooms)
{
	string groupKey = group.Key;
	int groupCount = group.Count();
	
	Console.WriteLine($"\n═══ {groupKey} LIFTS ═══");
	
	// Generate L1 (list of sequential room numbers)
	
	List<string> L1 = new List<string>();
	
	Console.WriteLine($"\n═ SEQUENTIAL ROOM NUMBERS (L1) ═\n");
	
	for(int i = 1; i <= groupCount; i++)
	{
	 	string s = $"{groupKey}-{i.ToString("D2")}";
	 	Console.WriteLine($"{s}");
	 	L1.Add(s);
	}
	
	// Generate L2 (list of actual room numbers)
	
	List<string> L2 = new List<string>();
	List<Room> roomList = new List<Room>();
	
	foreach (Room r in group)
	{
		roomList.Add(r);
	}
	
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
	
	// HashSet structure for removing duplicates from L2, then back to list
	// Source: https://www.educative.io/answers/how-to-remove-duplicates-from-a-list-in-c-sharp
	
	HashSet<string> L2A = new HashSet<string>(L2);
	
	List<string> L2B = L2A.ToList();
	
	Console.WriteLine($"\n═ RETAINED ROOM NUMBERS ═\n");
	
	foreach (string s in L2B)
	{
		Console.WriteLine($"{s}");
	}	
	
	// Regenerate L2 from L2B
	
	L2.Clear();
	L2.AddRange(L2B);
	
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
	
	Console.WriteLine($"\n═ PROCESSING SUMMARY ═\n");
	
	foreach (Room r in roomList)
	{
		Parameter p = r.LookupParameter("Number");
		string s = p.AsString();
		if (L2.Contains(s))
		{
			Console.WriteLine($"{s}		RETAINED");
			L2.RemoveAt(0);
		}
		if (!L2.Contains(s))
		{
			Console.WriteLine($"{s}		MODIFIED TO		{L2.First()}");
			//p.Set(L2.First());
			L2.RemoveAt(0);
		}
	}
}

// 2026-05-06 - New structure for room numbering script

// ═══ STEP 1 - RISERS ═══

// 1.1 - Creation of a filtered element collector 

List<Room> riserRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Where(r => r.LookupParameter("Occupancy")?.AsString() == "RISER")
    .ToList();
    
// 1.2 - Generate and print list of initial room numbers (L1)

List<string> L1 = new List<string>();

Console.WriteLine("═ INITIAL ROOM NUMBERS FOR RISERS ═\n");

foreach (Room r in riserRooms)
{
	string s = r.Number.ToString();
	Console.WriteLine($"Room Number: {s}");	
	L1.Add(s);
}

// 1.3 - Print list of sequential room numbers (L2)

List<string> L2 = new List<string>();

int riserRoomsCount = riserRooms.Count();

Console.WriteLine("\n═ SEQUENTIAL ROOM NUMBERS FOR RISERS ═\n");

for(int i = 1; i <= riserRoomsCount; i++)
{
	string s = $"R{i.ToString("D2")}";
	Console.WriteLine($"Room Number: {s}");
	L2.Add(s);
}

// 1.4 - Remove items from L1 that don't match L2 (riser rooms to renumber)

foreach (Room r in riserRooms.ToList())
{
	string s = r.Number.ToString();
	if(L2.Contains(s))
	{
		riserRooms.Remove(r);
	}
}

Console.WriteLine("\n═ RISER ROOMS TO RENUMBER ═\n");

foreach (Room r in riserRooms)
{
	string s = r.Number.ToString();
	Console.WriteLine($"Room Number: {s}");
}

// 1.5 - Remove items from L2 that exist in L1

Console.WriteLine("\n═ SEQUENTIAL RISER ROOM NUMBERS TO KEEP ═\n");

foreach (string s in L2.ToList())
{
    if (L1.Contains(s))
    {
        L2.Remove(s);
    }
}

foreach (string s in L2)
{
    Console.WriteLine($"{s}");
}

// 1.6 - Renumber remaining risers with remaining serial numbers

Console.WriteLine("\n═ RISERS RENUMBERED ═\n");
foreach (Room r in riserRooms)
{
	Parameter p = r.LookupParameter("Number");
	string s = L2.First();
	p.Set(s);
	Console.WriteLine($"Room Number: {s}");
	L2.RemoveAt(0);
}

// 2026-05-06 - Changes to room numbering script

// Strategy for checking if items in a list are sequentially arranged

List<string> L1 = new List<string>() { "R01", "R03", "R04", "R05", "R08", "RAA", "RBB", "RCC"};
List<string> L2 = new List<string>() { "R01", "R02", "R03", "R04", "R05", "R06", "R07", "R08"};

// Initial conditions check

Console.WriteLine("═════ INITIAL CONDITIONS ═════\n");

Console.WriteLine("═══ LIST L1 ═══\n");

int L1index = 0;
foreach (string item in L1)
{
	Console.WriteLine($"Item index: {L1index} - {item}");
	L1index++;
}


Console.WriteLine("\n\n═══ LIST L2 ═══\n");

int L2index = 0;
foreach (string item in L2)
{
	Console.WriteLine($"Item index: {L2index} - {item}");
	L2index++;
}

// Remove items from L1 that don't exist in L2

Console.WriteLine("\n\n═════ REMOVE ITEMS FROM L1 THAT DON'T EXIST IN L2 ═════");

foreach (string item in L1.ToList())
{
    if (!L2.Contains(item))
    {
        L1.Remove(item);
    }
}

Console.WriteLine("\n\n═══ REVISED LIST L1 ═══\n");

L1index = 0;
foreach (string item in L1)
{
	Console.WriteLine($"Item index: {L1index} - {item}");
	L1index++;
}

Console.WriteLine("\n\n═══ LIST L2 ═══\n");

L2index = 0;
foreach (string item in L2)
{
	Console.WriteLine($"Item index: {L2index} - {item}");
	L2index++;
}


// Remove items from L2 that exist in L1

Console.WriteLine("\n\n═════ REMOVE REDUNDANT ITEMS FROM L2 ═════");

foreach (string item in L2.ToList())
{
    if (L1.Contains(item))
    {
        L2.Remove(item);
    }
}

Console.WriteLine("\n\n═══ REVISED LIST L2 ═══");

L2index = 0;
foreach (string item in L2)
{
	Console.WriteLine($"Item index: {L2index} - {item}");
	L2index++;
}
L2index = 0;

// Merge items from L2 into L1

Console.WriteLine("\n\n═════ MERGE L2 INTO L1 ═════");

foreach (string item in L2.ToList())
{
    L1.Add(item);
}

Console.WriteLine("\n\n═══ FINAL LIST L1 ═══\n");

L1index = 0;
foreach (string item in L1)
{
	Console.WriteLine($"Item index: {L1index} - {item}");
	L1index++;
}
