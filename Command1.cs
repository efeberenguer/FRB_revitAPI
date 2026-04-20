namespace FRB
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand // ← Rename "Command1" as per the name of the .cs file
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Revit application and document variables
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // ▼ Your code goes here ▼

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Schedule-Palooza!");

                // Step 1 - Create a collector to get all departments of rooms

                /*FilteredElementCollector roomDepartments = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .ToList();*/

                // Step 2 - Create a list with all the different departments

                // Step 3 - Create a schedule for each department. Name should be "Dept - A", "Dept - B", etc.

                // Step 4 - Group the rooms by level

                // Step 5 - Sort the rooms in each level by name

                // Step 5 - Display the area for each level group

                // Step 6 - Calculate the total area and count

                // Benchmark: Display the room number, room name, department, comments, area, and level (hidden field)

                // Bonus: Make a schedule with with all the departments ("All departments")

                t.Commit();
            }

            return Result.Succeeded;
        }

        // ▲ Your code goes here ▲

        private Level GetLevelByName(Document doc, string levelName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Levels);
            collector.WhereElementIsNotElementType();

            foreach (Level curLevel in collector)
            {
                if (curLevel.Name == levelName)
                    return curLevel;
            }

            return null;
        }
        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Button 1";

            Common.ButtonDataClass myButtonData = new Common.ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData.Data;
        }
    }

}
