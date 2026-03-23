#:package GBX.NET@2.*
#:package GBX.NET.LZO@2.*
#:package GBX.NET.ZLib@1.*

#pragma warning disable GBXNET10001

using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.LZO;
using GBX.NET.ZLib;

Gbx.LZO = new Lzo();
Gbx.ZLib = new ZLib();

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run PlaceWaterCarrierLattice.cs -- <inputMapPath> [outputMapPath] [prototypeFilter]");
    return;
}

var inputMapPath = Path.GetFullPath(args[0]);
if (!File.Exists(inputMapPath))
{
    Console.Error.WriteLine($"Input map not found: {inputMapPath}");
    return;
}

var outputMapPath = args.Length >= 2
    ? Path.GetFullPath(args[1])
    : BuildDefaultOutputPath(inputMapPath);

var prototypeFilter = args.Length >= 3
    ? args[2]
    : "RoadIceWithWallLeftDiagLeftStraightOn";

var gbx = ParseBestEffort(inputMapPath);
var map = gbx.Node;
if (map is null)
{
    Console.Error.WriteLine("Map node is null.");
    return;
}

if (map.Blocks is null)
{
    Console.Error.WriteLine("Map has no Blocks list.");
    return;
}

var prototype = map.Blocks
    .Where(block => block.Name.Contains(prototypeFilter, StringComparison.OrdinalIgnoreCase))
    .OrderByDescending(block => block.Name.Contains(".Block.Gbx_CustomBlock", StringComparison.OrdinalIgnoreCase))
    .ThenByDescending(block => block.IsGhost)
    .FirstOrDefault();

if (prototype is null)
{
    Console.Error.WriteLine($"Could not find a prototype block matching '{prototypeFilter}' in the map.");
    return;
}

var size = map.Size;
var maxX = size.X - 1;
var maxZ = size.Z - 1;

if (maxX < 0 || maxZ < 2)
{
    Console.Error.WriteLine($"Map size is too small for the lattice: {size}.");
    return;
}

var lineStep = new Int3(-1, 0, -2);
var startRows = new[] { maxZ - 1, maxZ - 2 }.Distinct().Where(z => z >= 0).OrderByDescending(z => z).ToArray();

var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var block in map.Blocks)
{
    existing.Add(BuildPlacementKey(block.Name, block.Coord, block.Direction));
}

var added = 0;
var skippedExisting = 0;
var lineCount = 0;

for (var baseX = 0; baseX <= maxX; baseX += 3)
{
    foreach (var startZ in startRows)
    {
        lineCount++;

        var current = new Int3(baseX, prototype.Coord.Y, startZ);
        while (current.X >= 0 && current.X <= maxX && current.Z >= 0 && current.Z <= maxZ)
        {
            var key = BuildPlacementKey(prototype.Name, current, prototype.Direction);
            if (existing.Add(key))
            {
                var clone = (CGameCtnBlock)prototype.DeepClone();
                clone.Coord = current;
                clone.AbsolutePositionInMap = null;
                clone.YawPitchRoll = null;
                clone.MacroblockReference = null;
                map.Blocks.Add(clone);
                added++;
            }
            else
            {
                skippedExisting++;
            }

            current = new Int3(current.X + lineStep.X, current.Y, current.Z + lineStep.Z);
        }
    }
}

map.HasGhostBlocks = map.HasGhostBlocks || prototype.IsGhost;
if (!map.MapName.Contains("[Water Carrier Lattice]", StringComparison.Ordinal))
{
    map.MapName = $"{map.MapName} [Water Carrier Lattice]";
}

Directory.CreateDirectory(Path.GetDirectoryName(outputMapPath)!);
gbx.Save(outputMapPath);

Console.WriteLine($$"""
{
  "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
  "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
  "prototypeFilter": "{{prototypeFilter}}",
  "prototypeName": "{{prototype.Name.Replace("\\", "\\\\")}}",
  "prototypeCoord": "<{{prototype.Coord.X}}, {{prototype.Coord.Y}}, {{prototype.Coord.Z}}>",
  "mapSize": "<{{size.X}}, {{size.Y}}, {{size.Z}}>",
  "lineStep": "<{{lineStep.X}}, {{lineStep.Y}}, {{lineStep.Z}}>",
  "startRows": [{{string.Join(", ", startRows)}}],
  "lineCount": {{lineCount}},
  "addedBlockCount": {{added}},
  "skippedExistingCount": {{skippedExisting}},
  "totalBlockCount": {{map.Blocks.Count}}
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
    return Path.Combine(directory, $"{stem} - Water Carrier Lattice.Map.Gbx");
}

static string BuildPlacementKey(string name, Int3 coord, Direction direction)
    => $"{name}|{coord.X}|{coord.Y}|{coord.Z}|{direction}";
