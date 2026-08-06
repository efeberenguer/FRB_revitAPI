using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Architecture;
using FRB.Common;

namespace FRB
{
    [Transaction(TransactionMode.Manual)]
    public class LevelElevationMapper : IExternalCommand
    {
        private const string TargetParameterName = "AAI_LevelElevation";

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // ---------------------------------------------------------
            // OPEN USER SELECTION WINDOW
            // ---------------------------------------------------------

            LevelElevationMapperWindow window =
                new LevelElevationMapperWindow();

            bool? result = window.ShowDialog();

            if (result != true)
                return Result.Cancelled;


            // ---------------------------------------------------------
            // START ONE TRANSACTION
            // ---------------------------------------------------------

            List<CategoryResult> categoryResults =
                new List<CategoryResult>();

            bool anySelected =
                window.InternalDoorsSelected ||
                window.ExternalDoorsSelected ||
                window.RoomsSelected ||
                window.GrossInternalAreasSelected ||
                window.GrossExternalAreasSelected ||
                window.NetInternalAreasSelected ||
                window.SpecialtyEquipmentSelected;

            if (!anySelected)
            {
                TaskDialog.Show(
                    "Level Elevation Mapper",
                    "No categories were selected.\n\n" +
                    "Please select at least one category and run the mapper again.");

                return Result.Cancelled;
            }


            using (Transaction transaction =
                new Transaction(doc, "Map Level Elevations"))
            {
                transaction.Start();

                try
                {
                    // -------------------------------------------------
                    // INTERNAL DOORS
                    // -------------------------------------------------

                    if (window.InternalDoorsSelected)
                    {
                        MappingResult mapping =
                            ProcessDoors(
                                doc,
                                externalDoors: false);

                        categoryResults.Add(
                            new CategoryResult(
                                "Internal Doors",
                                mapping));
                    }


                    // -------------------------------------------------
                    // EXTERNAL DOORS
                    // -------------------------------------------------

                    if (window.ExternalDoorsSelected)
                    {
                        MappingResult mapping =
                            ProcessDoors(
                                doc,
                                externalDoors: true);

                        categoryResults.Add(
                            new CategoryResult(
                                "External Doors",
                                mapping));
                    }


                    // -------------------------------------------------
                    // ROOMS
                    // -------------------------------------------------

                    if (window.RoomsSelected)
                    {
                        MappingResult mapping =
                            ProcessRooms(doc);

                        categoryResults.Add(
                            new CategoryResult(
                                "Rooms",
                                mapping));
                    }


                    // -------------------------------------------------
                    // GIA
                    // -------------------------------------------------

                    if (window.GrossInternalAreasSelected)
                    {
                        MappingResult mapping =
                            ProcessAreas(
                                doc,
                                "GIA");

                        categoryResults.Add(
                            new CategoryResult(
                                "Gross Internal Areas (GIA)",
                                mapping));
                    }


                    // -------------------------------------------------
                    // GEA
                    // -------------------------------------------------

                    if (window.GrossExternalAreasSelected)
                    {
                        MappingResult mapping =
                            ProcessAreas(
                                doc,
                                "GEA");

                        categoryResults.Add(
                            new CategoryResult(
                                "Gross External Areas (GEA)",
                                mapping));
                    }


                    // -------------------------------------------------
                    // NIA
                    // -------------------------------------------------

                    if (window.NetInternalAreasSelected)
                    {
                        MappingResult mapping =
                            ProcessAreas(
                                doc,
                                "NIA");

                        categoryResults.Add(
                            new CategoryResult(
                                "Net Internal Areas (NIA)",
                                mapping));
                    }

                    // -------------------------------------------------
                    // SPECIALTY EQUIPMENT
                    // -------------------------------------------------

                    if (window.SpecialtyEquipmentSelected)
                    {
                        MappingResult mapping =
                            ProcessSpecialtyEquipment(
                                doc,
                                "Specialty Equipment");

                        categoryResults.Add(
                            new CategoryResult(
                                "Specialty Equipment",
                                mapping));
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.RollBack();

                    TaskDialog.Show(
                        "Level Elevation Mapper - Error",
                        ex.ToString());

                    return Result.Failed;
                }
            }


            // ---------------------------------------------------------
            // FINAL SUMMARY
            // ---------------------------------------------------------

            ShowSummary(categoryResults);

            return Result.Succeeded;
        }


        // =============================================================
        // ROOMS
        // =============================================================

        private static MappingResult ProcessRooms(Document doc)
        {
            MappingResult result = new MappingResult();

            List<Room> rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .OfType<Room>()
                .ToList();

            foreach (Room room in rooms)
            {
                ProcessElement(
                    doc,
                    room,
                    room.LevelId,
                    result);
            }

            return result;
        }


        // =============================================================
        // DOORS
        // =============================================================

        private static MappingResult ProcessDoors(
            Document doc,
            bool externalDoors)
        {
            MappingResult result = new MappingResult();

            List<FamilyInstance> doors =
                new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .WhereElementIsNotElementType()
                    .OfType<FamilyInstance>()
                    .ToList();

            foreach (FamilyInstance door in doors)
            {
                string familyName =
                    door.Symbol.Family.Name;

                bool isExternal =
                    familyName.StartsWith("AAI_DOR");

                bool isInternal =
                    familyName.StartsWith("AAI_DOR_Int");

                if (externalDoors)
                {
                    if (!isExternal || isInternal)
                        continue;
                }
                else
                {
                    if (!isInternal)
                        continue;
                }

                ProcessElement(
                    doc,
                    door,
                    door.LevelId,
                    result);
            }

            return result;
        }


        // =============================================================
        // AREAS
        // =============================================================

        private static MappingResult ProcessAreas(
            Document doc,
            string schemeName)
        {
            MappingResult result = new MappingResult();

            List<AreaScheme> matchingSchemes =
                new FilteredElementCollector(doc)
                    .OfClass(typeof(AreaScheme))
                    .Cast<AreaScheme>()
                    .Where(a =>
                        string.Equals(
                            a.Name,
                            schemeName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            foreach (AreaScheme scheme in matchingSchemes)
            {
                List<Area> areas =
                    new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Areas)
                        .WhereElementIsNotElementType()
                        .OfType<Area>()
                        .Where(a =>
                            a.AreaScheme != null &&
                            a.AreaScheme.Id == scheme.Id)
                        .ToList();

                foreach (Area area in areas)
                {
                    ProcessElement(
                        doc,
                        area,
                        area.LevelId,
                        result);
                }
            }

            return result;
        }

        // =============================================================
        // SPECIALTY EQUIPMENT
        // =============================================================

        private static MappingResult ProcessSpecialtyEquipment(
            Document doc,
            string equipmentType)
        {
            MappingResult result = new MappingResult();

            List<FamilyInstance> equipment =
                new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_SpecialityEquipment)
                    .WhereElementIsNotElementType()
                    .OfType<FamilyInstance>()
                    .ToList();

            foreach (FamilyInstance instance in equipment)
            {
                string familyName =
                    instance.Symbol.Family.Name;

                bool include = false;

                if (equipmentType == "Specialty Equipment")
                {
                    include =
                        familyName.StartsWith("AAI_SPC_FireCurtain") ||
                        familyName.StartsWith("AAI_SPC_FireShutter");
                }

                if (!include)
                    continue;

                ProcessElement(
                    doc,
                    instance,
                    instance.LevelId,
                    result);
            }

            return result;
        }

        // =============================================================
        // COMMON ELEMENT PROCESSING
        // =============================================================

        private static void ProcessElement(
            Document doc,
            Element element,
            ElementId levelId,
            MappingResult result)
        {
            result.Found++;

            // ---------------------------------------------------------
            // NO LEVEL
            // ---------------------------------------------------------

            if (levelId == null ||
                levelId == ElementId.InvalidElementId)
            {
                result.NoLevel++;
                return;
            }

            Level level =
                doc.GetElement(levelId) as Level;

            if (level == null)
            {
                result.NoLevel++;
                return;
            }


            // ---------------------------------------------------------
            // LEVEL ELEVATION
            // ---------------------------------------------------------

            double elevationMm =
                Math.Round(
                    UnitUtils.ConvertFromInternalUnits(
                        level.Elevation,
                        UnitTypeId.Millimeters));

            string targetValue =
                elevationMm.ToString();


            // ---------------------------------------------------------
            // PARAMETER
            // ---------------------------------------------------------

            Parameter parameter =
                element.LookupParameter(
                    TargetParameterName);

            if (parameter == null)
            {
                result.MissingParameter++;
                return;
            }

            if (parameter.IsReadOnly)
            {
                result.ReadOnly++;
                return;
            }


            // ---------------------------------------------------------
            // CHECK EXISTING VALUE
            // ---------------------------------------------------------

            string currentValue =
                parameter.AsString();

            if (string.Equals(
                    currentValue,
                    targetValue,
                    StringComparison.Ordinal))
            {
                result.AlreadyPopulated++;
                return;
            }


            // ---------------------------------------------------------
            // WRITE VALUE
            // ---------------------------------------------------------

            try
            {
                parameter.Set(targetValue);
                result.Changed++;
            }
            catch
            {
                result.Failed++;
            }
        }


        // =============================================================
        // SUMMARY
        // =============================================================

        private static void ShowSummary(
    List<CategoryResult> categoryResults)
        {
            int totalFound = 0;
            int totalChanged = 0;
            int totalAlreadyPopulated = 0;
            int totalNoLevel = 0;
            int totalMissingParameter = 0;
            int totalReadOnly = 0;
            int totalFailed = 0;

            string summary =
                "LEVEL ELEVATION MAPPER\n\n";

            foreach (CategoryResult category in categoryResults)
            {
                MappingResult r = category.Result;

                summary +=
                    category.Name.ToUpper() + "\n" +
                    "Found: " + r.Found + "\n";

                // Only display non-zero results
                if (r.Changed > 0)
                {
                    summary +=
                        "Changed: " + r.Changed + "\n";
                }

                if (r.AlreadyPopulated > 0)
                {
                    summary +=
                        "Already Populated: " + r.AlreadyPopulated + "\n";
                }

                if (r.NoLevel > 0)
                {
                    summary +=
                        "No Level: " + r.NoLevel + "\n";
                }

                if (r.MissingParameter > 0)
                {
                    summary +=
                        "Missing Parameter: " + r.MissingParameter + "\n";
                }

                if (r.ReadOnly > 0)
                {
                    summary +=
                        "Read Only: " + r.ReadOnly + "\n";
                }

                if (r.Failed > 0)
                {
                    summary +=
                        "Failed: " + r.Failed + "\n";
                }

                summary += "\n";


                // Totals
                totalFound += r.Found;
                totalChanged += r.Changed;
                totalAlreadyPopulated += r.AlreadyPopulated;
                totalNoLevel += r.NoLevel;
                totalMissingParameter += r.MissingParameter;
                totalReadOnly += r.ReadOnly;
                totalFailed += r.Failed;
            }


            summary +=
                "--------------------------------\n" +
                "TOTAL\n" +
                "Found: " + totalFound + "\n";

            if (totalChanged > 0)
            {
                summary +=
                    "Changed: " + totalChanged + "\n";
            }

            if (totalAlreadyPopulated > 0)
            {
                summary +=
                    "Already Populated: " + totalAlreadyPopulated + "\n";
            }

            if (totalNoLevel > 0)
            {
                summary +=
                    "No Level: " + totalNoLevel + "\n";
            }

            if (totalMissingParameter > 0)
            {
                summary +=
                    "Missing Parameter: " + totalMissingParameter + "\n";
            }

            if (totalReadOnly > 0)
            {
                summary +=
                    "Read Only: " + totalReadOnly + "\n";
            }

            if (totalFailed > 0)
            {
                summary +=
                    "Failed: " + totalFailed + "\n";
            }


            TaskDialog.Show(
                "Level Elevation Mapper - Summary",
                summary);
        }


        // =============================================================
        // RESULT CLASSES
        // =============================================================

        private class MappingResult
        {
            public int Found { get; set; }
            public int Changed { get; set; }
            public int AlreadyPopulated { get; set; }
            public int NoLevel { get; set; }
            public int MissingParameter { get; set; }
            public int ReadOnly { get; set; }
            public int Failed { get; set; }
        }


        private class CategoryResult
        {
            public string Name { get; }
            public MappingResult Result { get; }

            public CategoryResult(
                string name,
                MappingResult result)
            {
                Name = name;
                Result = result;
            }
        }


        // =============================================================
        // RIBBON BUTTON
        // =============================================================

        internal static PushButtonData GetButtonData()
        {
            string buttonInternalName =
                "btnLevelElevationMapper";

            string buttonTitle =
                "Level Elevation";

            ButtonDataClass buttonData =
                new ButtonDataClass(
                    buttonInternalName,
                    buttonTitle,
                    MethodBase.GetCurrentMethod()
                        .DeclaringType?.FullName,
                    Properties.Resources.Blue_32,
                    Properties.Resources.Blue_16,
                    "Map level elevations to AAI_LevelElevation");

            return buttonData.Data;
        }
    }
}
