#:package GBX.NET@2.*
#:package GBX.NET.LZO@2.*
#:package GBX.NET.ZLib@1.*

using GBX.NET;
using GBX.NET.Engines.GameData;
using GBX.NET.LZO;
using GBX.NET.ZLib;

Gbx.LZO = new Lzo();
Gbx.ZLib = new ZLib();

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: dotnet run CloneMinimalWrapperBlock.cs -- <donorBlockPath> <outputBlockPath> <archetypeBlockInfoId> [collectionId]");
    return;
}

var donorBlockPath = Path.GetFullPath(args[0]);
var outputBlockPath = Path.GetFullPath(args[1]);
var archetypeBlockInfoId = args[2];

if (!File.Exists(donorBlockPath))
{
    Console.Error.WriteLine($"Donor block not found: {donorBlockPath}");
    return;
}

var gbx = ParseBestEffort(donorBlockPath);
var item = gbx.Node;
if (item is null)
{
    Console.Error.WriteLine("Custom block node is null.");
    return;
}

if (item.GetEntityModelEdition() is not CGameBlockItem blockItem)
{
    Console.Error.WriteLine($"EntityModelEdition is not a CGameBlockItem. Actual type: {item.GetEntityModelEdition()?.GetType().FullName ?? "<null>"}");
    return;
}

var donorCollectionId = blockItem.ArchetypeBlockInfoCollectionId.Number
                       ?? item.Ident.Collection.Number
                       ?? 29;
var collectionId = args.Length >= 4 && int.TryParse(args[3], out var parsedCollectionId)
    ? parsedCollectionId
    : donorCollectionId;

blockItem.ArchetypeBlockInfoId = archetypeBlockInfoId;
blockItem.ArchetypeBlockInfoCollectionId = new Id(collectionId);

item.Ident = item.Ident with { Collection = new Id(collectionId) };
item.PageName = archetypeBlockInfoId;
item.Name = archetypeBlockInfoId;
item.Description = string.Empty;

Directory.CreateDirectory(Path.GetDirectoryName(outputBlockPath)!);
gbx.Save(outputBlockPath);

Console.WriteLine($$"""
{
  "donorBlockPath": "{{donorBlockPath.Replace("\\", "\\\\")}}",
  "outputBlockPath": "{{outputBlockPath.Replace("\\", "\\\\")}}",
  "archetypeBlockInfoId": "{{archetypeBlockInfoId}}",
  "collectionId": {{collectionId}},
  "note": "Minimal wrapper: keeps inline EntityModelEdition + DefaultPlacement, clears description."
}
""");

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
