// ═══ 1. RISERS ═══

// 1.1 Filtered element collector of all riser rooms

List<Room> projectRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.Cast<Room>()
	.ToList();

List<Room> riserRooms = new List<Room>();
foreach (Room room in projectRooms)
{
	Parameter roomNumberingPatternParam = room.LookupParameter("AAI_RoomNumberingPattern");
	if (roomNumberingPatternParam.AsString() == "RISER")
	{
		riserRooms.Add(room);
	}
	//Console.WriteLine($"Room Name: {room.Name}");
	//Console.WriteLine($"Room Name: {roomNumberingPatternParam.AsString()}");
}
	
// 1.2 Set a variable to count all riser rooms

int riserRoomsCount = riserRooms.Count();
Console.WriteLine("STEP 1.2 TEST PRINT\n");
Console.WriteLine(riserRoomsCount.ToString());

// 1.3 Generate a list with the numbers for each riser room

List <string> riserRoomsNumberSequence = new List <string>();

for (int i = 1; i <= riserRoomsCount; i++)
{
	riserRoomsNumberSequence.Add(("R")+(i.ToString("D2")));
}

Console.WriteLine("\nSTEP 1.3 TEST PRINT\n");
foreach (string sourceRoom in riserRoomsNumberSequence)
{
	Console.WriteLine($"{sourceRoom}");
}

// 1.4 Set the room number parameter

for (int i = 0; i < riserRoomsNumberSequence.Count; i++)
{
	Parameter riserRoomNumber = riserRooms[i].LookupParameter("Number");
	riserRoomNumber.Set(riserRoomsNumberSequence[i]);
}

Console.WriteLine("\nSTEP 1.4 TEST PRINT\n");
foreach (var targetRoom in riserRooms)
{
		Console.WriteLine(targetRoom.LookupParameter("Number").AsString());
}

// ═══ 2. LIFTS ═══

// 2.1. Filtered element collector of all lifts by AAI_RoomNumberingPattern

// Generate an empty list for each type of lift

List<Room> liftsCL = new List<Room>();
List<Room> liftsDW = new List<Room>();
List<Room> liftsEL = new List<Room>();
List<Room> liftsFF = new List<Room>();
List<Room> liftsGL = new List<Room>();
List<Room> liftsLP = new List<Room>();
List<Room> liftsPL = new List<Room>();
List<Room> liftsSL = new List<Room>();

foreach (Room room in projectRooms)
{
	Parameter roomNumberingPatternParam = room.LookupParameter("AAI_RoomNumberingPattern");
	if (roomNumberingPatternParam.AsString() == "CL")
	{
		liftsCL.Add(room);
	}
	else if (roomNumberingPatternParam.AsString() == "DW")
	{
		liftsDW.Add(room);
	}
	else if (roomNumberingPatternParam.AsString() == "EL")
	{
		liftsEL.Add(room);
	}
	else if (roomNumberingPatternParam.AsString() == "FF")
	{
		liftsFF.Add(room);
	}
	else if (roomNumberingPatternParam.AsString() == "GL")
	{
		liftsGL.Add(room);
	}
	else if (roomNumberingPatternParam.AsString() == "LP")
	{
		liftsLP.Add(room);
	}
	else if (roomNumberingPatternParam.AsString() == "PL")
	{
		liftsPL.Add(room);
	}
	else if (roomNumberingPatternParam.AsString() == "SL")
	{
		liftsSL.Add(room);
	}
}

// 2.2 Set a variable to count all lift rooms

int liftsCLCount = liftsCL.Count();
int liftsDWCount = liftsDW.Count();
int liftsELCount = liftsEL.Count();
int liftsFFCount = liftsFF.Count();
int liftsGLCount = liftsGL.Count();
int liftsLPCount = liftsLP.Count();
int liftsPLCount = liftsPL.Count();
int liftsSLCount = liftsSL.Count();

Console.WriteLine("STEP 2.2 TEST PRINT\n");
Console.WriteLine("TOTAL LIFTS BY TYPE\n");
Console.WriteLine($"Bycicle Lifts (CL): {liftsCLCount.ToString()}");
Console.WriteLine($"Dumbwaiters (DW): {liftsCLCount.ToString()}");
Console.WriteLine($"Evacuation Lifts (EL): {liftsCLCount.ToString()}");
Console.WriteLine($"Firefighter's Lifts (FL): {liftsCLCount.ToString()}");
Console.WriteLine($"Goods Lift (GL): {liftsCLCount.ToString()}");
Console.WriteLine($"Lifting Platform (LP): {liftsCLCount.ToString()}");
Console.WriteLine($"Passenger Lift (PL): {liftsCLCount.ToString()}");
Console.WriteLine($"Stair Lift (SL): {liftsCLCount.ToString()}");

// 2.3 Generate a list with the numbers for each type of lift

// CL - Bycicle Lift

List <string> liftsCLSequence = new List <string>();

for (int i = 1; i <= liftsCLCount; i++)
{
	liftsCLSequence.Add(("CL-")+(i.ToString("D2")));
}

// DW - Dumbwaiter

List <string> liftsDWSequence = new List <string>();

for (int i = 1; i <= liftsDWCount; i++)
{
	liftsDWSequence.Add(("DW-")+(i.ToString("D2")));
}

// EL - Evacuation Lift

List <string> liftsELSequence = new List <string>();

for (int i = 1; i <= liftsELCount; i++)
{
	liftsELSequence.Add(("EL-")+(i.ToString("D2")));
}

// FF - Firefighter's Lift

List <string> liftsFFSequence = new List <string>();

for (int i = 1; i <= liftsFFCount; i++)
{
	liftsFFSequence.Add(("FF-")+(i.ToString("D2")));
}

// GL - Goods Lift

List <string> liftsGLSequence = new List <string>();

for (int i = 1; i <= liftsGLCount; i++)
{
	liftsGLSequence.Add(("GL-")+(i.ToString("D2")));
}

// LP - Lifting Platform

List <string> liftsLPSequence = new List <string>();

for (int i = 1; i <= liftsLPCount; i++)
{
	liftsLPSequence.Add(("LP-")+(i.ToString("D2")));
}

// PL - Passenger Lift

List <string> liftsPLSequence = new List <string>();

for (int i = 1; i <= liftsPLCount; i++)
{
	liftsPLSequence.Add(("PL-")+(i.ToString("D2")));
}

// SL - Stair Lift

List <string> liftsSLSequence = new List <string>();

for (int i = 1; i <= liftsSLCount; i++)
{
	liftsSLSequence.Add(("SL-")+(i.ToString("D2")));
}

Console.WriteLine("\nSTEP 2.3 TEST PRINT\n");

Console.WriteLine($"═ CL - Bycicle Lifts ═\n");
foreach (string CL in liftsCLSequence)
{
	Console.WriteLine($"{CL}");
}

Console.WriteLine($"\n═ DW - Dumbwaiters ═\n");
foreach (string DW in liftsDWSequence)
{
	Console.WriteLine($"{DW}");
}

Console.WriteLine($"\n═ EL - Evacuation Lifts ═\n");
foreach (string EL in liftsELSequence)
{
	Console.WriteLine($"{EL}");
}

Console.WriteLine($"\n═ FF - Firefighter's Lifts ═\n");
foreach (string FF in liftsFFSequence)
{
	Console.WriteLine($"{FF}");
}

Console.WriteLine($"\n═ GL - Goods Lifts ═\n");
foreach (string GL in liftsGLSequence)
{
	Console.WriteLine($"{GL}");
}

Console.WriteLine($"\n═ LP - Lifting Platforms ═\n");
foreach (string LP in liftsLPSequence)
{
	Console.WriteLine($"{LP}");
}

Console.WriteLine($"\n═ PL - Passenger Lifts ═\n");
foreach (string PL in liftsPLSequence)
{
	Console.WriteLine($"{PL}");
}

Console.WriteLine($"\n═ SL - Stair Lifts ═\n");
foreach (string SL in liftsSLSequence)
{
	Console.WriteLine($"{SL}");
}

// 2.4 Set the room number parameter

// CL - Bycicle Lift

Console.WriteLine("\nSTEP 2.4 TEST PRINT\n");

for (int i = 0; i < liftsCLSequence.Count; i++)
{
	Parameter CLRoomNumber = liftsCL[i].LookupParameter("Number");
	CLRoomNumber.Set(liftsCLSequence[i]);
	Parameter CLRoomName = liftsCL[i].LookupParameter("Name");
	CLRoomNumber.Set("BYCICLE LIFT");
}

foreach (Room CL in liftsCL)
{
	Console.WriteLine(CL.LookupParameter("Number").AsString());
}

// DW - Dumbwaiter

for (int i = 0; i < liftsDWSequence.Count; i++)
{
	Parameter DWRoomNumber = liftsDW[i].LookupParameter("Number");
	DWRoomNumber.Set(liftsDWSequence[i]);
}

foreach (Room DW in liftsDW)
{
	Console.WriteLine(DW.LookupParameter("Number").AsString());
}

// EL - Evacuation Lifts

for (int i = 0; i < liftsELSequence.Count; i++)
{
	Parameter ELRoomNumber = liftsEL[i].LookupParameter("Number");
	ELRoomNumber.Set(liftsELSequence[i]);
}

foreach (Room EL in liftsEL)
{
	Console.WriteLine(EL.LookupParameter("Number").AsString());
}

// FF - Firefighters Lifts

for (int i = 0; i < liftsFFSequence.Count; i++)
{
	Parameter FFRoomNumber = liftsFF[i].LookupParameter("Number");
	FFRoomNumber.Set(liftsFFSequence[i]);
}

foreach (Room FF in liftsFF)
{
	Console.WriteLine(FF.LookupParameter("Number").AsString());
}

// GL - Goods Lifts

for (int i = 0; i < liftsGLSequence.Count; i++)
{
	Parameter GLRoomNumber = liftsGL[i].LookupParameter("Number");
	GLRoomNumber.Set(liftsGLSequence[i]);
}

foreach (Room GL in liftsGL)
{
	Console.WriteLine(GL.LookupParameter("Number").AsString());
}

// LP - Lifting Platform

for (int i = 0; i < liftsLPSequence.Count; i++)
{
	Parameter LPRoomNumber = liftsLP[i].LookupParameter("Number");
	LPRoomNumber.Set(liftsLPSequence[i]);
}

foreach (Room LP in liftsLP)
{
	Console.WriteLine(LP.LookupParameter("Number").AsString());
}

// PL - Passenger Lift

for (int i = 0; i < liftsPLSequence.Count; i++)
{
	Parameter PLRoomNumber = liftsPL[i].LookupParameter("Number");
	PLRoomNumber.Set(liftsPLSequence[i]);
}

foreach (Room PL in liftsPL)
{
	Console.WriteLine(PL.LookupParameter("Number").AsString());
}

// SL - Stair Lift

for (int i = 0; i < liftsSLSequence.Count; i++)
{
	Parameter SLRoomNumber = liftsSL[i].LookupParameter("Number");
	SLRoomNumber.Set(liftsSLSequence[i]);
}

foreach (Room SL in liftsSL)
{
	Console.WriteLine(SL.LookupParameter("Number").AsString());
}

// ═══ 3. STAIRS ═══

// 3.1. Filtered element collector of all stairs by AAI_RoomNumberingPattern

var stairRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Where(r => r.LookupParameter("AAI_RoomNumberingPattern")?.AsString() == "STAIR")
	.GroupBy(r => r.Level.Name);

// 3.2. Set up value of room number

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
		roomIndex++;
		Parameter stairRoomsNumber = stairRooms.LookupParameter("Number");
		stairRoomsNumber.Set($"{roomLevelString}-ST{roomIndexString}");
		
		Console.WriteLine($" - {roomLevelString}-ST{roomIndexString}");
	}
}

// ═══ 4. ROOMS ═══

// 4.1. Filtered element collector of all stairs by AAI_RoomNumberingPattern

var allOtherRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.WhereElementIsNotElementType()
	.Cast<Room>()
	.Where(r => r.LookupParameter("AAI_RoomNumberingPattern")?.AsString() == "ROOM")
	.GroupBy(r => r.Level.Name);

// 4.2. Set up value of room number

Console.WriteLine("\nSTEP 4.2 TEST PRINT\n");

Console.WriteLine("Rooms grouped by level:");
foreach (var group in allOtherRooms)
{
	Console.WriteLine($"\n{group.Key}: {group.Count()} regular rooms");
	int roomIndex = 1;
	foreach (Room allOtherRooms in group)
	{
		Level roomLevel = allOtherRooms.Level;
		string roomLevelString = roomLevel.Name.ToString();
		string roomIndexString = roomIndex.ToString("D2");
		roomIndex++;
		Parameter otherRoomsNumber = allOtherRooms.LookupParameter("Number");
		otherRoomsNumber.Set($"{roomLevelString}-{roomIndexString}");
		
		Console.WriteLine($" - {roomLevelString}-{roomIndexString}");
	}
}
