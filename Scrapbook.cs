
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
