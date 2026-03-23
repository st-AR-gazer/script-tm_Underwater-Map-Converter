#:package GBX.NET@2.*
#:package GBX.NET.LZO@2.*
#:package GBX.NET.ZLib@1.*

using System.Text;
using GBX.NET;
using GBX.NET.Engines.GameData;
using GBX.NET.LZO;
using GBX.NET.ZLib;

Gbx.LZO = new Lzo();
Gbx.ZLib = new ZLib();

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: dotnet run GenerateMinimalWaterWrapperSet.cs -- <inventoryTxtPath> <donorRootDir> <outputRootDir>");
    return;
}

var inventoryTxtPath = Path.GetFullPath(args[0]);
var donorRootDir = Path.GetFullPath(args[1]);
var outputRootDir = Path.GetFullPath(args[2]);

if (!File.Exists(inventoryTxtPath))
{
    Console.Error.WriteLine($"Inventory file not found: {inventoryTxtPath}");
    return;
}

var environments = new HashSet<string>(StringComparer.Ordinal)
{
    "BlueBay",
    "GreenCoast",
    "RedIsland",
    "WhiteShore",
};

var entries = ParseInventory(inventoryTxtPath, environments);

if (entries.Count == 0)
{
    Console.Error.WriteLine("No block entries were parsed from the inventory file.");
    return;
}

Directory.CreateDirectory(outputRootDir);

var manifestLines = new List<string>
{
    "Environment\tGroup\tBlock\tCollection\tOutputPath"
};

var generatedCount = 0;

foreach (var entry in entries)
{
    var donorPath = Path.Combine(donorRootDir, entry.Environment, "GroundFlatVoid.Block.Gbx");
    if (!File.Exists(donorPath))
    {
        Console.Error.WriteLine($"Donor block not found for {entry.Environment}: {donorPath}");
        continue;
    }

    var gbx = ParseBestEffort(donorPath);
    var item = gbx.Node;
    if (item?.GetEntityModelEdition() is not CGameBlockItem blockItem)
    {
        Console.Error.WriteLine($"Donor wrapper is not a CGameBlockItem: {donorPath}");
        continue;
    }

    var collectionId = blockItem.ArchetypeBlockInfoCollectionId.Number
                       ?? item.Ident.Collection.Number
                       ?? throw new InvalidOperationException($"Unable to determine collection for donor {donorPath}");

    blockItem.ArchetypeBlockInfoId = entry.BlockName;
    blockItem.ArchetypeBlockInfoCollectionId = new Id(collectionId);

    item.Ident = item.Ident with { Collection = new Id(collectionId) };
    item.PageName = entry.BlockName;
    item.Name = entry.BlockName;
    item.Description = string.Empty;

    var outputDir = Path.Combine(outputRootDir, entry.Environment, entry.Group);
    Directory.CreateDirectory(outputDir);

    var outputPath = Path.Combine(outputDir, entry.BlockName + ".Block.Gbx");
    gbx.Save(outputPath);

    manifestLines.Add($"{entry.Environment}\t{entry.Group}\t{entry.BlockName}\t{collectionId}\t{outputPath}");
    generatedCount++;
}

var manifestPath = Path.Combine(outputRootDir, "manifest.tsv");
File.WriteAllLines(manifestPath, manifestLines, Encoding.UTF8);

var groupedCounts = entries
    .GroupBy(x => (x.Environment, x.Group))
    .OrderBy(x => x.Key.Environment)
    .ThenBy(x => x.Key.Group)
    .Select(x => new
    {
        x.Key.Environment,
        x.Key.Group,
        Count = x.Count(),
    })
    .ToList();

Console.WriteLine($$"""
{
  "inventoryTxtPath": "{{inventoryTxtPath.Replace("\\", "\\\\")}}",
  "donorRootDir": "{{donorRootDir.Replace("\\", "\\\\")}}",
  "outputRootDir": "{{outputRootDir.Replace("\\", "\\\\")}}",
  "generatedCount": {{generatedCount}},
  "manifestPath": "{{manifestPath.Replace("\\", "\\\\")}}"
}
""");

foreach (var groupedCount in groupedCounts)
{
    Console.WriteLine($"{groupedCount.Environment} {groupedCount.Group}: {groupedCount.Count}");
}

return;

static List<Entry> ParseInventory(string inventoryTxtPath, IReadOnlySet<string> environments)
{
    var entries = new List<Entry>();
    string? currentEnvironment = null;
    string? currentGroup = null;

    foreach (var rawLine in File.ReadLines(inventoryTxtPath))
    {
        var line = rawLine.Trim();

        if (line.Length == 0)
        {
            continue;
        }

        if (line.Equals("Current technical explanation", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        if (environments.Contains(line))
        {
            currentEnvironment = line;
            currentGroup = null;
            continue;
        }

        if (line.Equals("Working:", StringComparison.OrdinalIgnoreCase))
        {
            currentGroup = "Working";
            continue;
        }

        if (line.Equals("Not working:", StringComparison.OrdinalIgnoreCase))
        {
            currentGroup = "NotWorking";
            continue;
        }

        if (line.StartsWith("- ", StringComparison.Ordinal) &&
            currentEnvironment is not null &&
            currentGroup is not null)
        {
            var value = line[2..].Trim();
            if (value.Equals("none observed in the provided test map / user report", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entries.Add(new Entry(currentEnvironment, currentGroup, value));
        }
    }

    return entries;
}

static Gbx<CGameItemModel> ParseBestEffort(string path)
{
    var settings = new GbxReadSettings
    {
        IgnoreExceptionsInBody = true,
        SafeSkippableChunks = true,
    };

    try
    {
        return Gbx.Parse<CGameItemModel>(path, settings);
    }
    catch
    {
        return Gbx.Parse<CGameItemModel>(path, settings with { OpenPlanetHookExtractMode = true });
    }
}

sealed record Entry(string Environment, string Group, string BlockName);
