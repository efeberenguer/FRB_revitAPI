namespace FRB
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
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
                t.Start("Create schedule");

                // 01. Create schedule
                ElementId catId = new ElementId(BuiltInCategory.OST_Doors);
                ViewSchedule newSchedule = ViewSchedule.CreateSchedule(doc, catId);
                newSchedule.Name = "My Door Schedule";

                t.Commit();
            }

            return Result.Succeeded;
        }

        // ▲ Your code goes here ▲
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
