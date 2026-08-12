using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

Guid parameterGuid =
    new Guid("a6adc8cd-5be6-4533-809e-a7337d492445");

int elementsChecked = 0;
int parameterFound = 0;
int populated = 0;
int cleared = 0;

BuiltInCategory[] categories =
{
    BuiltInCategory.OST_Areas,
    BuiltInCategory.OST_Doors,
    BuiltInCategory.OST_Rooms,
    BuiltInCategory.OST_SpecialityEquipment
};

using (Transaction tx =
    new Transaction(doc, "Clear AAI_LevelElevation"))
{
    tx.Start();

    foreach (BuiltInCategory category in categories)
    {
        FilteredElementCollector collector =
            new FilteredElementCollector(doc)
            .OfCategory(category)
            .WhereElementIsNotElementType();

        foreach (Element element in collector)
        {
            elementsChecked++;

            Parameter p = element.get_Parameter(parameterGuid);

            if (p == null)
                continue;

            parameterFound++;

            if (p.StorageType != StorageType.String)
                continue;

            if (string.IsNullOrWhiteSpace(p.AsString()))
                continue;

            populated++;

            if (p.IsReadOnly)
                continue;

            p.Set(string.Empty);

            if (string.IsNullOrEmpty(p.AsString()))
                cleared++;
        }
    }

    tx.Commit();
}

TaskDialog.Show(
    "AAI_LevelElevation",
    "Elements checked: " + elementsChecked +
    "\n\nParameter found: " + parameterFound +
    "\n\nPopulated: " + populated +
    "\n\nCleared: " + cleared);