#:package GBX.NET@2.*
#:package GBX.NET.LZO@2.*
#:package GBX.NET.ZLib@1.*

using System.Reflection;
using System.Text.RegularExpressions;
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.LZO;
using GBX.NET.ZLib;

Gbx.LZO = new Lzo();
Gbx.ZLib = new ZLib();

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run MapFloodVistaMap.cs -- <inputMapPath> [outputMapPath] [floodY] [--preserve-terrain-y] [--set-deco-offset]");
    return;
}

var optionArgs = args.Where(static arg => arg.StartsWith("--", StringComparison.Ordinal)).ToHashSet(StringComparer.OrdinalIgnoreCase);
var positionalArgs = args.Where(static arg => !arg.StartsWith("--", StringComparison.Ordinal)).ToArray();

var preserveTerrainY = optionArgs.Contains("--preserve-terrain-y");
var setDecoOffset = optionArgs.Contains("--set-deco-offset");

var inputMapPath = Path.GetFullPath(positionalArgs[0]);
if (!File.Exists(inputMapPath))
{
    Console.Error.WriteLine($"Input map not found: {inputMapPath}");
    return;
}

var outputMapPath = positionalArgs.Length >= 2
    ? Path.GetFullPath(positionalArgs[1])
    : BuildDefaultOutputPath(inputMapPath);

int? requestedFloodY = null;
if (positionalArgs.Length >= 3)
{
    if (!int.TryParse(positionalArgs[2], out var parsedFloodY))
    {
        Console.Error.WriteLine($"Invalid floodY '{positionalArgs[2]}'.");
        return;
    }

    requestedFloodY = parsedFloodY;
}

var gbx = ParseBestEffort(inputMapPath);
var map = gbx.Node;
if (map is null)
{
    Console.Error.WriteLine("Map node is null.");
    return;
}

var environment = TryExtractEnvironmentFromHeader(inputMapPath)
                  ?? map.Collection?.ToString()
                  ?? string.Empty;
var targetWaterZoneId = ResolveTargetWaterZoneId(environment);
if (targetWaterZoneId is null)
{
    Console.Error.WriteLine($"Unsupported or unknown environment '{environment}'.");
    return;
}

var genealogies = map.ZoneGenealogy;
if (genealogies is null || genealogies.Count == 0)
{
    Console.Error.WriteLine("Map has no ZoneGenealogy entries.");
    return;
}

var blocks = map.Blocks;
if (blocks is null || blocks.Count == 0)
{
    Console.Error.WriteLine("Map has no blocks.");
    return;
}

var waterGenealogyPrototype = genealogies
    .FirstOrDefault(g => string.Equals(ReadStringProperty(g, "CurrentZoneId"), targetWaterZoneId, StringComparison.OrdinalIgnoreCase));
if (waterGenealogyPrototype is null)
{
    Console.Error.WriteLine($"Could not find a '{targetWaterZoneId}' genealogy prototype in the map.");
    return;
}

var waterBlockPrototype = blocks
    .FirstOrDefault(block =>
        block.IsGround &&
        string.Equals(NormalizeTypeIdValue(block.BlockModel.Id), targetWaterZoneId, StringComparison.OrdinalIgnoreCase));
if (waterBlockPrototype is null)
{
    Console.Error.WriteLine($"Could not find a '{targetWaterZoneId}' terrain block prototype in the map.");
    return;
}

var terrainTypeIds = CollectTerrainTypeIds(genealogies, environment);
var defaultFloodY = ComputeDefaultFloodY(blocks, terrainTypeIds, targetWaterZoneId, waterBlockPrototype.Coord.Y);
var floodY = requestedFloodY ?? defaultFloodY;
var originalWaterY = waterBlockPrototype.Coord.Y;
var originalDecoBaseHeightOffset = ReadInt32Property(map, "DecoBaseHeightOffset");
var targetDecoBaseHeightOffset = floodY - originalWaterY;

var prototypeZoneIds = ReadStringArrayProperty(waterGenealogyPrototype, "ZoneIds");
var prototypeCurrentIndex = ReadInt32Property(waterGenealogyPrototype, "CurrentIndex");
var prototypeDir = ReadEnumProperty<Direction>(waterGenealogyPrototype, "Dir");
var prototypeCurrentZoneId = ReadStringProperty(waterGenealogyPrototype, "CurrentZoneId") ?? targetWaterZoneId;

var rewrittenGenealogyCount = 0;
foreach (var genealogy in genealogies)
{
    SetProperty(genealogy, "ZoneIds", prototypeZoneIds.ToArray());
    SetProperty(genealogy, "CurrentIndex", prototypeCurrentIndex);
    SetProperty(genealogy, "Dir", prototypeDir);
    SetProperty(genealogy, "CurrentZoneId", prototypeCurrentZoneId);
    rewrittenGenealogyCount += 1;
}

var rewrittenTerrainBlockCount = 0;
foreach (var block in blocks)
{
    if (!block.IsGround)
    {
        continue;
    }

    var typeId = NormalizeTypeIdValue(block.BlockModel.Id);
    if (!terrainTypeIds.Contains(typeId))
    {
        continue;
    }

    block.BlockModel = new Ident(targetWaterZoneId);
    var rewrittenY = preserveTerrainY ? block.Coord.Y : floodY;
    block.Coord = new Int3(block.Coord.X, rewrittenY, block.Coord.Z);
    block.Direction = waterBlockPrototype.Direction;
    block.Flags = waterBlockPrototype.Flags;
    block.Variant = waterBlockPrototype.Variant;
    block.SubVariant = waterBlockPrototype.SubVariant;
    block.Color = waterBlockPrototype.Color;
    block.WaypointSpecialProperty = null;
    block.IsGhost = false;
    block.IsFree = false;
    rewrittenTerrainBlockCount += 1;
}

if (setDecoOffset)
{
    SetProperty(map, "DecoBaseHeightOffset", targetDecoBaseHeightOffset);
}

if (!map.MapName.Contains("[All Water Prototype]", StringComparison.Ordinal))
{
    map.MapName = $"{map.MapName} [All Water Prototype]";
}

Directory.CreateDirectory(Path.GetDirectoryName(outputMapPath)!);
gbx.Save(outputMapPath);

Console.WriteLine($$"""
{
  "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
  "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
  "environment": "{{environment}}",
  "targetWaterZoneId": "{{targetWaterZoneId}}",
  "floodY": {{floodY}},
  "preserveTerrainY": {{preserveTerrainY.ToString().ToLowerInvariant()}},
  "setDecoOffset": {{setDecoOffset.ToString().ToLowerInvariant()}},
  "originalWaterY": {{originalWaterY}},
  "originalDecoBaseHeightOffset": {{originalDecoBaseHeightOffset}},
  "targetDecoBaseHeightOffset": {{targetDecoBaseHeightOffset}},
  "rewrittenGenealogyCount": {{rewrittenGenealogyCount}},
  "rewrittenTerrainBlockCount": {{rewrittenTerrainBlockCount}},
  "terrainTypeIds": [{{string.Join(", ", terrainTypeIds.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase).Select(static x => $"\"{x}\""))}}]
}
""");

static Gbx<CGameCtnChallenge> ParseBestEffort(string path)
{
    var settings = new GbxReadSettings
    {
        IgnoreExceptionsInBody = true,
        SafeSkippableChunks = true,
    };

    try
    {
        return Gbx.Parse<CGameCtnChallenge>(path, settings);
    }
    catch
    {
        return Gbx.Parse<CGameCtnChallenge>(path, settings with { OpenPlanetHookExtractMode = true });
    }
}

static string BuildDefaultOutputPath(string inputMapPath)
{
    var directory = Path.GetDirectoryName(inputMapPath) ?? Environment.CurrentDirectory;
    var fileName = Path.GetFileName(inputMapPath);
    var stem = fileName.EndsWith(".Map.Gbx", StringComparison.OrdinalIgnoreCase)
        ? fileName[..^8]
        : Path.GetFileNameWithoutExtension(fileName);
    return Path.Combine(directory, $"{stem} - All Water Prototype.Map.Gbx");
}

static string? TryExtractEnvironmentFromHeader(string mapPath)
{
    try
    {
        var bytes = File.ReadAllBytes(mapPath);
        var text = System.Text.Encoding.Latin1.GetString(bytes);

        var quotedMatch = Regex.Match(text, "envir\\s*=\\s*\\\"(?<env>[^\\\"]+)\\\"", RegexOptions.IgnoreCase);
        if (quotedMatch.Success)
        {
            return quotedMatch.Groups["env"].Value.Trim();
        }

        var bareMatch = Regex.Match(text, "envir\\s*=\\s*(?<env>[A-Za-z0-9_]+)", RegexOptions.IgnoreCase);
        return bareMatch.Success ? bareMatch.Groups["env"].Value.Trim() : null;
    }
    catch
    {
        return null;
    }
}

static string? ResolveTargetWaterZoneId(string environment) => environment.Trim() switch
{
    "WhiteShore" => "Water",
    "RedIsland" => "Water",
    "BlueBay" => "Sea",
    "GreenCoast" => "Lake",
    _ => null,
};

static HashSet<string> CollectTerrainTypeIds(IEnumerable<object> genealogies, string environment)
{
    var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var genealogy in genealogies)
    {
        var currentZoneId = ReadStringProperty(genealogy, "CurrentZoneId");
        if (!string.IsNullOrWhiteSpace(currentZoneId) && !currentZoneId.StartsWith("VoidTo", StringComparison.OrdinalIgnoreCase))
        {
            result.Add(currentZoneId);
        }

        foreach (var zoneId in ReadStringArrayProperty(genealogy, "ZoneIds"))
        {
            if (!string.IsNullOrWhiteSpace(zoneId) && !zoneId.StartsWith("VoidTo", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(zoneId);
            }
        }
    }

    foreach (var extra in GetEnvironmentTerrainExtras(environment))
    {
        result.Add(extra);
    }

    return result;
}

static IEnumerable<string> GetEnvironmentTerrainExtras(string environment)
{
    return environment.Trim() switch
    {
        "WhiteShore" => new[] { "DecoTerrainRocky" },
        _ => Array.Empty<string>(),
    };
}

static int ComputeDefaultFloodY(
    IEnumerable<CGameCtnBlock> blocks,
    HashSet<string> terrainTypeIds,
    string targetWaterZoneId,
    int fallbackWaterY)
{
    var maxPlayableY = int.MinValue;
    var maxExistingWaterY = int.MinValue;

    foreach (var block in blocks)
    {
        if (!block.IsGround)
        {
            continue;
        }

        var typeId = NormalizeTypeIdValue(block.BlockModel.Id);

        if (string.Equals(typeId, targetWaterZoneId, StringComparison.OrdinalIgnoreCase))
        {
            maxExistingWaterY = Math.Max(maxExistingWaterY, block.Coord.Y);
        }

        if (terrainTypeIds.Contains(typeId))
        {
            continue;
        }

        if (IsFloodReferenceBlock(typeId, block))
        {
            maxPlayableY = Math.Max(maxPlayableY, block.Coord.Y);
        }
    }

    if (maxPlayableY != int.MinValue)
    {
        return maxPlayableY;
    }

    if (maxExistingWaterY != int.MinValue)
    {
        return maxExistingWaterY;
    }

    return fallbackWaterY;
}

static bool IsFloodReferenceBlock(string typeId, CGameCtnBlock block)
{
    if (block.WaypointSpecialProperty is not null)
    {
        return true;
    }

    return typeId.StartsWith("Road", StringComparison.OrdinalIgnoreCase)
           || typeId.StartsWith("Platform", StringComparison.OrdinalIgnoreCase)
           || typeId.StartsWith("TrackWall", StringComparison.OrdinalIgnoreCase);
}

static string NormalizeTypeIdValue(string rawId)
{
    if (string.IsNullOrWhiteSpace(rawId))
    {
        return "Unknown";
    }

    return rawId
        .Replace(".Block.Gbx_CustomBlock", string.Empty, StringComparison.OrdinalIgnoreCase)
        .Replace(".Block.Gbx", string.Empty, StringComparison.OrdinalIgnoreCase)
        .Replace(".Item.Gbx", string.Empty, StringComparison.OrdinalIgnoreCase)
        .Replace('/', '\\')
        .Trim();
}

static string? ReadStringProperty(object instance, string propertyName)
{
    var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
    if (property is null || !property.CanRead)
    {
        return null;
    }

    return property.GetValue(instance) as string;
}

static int ReadInt32Property(object instance, string propertyName)
{
    var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
    if (property is null || !property.CanRead)
    {
        return 0;
    }

    var value = property.GetValue(instance);
    return value switch
    {
        int direct => direct,
        IConvertible convertible => convertible.ToInt32(System.Globalization.CultureInfo.InvariantCulture),
        _ => 0,
    };
}

static TEnum ReadEnumProperty<TEnum>(object instance, string propertyName) where TEnum : struct
{
    var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
    if (property is null || !property.CanRead)
    {
        return default;
    }

    var value = property.GetValue(instance);
    return value is TEnum typed ? typed : default;
}

static IReadOnlyList<string> ReadStringArrayProperty(object instance, string propertyName)
{
    var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
    if (property is null || !property.CanRead)
    {
        return Array.Empty<string>();
    }

    var value = property.GetValue(instance);
    if (value is string[] strings)
    {
        return strings;
    }

    if (value is IEnumerable<string> enumerable)
    {
        return enumerable.ToArray();
    }

    return Array.Empty<string>();
}

static void SetProperty(object instance, string propertyName, object? value)
{
    var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
    if (property is null || !property.CanWrite)
    {
        throw new InvalidOperationException($"Property '{propertyName}' is not writable on {instance.GetType().FullName}.");
    }

    property.SetValue(instance, value);
}
