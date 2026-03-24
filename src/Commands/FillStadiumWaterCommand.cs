#pragma warning disable GBXNET10001

using System.Globalization;
using GBX.NET;
using GBX.NET.Engines.Game;
using UnderwaterMapConverter.Infrastructure;

namespace UnderwaterMapConverter.Commands;

internal static class FillStadiumWaterCommand
{
    private const string WaterBlockName = "DecoWallWaterBase";

    public static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            return CliHelp.Write("Missing input map path for fill-stadium-water.");
        }

        var optionArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flagArgs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var positionalArgs = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                positionalArgs.Add(arg);
                continue;
            }

            if (arg is "--write")
            {
                flagArgs.Add(arg);
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new InvalidOperationException($"Missing value after {arg}.");
            }

            optionArgs[arg] = args[index + 1];
            index++;
        }

        var inputMapPath = Path.GetFullPath(positionalArgs[0]);
        if (!File.Exists(inputMapPath))
        {
            throw new FileNotFoundException($"Input map not found: {inputMapPath}");
        }

        var outputMapPath = positionalArgs.Count >= 2
            ? Path.GetFullPath(positionalArgs[1])
            : MapPathHelpers.BuildDefaultOutputPath(inputMapPath, "Water Volume");

        var limit = ReadIntOption(optionArgs, "--limit", 0);
        var mapNameSuffix = optionArgs.TryGetValue("--map-name-suffix", out var mapNameSuffixRaw)
            ? mapNameSuffixRaw
            : null;
        var write = flagArgs.Contains("--write");

        var gbx = GbxIo.ParseChallengeBestEffort(inputMapPath);
        var map = gbx.Node ?? throw new InvalidOperationException("Map node is null.");
        if (map.Blocks is null || map.Blocks.Count == 0)
        {
            throw new InvalidOperationException("Map has no blocks.");
        }

        var size = map.Size;
        var maxWorldY = (size.Y - 1) * 8f;
        var minLayerWorldY = ReadFloatOption(optionArgs, "--min-world-y", 0f);
        var maxLayerWorldY = ReadFloatOption(optionArgs, "--max-world-y", maxWorldY);

        if (maxLayerWorldY < minLayerWorldY)
        {
            throw new InvalidOperationException("--max-world-y must be >= --min-world-y.");
        }

        const float blockHeight = 8f;
        var layerWorldYs = new List<float>();
        for (var y = minLayerWorldY; y <= maxLayerWorldY + 0.001f; y += blockHeight)
        {
            layerWorldYs.Add(y);
        }

        var projectedCount = (long)size.X * size.Z * layerWorldYs.Count;

        if (!write)
        {
            Console.WriteLine($$"""
            {
              "mode": "dry-run",
              "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
              "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
              "blockName": "{{WaterBlockName}}",
              "gridSize": [{{size.X}}, {{size.Y}}, {{size.Z}}],
              "minWorldY": {{minLayerWorldY.ToString(CultureInfo.InvariantCulture)}},
              "maxWorldY": {{maxLayerWorldY.ToString(CultureInfo.InvariantCulture)}},
              "layerCount": {{layerWorldYs.Count}},
              "projectedAddedBlockCount": {{projectedCount}},
              "note": "Pass --write to actually emit the output map."
            }
            """);

            return 0;
        }

        var prototype = map.Blocks.FirstOrDefault(block =>
                           string.Equals(block.Name, WaterBlockName, StringComparison.OrdinalIgnoreCase))
                       ?? map.Blocks[0];

        var existingPlacements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var block in map.Blocks)
        {
            existingPlacements.Add(BuildPlacementKey(block.Name, GetWorldPosition(block)));
        }

        var added = 0;
        var skippedExisting = 0;
        var addedAnchor = false;
        Int3? anchorCoord = null;

        for (var coordZ = 0; coordZ < size.Z; coordZ++)
        {
            for (var coordX = 0; coordX < size.X; coordX++)
            {
                foreach (var worldY in layerWorldYs)
                {
                    var world = new Vec3(coordX * 32f, worldY, coordZ * 32f);
                    var key = BuildPlacementKey(WaterBlockName, world);
                    if (!existingPlacements.Add(key))
                    {
                        skippedExisting++;
                        continue;
                    }

                    var coordY = (int)MathF.Floor(worldY / 8f);
                    map.Blocks.Add(CreatePlacedWaterBlock(prototype, new Int3(coordX, coordY, coordZ), isGhost: true));
                    added++;

                    if (limit > 0 && added >= limit)
                    {
                        goto done;
                    }
                }
            }
        }

        done:

        var minCoordY = (int)MathF.Floor(minLayerWorldY / 8f);
        var maxCoordY = (int)MathF.Floor(maxLayerWorldY / 8f);

        if (limit <= 0 && TryAddStandaloneAnchorBlock(map, prototype, size, minCoordY, maxCoordY, existingPlacements, out anchorCoord))
        {
            addedAnchor = true;
            added++;
        }

        var bakedAdded = GenerateBakedBlocks(
            map,
            prototype,
            0,
            size.X - 1,
            0,
            size.Z - 1,
            minCoordY,
            maxCoordY,
            anchorCoord);

        map.HasGhostBlocks = true;
        if (!string.IsNullOrWhiteSpace(mapNameSuffix))
        {
            if (!map.MapName.EndsWith(mapNameSuffix, StringComparison.OrdinalIgnoreCase))
            {
                map.MapName = $"{map.MapName} {mapNameSuffix}".Trim();
            }
        }
        else if (!map.MapName.Contains("[Water Volume]", StringComparison.Ordinal))
        {
            map.MapName = $"{map.MapName} [Water Volume]";
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputMapPath)!);
        gbx.Save(outputMapPath);

        Console.WriteLine($$"""
        {
          "mode": "write",
          "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
          "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
          "blockName": "{{WaterBlockName}}",
          "gridSize": [{{size.X}}, {{size.Y}}, {{size.Z}}],
          "minWorldY": {{minLayerWorldY.ToString(CultureInfo.InvariantCulture)}},
          "maxWorldY": {{maxLayerWorldY.ToString(CultureInfo.InvariantCulture)}},
          "layerCount": {{layerWorldYs.Count}},
          "addedBlockCount": {{added}},
          "skippedExistingCount": {{skippedExisting}},
          "anchorAdded": {{addedAnchor.ToString().ToLowerInvariant()}},
          "anchorCoord": {{FormatOptionalCoord(anchorCoord)}},
          "bakedBlockCount": {{bakedAdded}},
          "totalBlockCount": {{map.Blocks.Count}}
        }
        """);

        return 0;
    }

    private static CGameCtnBlock CreatePlacedWaterBlock(CGameCtnBlock prototype, Int3 coord, bool isGhost)
    {
        var clone = (CGameCtnBlock)prototype.DeepClone();
        clone.Name = WaterBlockName;
        clone.BlockModel = clone.BlockModel with { Id = WaterBlockName };
        clone.IsGhost = isGhost;
        clone.IsFree = false;
        clone.IsGround = false;
        clone.AbsolutePositionInMap = null;
        clone.Direction = Direction.North;
        clone.YawPitchRoll = default;
        clone.Variant = 0;
        clone.SubVariant = 0;
        clone.Coord = coord;
        return clone;
    }

    private static int GenerateBakedBlocks(
        CGameCtnChallenge map,
        CGameCtnBlock prototype,
        int xMin,
        int xMax,
        int zMin,
        int zMax,
        int yMin,
        int yMax,
        Int3? anchorCoord)
    {
        map.BakedBlocks ??= [];
        var count = 0;

        for (var z = zMin; z <= zMax; z++)
        {
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var coord = new Int3(x, y, z);
                    AddBakedBlock(map, prototype, "DecoWallWaterBaseFCB", coord with { Y = y - 1 }, Direction.North, variant: 0, isGround: false, isGhost: true);
                    AddBakedBlock(map, prototype, "DecoWallWaterFCBInside", coord with { Y = y - 1 }, Direction.North, variant: 0, isGround: false, isGhost: true);
                    AddBakedBlock(map, prototype, "DecoWallWaterFCT", coord with { Y = y + 1 }, Direction.North, variant: 0, isGround: false, isGhost: true);
                    count += 3;
                }
            }
        }

        var edges = new[]
        {
            (dir: Direction.North, isXAxis: true, fixedCoord: zMin - 1, rangeMin: xMin, rangeMax: xMax, highCornerPos: xMax, lowCornerPos: xMin),
            (dir: Direction.East, isXAxis: false, fixedCoord: xMax + 1, rangeMin: zMin, rangeMax: zMax, highCornerPos: zMax, lowCornerPos: zMin),
            (dir: Direction.South, isXAxis: true, fixedCoord: zMax + 1, rangeMin: xMin, rangeMax: xMax, highCornerPos: xMin, lowCornerPos: xMax),
            (dir: Direction.West, isXAxis: false, fixedCoord: xMin - 1, rangeMin: zMin, rangeMax: zMax, highCornerPos: zMin, lowCornerPos: zMax),
        };

        foreach (var edge in edges)
        {
            for (var pos = edge.rangeMin; pos <= edge.rangeMax; pos++)
            {
                var coord2d = edge.isXAxis
                    ? new Int3(pos, 0, edge.fixedCoord)
                    : new Int3(edge.fixedCoord, 0, pos);

                for (var y = yMin; y <= yMax; y++)
                {
                    var coord = coord2d with { Y = y };
                    AddBakedBlock(map, prototype, "DecoWallWaterVFC", coord, edge.dir, variant: 3, isGround: false, isGhost: true);

                    var hfcVariant = pos == edge.highCornerPos ? 7
                        : pos == edge.lowCornerPos ? 13
                        : 15;

                    AddBakedBlock(map, prototype, "DecoWallWaterHFCInsideShort", coord, edge.dir, variant: hfcVariant, isGround: false, isGhost: true);
                    count += 2;
                }
            }
        }

        if (anchorCoord is Int3 anchor)
        {
            AddBakedBlock(map, prototype, "DecoWallWaterBaseFCB", new Int3(anchor.X, anchor.Y - 1, anchor.Z), Direction.North, variant: 0, isGround: false, isGhost: false);
            AddBakedBlock(map, prototype, "DecoWallWaterFCBInside", new Int3(anchor.X, anchor.Y - 1, anchor.Z), Direction.North, variant: 0, isGround: false, isGhost: false);
            AddBakedBlock(map, prototype, "DecoWallWaterFCT", new Int3(anchor.X, anchor.Y + 1, anchor.Z), Direction.North, variant: 0, isGround: false, isGhost: false);

            foreach (var (coord, direction) in BuildAnchorEdgeCoords(anchor))
            {
                AddBakedBlock(map, prototype, "DecoWallWaterVFC", coord, direction, variant: 3, isGround: false, isGhost: false);
                AddBakedBlock(map, prototype, "DecoWallWaterHFCInsideShort", coord, direction, variant: 5, isGround: false, isGhost: false);
            }

            count += 11;
        }

        return count;
    }

    private static void AddBakedBlock(
        CGameCtnChallenge map,
        CGameCtnBlock prototype,
        string name,
        Int3 coord,
        Direction direction,
        int variant,
        bool isGround,
        bool isGhost)
    {
        var clone = (CGameCtnBlock)prototype.DeepClone();
        clone.Name = name;
        clone.BlockModel = clone.BlockModel with { Id = name };
        clone.Coord = coord;
        clone.Direction = direction;
        clone.Variant = (byte)variant;
        clone.SubVariant = 0;
        clone.IsGhost = isGhost;
        clone.IsFree = false;
        clone.IsGround = isGround;
        clone.AbsolutePositionInMap = null;
        clone.YawPitchRoll = default;
        clone.Color = default;
        map.BakedBlocks!.Add(clone);
    }

    private static bool TryAddStandaloneAnchorBlock(
        CGameCtnChallenge map,
        CGameCtnBlock prototype,
        Int3 size,
        int yMin,
        int yMax,
        ISet<string> existingPlacements,
        out Int3? anchorCoord)
    {
        anchorCoord = null;

        if (size.Y < 3 || HasStandaloneNormalAnchor(map))
        {
            return false;
        }

        var targetY = DeterminePreferredAnchorY(map, size, yMin, yMax);
        var targetZ = Math.Clamp(size.Z / 2 - 1, 0, size.Z - 1);
        var startX = Math.Clamp(size.X - 8, 0, size.X - 1);

        for (var offset = 0; offset < size.X; offset++)
        {
            var xCandidates = new[]
            {
                startX - offset,
                startX + offset,
            };

            foreach (var x in xCandidates)
            {
                if (x < 0 || x >= size.X)
                {
                    continue;
                }

                var coord = new Int3(x, targetY, targetZ);
                var world = new Vec3(coord.X * 32f, coord.Y * 8f, coord.Z * 32f);
                var existingBlockAtCoord = (map.Blocks ?? [])
                    .FirstOrDefault(block => !block.IsFree && block.Coord == coord);

                if (existingBlockAtCoord is not null)
                {
                    if (string.Equals(existingBlockAtCoord.Name, WaterBlockName, StringComparison.OrdinalIgnoreCase) &&
                        existingBlockAtCoord.IsGhost &&
                        !existingBlockAtCoord.IsFree)
                    {
                        existingBlockAtCoord.IsGhost = false;
                        existingBlockAtCoord.IsFree = false;
                        existingBlockAtCoord.IsGround = false;
                        existingBlockAtCoord.Direction = Direction.North;
                        existingBlockAtCoord.Variant = 0;
                        existingBlockAtCoord.SubVariant = 0;
                        existingBlockAtCoord.AbsolutePositionInMap = null;
                        existingBlockAtCoord.YawPitchRoll = default;
                        anchorCoord = coord;
                        return true;
                    }

                    continue;
                }

                if (existingPlacements.Contains(BuildPlacementKey(WaterBlockName, world)))
                {
                    continue;
                }

                map.Blocks!.Add(CreatePlacedWaterBlock(prototype, coord, isGhost: false));
                existingPlacements.Add(BuildPlacementKey(WaterBlockName, world));
                anchorCoord = coord;
                return true;
            }
        }

        return false;
    }

    private static int DeterminePreferredAnchorY(CGameCtnChallenge map, Int3 size, int yMin, int yMax)
    {
        var normalWaterYs = (map.Blocks ?? [])
            .Where(block =>
                string.Equals(block.Name, WaterBlockName, StringComparison.OrdinalIgnoreCase) &&
                !block.IsGhost &&
                !block.IsFree)
            .Select(block => block.Coord.Y)
            .Distinct()
            .OrderBy(static y => y)
            .ToArray();

        if (normalWaterYs.Length > 0)
        {
            var minNormalY = normalWaterYs[0];
            var maxNormalY = normalWaterYs[^1];
            return Math.Clamp((minNormalY + maxNormalY) / 2, 1, size.Y - 2);
        }

        var fallbackY = yMin > 7
            ? yMin - 7
            : 12;

        return Math.Clamp(fallbackY, 1, size.Y - 2);
    }

    private static bool HasStandaloneNormalAnchor(CGameCtnChallenge map)
    {
        var normalWaterBlocks = (map.Blocks ?? [])
            .Where(block =>
                string.Equals(block.Name, WaterBlockName, StringComparison.OrdinalIgnoreCase) &&
                !block.IsGhost &&
                !block.IsFree)
            .ToList();

        if (normalWaterBlocks.Count == 0)
        {
            return false;
        }

        var coordKeys = normalWaterBlocks
            .Select(block => BuildCoordKey(block.Coord))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var block in normalWaterBlocks)
        {
            var belowKey = BuildCoordKey(new Int3(block.Coord.X, block.Coord.Y - 1, block.Coord.Z));
            var aboveKey = BuildCoordKey(new Int3(block.Coord.X, block.Coord.Y + 1, block.Coord.Z));
            if (!coordKeys.Contains(belowKey) && !coordKeys.Contains(aboveKey))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<(Int3 Coord, Direction Direction)> BuildAnchorEdgeCoords(Int3 anchor)
    {
        yield return (new Int3(anchor.X, anchor.Y, anchor.Z - 1), Direction.North);
        yield return (new Int3(anchor.X + 1, anchor.Y, anchor.Z), Direction.East);
        yield return (new Int3(anchor.X, anchor.Y, anchor.Z + 1), Direction.South);
        yield return (new Int3(anchor.X - 1, anchor.Y, anchor.Z), Direction.West);
    }

    private static Vec3 GetWorldPosition(CGameCtnBlock block)
    {
        if (block.AbsolutePositionInMap is Vec3 absolutePositionInMap)
        {
            return absolutePositionInMap;
        }

        return new Vec3(block.Coord.X * 32f, block.Coord.Y * 8f, block.Coord.Z * 32f);
    }

    private static string BuildCoordKey(Int3 coord)
        => $"{coord.X}|{coord.Y}|{coord.Z}";

    private static string FormatOptionalCoord(Int3? coord)
        => coord is Int3 value
            ? string.Create(CultureInfo.InvariantCulture, $"[{value.X}, {value.Y}, {value.Z}]")
            : "null";

    private static string BuildPlacementKey(string name, Vec3 world)
        => $"{name}|{world.X:F3}|{world.Y:F3}|{world.Z:F3}";

    private static int ReadIntOption(IReadOnlyDictionary<string, string> options, string name, int fallback)
        => options.TryGetValue(name, out var raw)
            ? int.Parse(raw, CultureInfo.InvariantCulture)
            : fallback;

    private static float ReadFloatOption(IReadOnlyDictionary<string, string> options, string name, float fallback)
        => options.TryGetValue(name, out var raw)
            ? float.Parse(raw, CultureInfo.InvariantCulture)
            : fallback;
}
