// Filled Region Explorer v0.4
// Revit 2024 + ArchSmarter Launchpad

Console.WriteLine("");
Console.WriteLine("Filled Regions");
Console.WriteLine("==============");
Console.WriteLine("");

try
{
    // ---------------------------------------------------------
    // Build View -> Sheet lookup
    // ---------------------------------------------------------

    Dictionary<ElementId, ViewSheet> viewToSheet =
        new Dictionary<ElementId, ViewSheet>();

    foreach (Viewport viewport in new FilteredElementCollector(doc)
        .OfClass(typeof(Viewport))
        .Cast<Viewport>())
    {
        ViewSheet sheet =
            doc.GetElement(viewport.SheetId) as ViewSheet;

        if (sheet != null &&
            !viewToSheet.ContainsKey(viewport.ViewId))
        {
            viewToSheet.Add(viewport.ViewId, sheet);
        }
    }


    // ---------------------------------------------------------
    // Collect Filled Regions
    // ---------------------------------------------------------

    var regions = new FilteredElementCollector(doc)
        .OfClass(typeof(FilledRegion))
        .Cast<FilledRegion>()
        .OrderBy(r => r.Id.IntegerValue)
        .ToList();


    Console.WriteLine($"Total Filled Regions: {regions.Count}");
    Console.WriteLine("");
    
    //
	int maskingCount = 0;
	
	foreach (FilledRegion region in regions)
	{
	    FilledRegionType type =
	        doc.GetElement(region.GetTypeId()) as FilledRegionType;
	
	    if (type != null && type.IsMasking)
	    {
	        maskingCount++;
	    }
	}
	
	int filledCount = regions.Count - maskingCount;
	
	Console.WriteLine($"Filled Regions : {filledCount}");
	Console.WriteLine($"Masking Regions: {maskingCount}");
	Console.WriteLine("");    
    
	// ---------------------------------------------------------
	// View Type Summary
	// ---------------------------------------------------------
	
	Console.WriteLine("By View Type");
	Console.WriteLine("------------");
	
	Dictionary<ViewType, int> viewTypeCounts =
	    new Dictionary<ViewType, int>();
	
	foreach (FilledRegion region in regions)
	{
	    View view =
	        doc.GetElement(region.OwnerViewId) as View;
	
	    if (view == null)
	        continue;
	
	    if (viewTypeCounts.ContainsKey(view.ViewType))
	    {
	        viewTypeCounts[view.ViewType]++;
	    }
	    else
	    {
	        viewTypeCounts.Add(view.ViewType, 1);
	    }
	}
	
	
	foreach (KeyValuePair<ViewType, int> item in viewTypeCounts
	    .OrderBy(x => x.Key.ToString()))
	{
	    Console.WriteLine(
	        $"{item.Key,-20}: {item.Value}");
	}
	
	Console.WriteLine("");
	
	// ---------------------------------------------------------
	// View Summary
	// ---------------------------------------------------------
	
	Console.WriteLine("By View");
	Console.WriteLine("-------");
	
	Dictionary<string, int> viewCounts =
	    new Dictionary<string, int>();
	
	foreach (FilledRegion region in regions)
	{
	    View view =
	        doc.GetElement(region.OwnerViewId) as View;
	
	    if (view == null)
	        continue;
	
	    string viewName = view.Name;
	
	    if (viewCounts.ContainsKey(viewName))
	    {
	        viewCounts[viewName]++;
	    }
	    else
	    {
	        viewCounts.Add(viewName, 1);
	    }
	}
	
	
	foreach (KeyValuePair<string, int> item in viewCounts
	    .OrderByDescending(x => x.Value))
	{
	    Console.WriteLine(
	        $"{item.Key,-50}: {item.Value}");
	}
	
	Console.WriteLine("");

	// ---------------------------------------------------------
	// Views with Filled Regions Not On Sheets
	// ---------------------------------------------------------
	
	Console.WriteLine("Views With Filled Regions Not On Sheets");
	Console.WriteLine("--------------------------------------");
	
	Dictionary<string, int> unsheetedViews =
	    new Dictionary<string, int>();
	
	foreach (FilledRegion region in regions)
	{
	    View view =
	        doc.GetElement(region.OwnerViewId) as View;
	
	    if (view == null)
	        continue;
	
	    bool onSheet =
	        viewToSheet.ContainsKey(view.Id);
	
	    if (!onSheet)
	    {
	        if (unsheetedViews.ContainsKey(view.Name))
	        {
	            unsheetedViews[view.Name]++;
	        }
	        else
	        {
	            unsheetedViews.Add(view.Name, 1);
	        }
	    }
	}
	
	
	if (unsheetedViews.Count == 0)
	{
	    Console.WriteLine("None");
	}
	else
	{
	    foreach (KeyValuePair<string, int> item in unsheetedViews
	        .OrderByDescending(x => x.Value))
	    {
	        Console.WriteLine(
	            $"{item.Key,-50}: {item.Value}");
	    }
	}
	
	Console.WriteLine("");
	
	// ---------------------------------------------------------
	// Filled Regions by Sheet
	// ---------------------------------------------------------
	
	Console.WriteLine("Filled Regions by Sheet");
	Console.WriteLine("-----------------------");
	
	Dictionary<string, int> sheetCounts =
	    new Dictionary<string, int>();
	
	foreach (FilledRegion region in regions)
	{
	    View view =
	        doc.GetElement(region.OwnerViewId) as View;
	
	    if (view == null)
	        continue;
	
	    if (!viewToSheet.TryGetValue(view.Id, out ViewSheet sheet))
	        continue;
	
	    string sheetKey =
	        $"{sheet.SheetNumber} - {sheet.Name}";
	
	    if (sheetCounts.ContainsKey(sheetKey))
	    {
	        sheetCounts[sheetKey]++;
	    }
	    else
	    {
	        sheetCounts.Add(sheetKey, 1);
	    }
	}
	
	if (sheetCounts.Count == 0)
	{
	    Console.WriteLine("None");
	}
	else
	{
	    foreach (KeyValuePair<string, int> item in sheetCounts
	        .OrderByDescending(x => x.Value))
	    {
	        Console.WriteLine($"{item.Key,-60}: {item.Value}");
	    }
	}
	
	Console.WriteLine("");

    //
    
    foreach (FilledRegion region in regions)
    {
        try
        {
            View view =
                doc.GetElement(region.OwnerViewId) as View;


            FilledRegionType regionType =
                doc.GetElement(region.GetTypeId()) as FilledRegionType;


            string regionClass = "Unknown";
            string regionName = "-";


            if (regionType != null)
            {
                regionClass = regionType.IsMasking
                    ? "Masking Region"
                    : "Filled Region";

                regionName = regionType.Name;
            }


            // -------------------------------------------------
            // Group information
            // -------------------------------------------------

            bool isGrouped =
                region.GroupId != ElementId.InvalidElementId;

            string groupName = "-";


            if (isGrouped)
            {
                Autodesk.Revit.DB.Group group =
                    doc.GetElement(region.GroupId)
                    as Autodesk.Revit.DB.Group;


                if (group != null)
                {
                    GroupType groupType =
                        doc.GetElement(group.GetTypeId())
                        as GroupType;


                    if (groupType != null)
                    {
                        groupName = groupType.Name;
                    }
                }
            }


            // -------------------------------------------------
            // Workset information
            // -------------------------------------------------

            string worksetName = "-";

            Workset workset =
                doc.GetWorksetTable()
                .GetWorkset(region.WorksetId);

            if (workset != null)
            {
                worksetName = workset.Name;
            }


            // -------------------------------------------------
            // Sheet information
            // -------------------------------------------------

            bool onSheet = false;

            string sheetNumber = "-";
            string sheetName = "-";


            if (view != null &&
                viewToSheet.TryGetValue(view.Id, out ViewSheet sheet))
            {
                onSheet = true;
                sheetNumber = sheet.SheetNumber;
                sheetName = sheet.Name;
            }


            // -------------------------------------------------
            // Output
            // -------------------------------------------------

            Console.WriteLine($"Element Id   : {region.Id.IntegerValue}");
            Console.WriteLine($"Class        : {regionClass}");
            Console.WriteLine($"Type         : {regionName}");
            Console.WriteLine($"View         : {view?.Name}");
            Console.WriteLine($"View Type    : {view?.ViewType}");
            Console.WriteLine($"Grouped      : {(isGrouped ? "Yes" : "No")}");
            Console.WriteLine($"Group Name   : {groupName}");
            Console.WriteLine($"Workset      : {worksetName}");
            Console.WriteLine($"On Sheet     : {(onSheet ? "Yes" : "No")}");
            Console.WriteLine($"Sheet Number : {sheetNumber}");
            Console.WriteLine($"Sheet Name   : {sheetName}");
            Console.WriteLine("----------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error processing Filled Region {region.Id.IntegerValue}");

            Console.WriteLine(ex.ToString());
            Console.WriteLine("----------------------------------------");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Failed collecting Filled Regions");
    Console.WriteLine(ex.ToString());
}