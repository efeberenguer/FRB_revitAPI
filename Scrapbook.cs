
// 2026-05-06 - Changes to room numbering script

// ═══ STEP 1 - RISERS ═══

// 1.1 - Creation of a filtered element collector to get list L1

List<Room> riserRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Where(r => r.LookupParameter("Occupancy")?.AsString() == "RISER")
    .ToList();
        
// 1.2 - Creation of list L2

int riserRoomsL2Count = riserRooms.Count;

List<string> riserRoomsL2 = new List<string>();

for(int i = 1; i <= riserRoomsL2Count; i++)
{
 	string riserRoomsL2index = $"R{i.ToString("D2")}";
 	riserRoomsL2.Add(riserRoomsL2index);
 	Console.WriteLine($"{riserRoomsL2index}");
}

// 1.2 - Renumber rooms from filtered element collector 
/*
int roomIndex = 1;
foreach (Room riserRoom in riserRooms)
{
    string roomIndexString = roomIndex.ToString("D2");
    Parameter riserRoomsNumber = riserRoom.LookupParameter("Number");
    riserRoomsNumber.Set($"R{roomIndexString}");
    roomIndex++;
}*/
