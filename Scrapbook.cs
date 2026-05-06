
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
