List<Room> projectRooms = new FilteredElementCollector(doc)
	.OfCategory(BuiltInCategory.OST_Rooms)
	.Cast<Room>()
	.ToList();
	
Random random = new Random();

foreach (Room room in projectRooms)
{
	Parameter projectRoomsNumber = room.LookupParameter("Number");
	int randomRoomNumber = random.Next();
	string randomString = randomRoomNumber.ToString("D8");
	projectRoomsNumber.Set($"{randomString}");
}
