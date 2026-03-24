#pragma warning disable GBXNET10001

using System.Globalization;
using GBX.NET;
using GBX.NET.Engines.Game;
using UnderwaterMapConverter.Infrastructure;

namespace UnderwaterMapConverter.Commands;

internal static class FillStadiumWaterFromMacroblockCommand
{
    private const string WaterBlockName = "DecoWallWaterBase";
    private const string DefaultMacroblockPath = @"C:\Users\ar\Documents\Trackmania2020\Blocks\Stadium\macroblock_water_3x3.Macroblock.Gbx";

    public static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            return CliHelp.Write("Missing input map path for fill-stadium-water-macroblock.");
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
            : MapPathHelpers.BuildDefaultOutputPath(inputMapPath, "Water Macroblock Fill");

        var macroblockPath = optionArgs.TryGetValue("--macroblock-template", out var macroblockRaw)
            ? Path.GetFullPath(macroblockRaw)
            : DefaultMacroblockPath;
        if (!File.Exists(macroblockPath))
        {
            throw new FileNotFoundException($"Macroblock template not found: {macroblockPath}");
        }

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

        var macroblock = Gbx.Parse<CGameCtnMacroBlockInfo>(macroblockPath);
        var template = ExtractTemplate(macroblock.Node ?? throw new InvalidOperationException("Macroblock node is null."));

        var size = map.Size;
        var maxWorldY = (size.Y - 1) * 8f;
        var minLayerWorldY = ReadFloatOption(optionArgs, "--min-world-y", 0f);
        var maxLayerWorldY = ReadFloatOption(optionArgs, "--max-world-y", maxWorldY);

        if (maxLayerWorldY < minLayerWorldY)
        {
            throw new InvalidOperationException("--max-world-y must be >= --min-world-y.");
        }

        var minCoordY = (int)MathF.Floor(minLayerWorldY / 8f);
        var maxCoordY = (int)MathF.Floor(maxLayerWorldY / 8f);
        var layerCount = maxCoordY - minCoordY + 1;
        var projectedCount = (long)size.X * size.Z * layerCount;

        if (!write)
        {
            Console.WriteLine($$"""
            {
              "mode": "dry-run",
              "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
              "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
              "macroblockPath": "{{macroblockPath.Replace("\\", "\\\\")}}",
              "gridSize": [{{size.X}}, {{size.Y}}, {{size.Z}}],
              "minWorldY": {{minLayerWorldY.ToString(CultureInfo.InvariantCulture)}},
              "maxWorldY": {{maxLayerWorldY.ToString(CultureInfo.InvariantCulture)}},
              "layerCount": {{layerCount}},
              "projectedAddedBlockCount": {{projectedCount}},
              "lowerFlags": {{template.LowerFlags}},
              "lowerDirection": "{{template.LowerDirection}}",
              "topFlags": {{template.TopFlags}},
              "topDirection": "{{template.TopDirection}}",
              "note": "Pass --write to emit the macroblock-styled output map."
            }
            """);

            return 0;
        }

        var added = 0;
        var rewrittenExisting = 0;
        var prototype = map.Blocks[0];

        for (var coordZ = 0; coordZ < size.Z; coordZ++)
        {
            for (var coordX = 0; coordX < size.X; coordX++)
            {
                for (var coordY = minCoordY; coordY <= maxCoordY; coordY++)
                {
                    var coord = new Int3(coordX, coordY, coordZ);
                    var existingWater = map.Blocks
                        .FirstOrDefault(block =>
                            !block.IsFree &&
                            string.Equals(block.Name, WaterBlockName, StringComparison.OrdinalIgnoreCase) &&
                            block.Coord == coord);

                    var isTop = coordY == maxCoordY;
                    if (existingWater is not null)
                    {
                        ApplyMacroblockStyle(existingWater, template, isTop);
                        rewrittenExisting++;
                        continue;
                    }

                    map.Blocks.Add(CreateMacroblockStyledBlock(prototype, template, coord, isTop));
                    added++;
                }
            }
        }

        var bakedAdded = RegenerateMacroblockWaterBakedBlocks(map, prototype, 0, size.X - 1, 0, size.Z - 1, minCoordY, maxCoordY);

        if (!string.IsNullOrWhiteSpace(mapNameSuffix))
        {
            if (!map.MapName.EndsWith(mapNameSuffix, StringComparison.OrdinalIgnoreCase))
            {
                map.MapName = $"{map.MapName} {mapNameSuffix}".Trim();
            }
        }
        else if (!map.MapName.Contains("[Water Macroblock Fill]", StringComparison.Ordinal))
        {
            map.MapName = $"{map.MapName} [Water Macroblock Fill]";
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputMapPath)!);
        gbx.Save(outputMapPath);

        Console.WriteLine($$"""
        {
          "mode": "write",
          "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
          "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
          "macroblockPath": "{{macroblockPath.Replace("\\", "\\\\")}}",
          "gridSize": [{{size.X}}, {{size.Y}}, {{size.Z}}],
          "minWorldY": {{minLayerWorldY.ToString(CultureInfo.InvariantCulture)}},
          "maxWorldY": {{maxLayerWorldY.ToString(CultureInfo.InvariantCulture)}},
          "layerCount": {{layerCount}},
          "addedBlockCount": {{added}},
          "rewrittenExistingCount": {{rewrittenExisting}},
          "bakedBlockCount": {{bakedAdded}},
          "totalBlockCount": {{map.Blocks.Count}},
          "lowerFlags": {{template.LowerFlags}},
          "lowerDirection": "{{template.LowerDirection}}",
          "topFlags": {{template.TopFlags}},
          "topDirection": "{{template.TopDirection}}"
        }
        """);

        return 0;
    }

    private static MacroblockWaterTemplate ExtractTemplate(CGameCtnMacroBlockInfo macroblock)
    {
        var spawns = (macroblock.BlockSpawns ?? [])
            .Where(spawn =>
                spawn.BlockModel is not null &&
                string.Equals(spawn.BlockModel.Id, WaterBlockName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (spawns.Count == 0)
        {
            throw new InvalidOperationException($"Macroblock '{macroblock.Name}' contains no {WaterBlockName} block spawns.");
        }

        var topY = spawns.Max(spawn => spawn.Coord.Y);
        var topSpawns = spawns.Where(spawn => spawn.Coord.Y == topY).ToList();
        var lowerSpawns = spawns.Where(spawn => spawn.Coord.Y < topY).ToList();

        var topFlags = DistinctSingleValue(topSpawns.Select(spawn => spawn.Flags), "top spawn flags");
        var topDirection = DistinctSingleValue(topSpawns.Select(spawn => spawn.Direction), "top spawn directions");
        var topModel = DistinctSingleValue(topSpawns.Select(spawn => spawn.BlockModel), "top spawn block models");

        var lowerFlags = lowerSpawns.Count > 0
            ? DistinctSingleValue(lowerSpawns.Select(spawn => spawn.Flags), "lower spawn flags")
            : topFlags;
        var lowerDirection = lowerSpawns.Count > 0
            ? DistinctSingleValue(lowerSpawns.Select(spawn => spawn.Direction), "lower spawn directions")
            : topDirection;
        var lowerModel = lowerSpawns.Count > 0
            ? DistinctSingleValue(lowerSpawns.Select(spawn => spawn.BlockModel), "lower spawn block models")
            : topModel;

        return new MacroblockWaterTemplate(
            LowerModel: lowerModel,
            LowerFlags: lowerFlags,
            LowerDirection: lowerDirection,
            TopModel: topModel,
            TopFlags: topFlags,
            TopDirection: topDirection);
    }

    private static T DistinctSingleValue<T>(IEnumerable<T> values, string description) where T : notnull
    {
        var distinct = values.Distinct().ToArray();
        return distinct.Length switch
        {
            0 => throw new InvalidOperationException($"Macroblock has no {description}."),
            1 => distinct[0],
            _ => throw new InvalidOperationException($"Macroblock has multiple {description}: {string.Join(", ", distinct)}")
        };
    }

    private static CGameCtnBlock CreateMacroblockStyledBlock(
        CGameCtnBlock prototype,
        MacroblockWaterTemplate template,
        Int3 coord,
        bool isTop)
    {
        var clone = (CGameCtnBlock)prototype.DeepClone();
        ApplyMacroblockStyle(clone, template, isTop);
        clone.Coord = coord;
        return clone;
    }

    private static void ApplyMacroblockStyle(
        CGameCtnBlock block,
        MacroblockWaterTemplate template,
        bool isTop)
    {
        var style = isTop ? template.TopModel : template.LowerModel;
        var flags = isTop ? template.TopFlags : template.LowerFlags;
        var direction = isTop ? template.TopDirection : template.LowerDirection;

        block.Name = WaterBlockName;
        block.BlockModel = style;
        block.Flags = flags;
        block.Direction = direction;
        block.IsGhost = false;
        block.IsFree = false;
        block.IsGround = false;
        block.AbsolutePositionInMap = null;
        block.YawPitchRoll = default;
    }

    private static int RegenerateMacroblockWaterBakedBlocks(
        CGameCtnChallenge map,
        CGameCtnBlock prototype,
        int xMin,
        int xMax,
        int zMin,
        int zMax,
        int yMin,
        int yMax)
    {
        var preserved = (map.BakedBlocks ?? [])
            .Where(block =>
                !block.Name.StartsWith("DecoWallWater", StringComparison.OrdinalIgnoreCase) &&
                !ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id).StartsWith("DecoWallWater", StringComparison.OrdinalIgnoreCase))
            .ToList();

        map.BakedBlocks = preserved;
        var count = 0;

        for (var z = zMin; z <= zMax; z++)
        {
            for (var x = xMin; x <= xMax; x++)
            {
                for (var y = yMin; y <= yMax; y++)
                {
                    var coord = new Int3(x, y, z);
                    AddBakedBlock(map, prototype, "DecoWallWaterBaseFCB", coord with { Y = y - 1 }, Direction.North, variant: 0);
                    AddBakedBlock(map, prototype, "DecoWallWaterFCBInside", coord with { Y = y - 1 }, Direction.North, variant: 0);
                    AddBakedBlock(map, prototype, "DecoWallWaterFCT", coord with { Y = y + 1 }, Direction.North, variant: 0);
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
                    AddBakedBlock(map, prototype, "DecoWallWaterVFC", coord, edge.dir, variant: 3);

                    var hfcVariant = pos == edge.highCornerPos ? 7
                        : pos == edge.lowCornerPos ? 13
                        : 15;

                    AddBakedBlock(map, prototype, "DecoWallWaterHFCInsideShort", coord, edge.dir, variant: hfcVariant);
                    count += 2;
                }
            }
        }

        return count;
    }

    private static void AddBakedBlock(
        CGameCtnChallenge map,
        CGameCtnBlock prototype,
        string name,
        Int3 coord,
        Direction direction,
        int variant)
    {
        var clone = (CGameCtnBlock)prototype.DeepClone();
        clone.Name = name;
        clone.BlockModel = clone.BlockModel with { Id = name };
        clone.Coord = coord;
        clone.Direction = direction;
        clone.Variant = (byte)variant;
        clone.SubVariant = 0;
        clone.IsGhost = false;
        clone.IsFree = false;
        clone.IsGround = false;
        clone.AbsolutePositionInMap = null;
        clone.YawPitchRoll = default;
        clone.Color = default;
        map.BakedBlocks!.Add(clone);
    }

    private static Vec3 GetWorldPosition(CGameCtnBlock block)
    {
        if (block.AbsolutePositionInMap is Vec3 absolutePositionInMap)
        {
            return absolutePositionInMap;
        }

        return new Vec3(block.Coord.X * 32f, block.Coord.Y * 8f, block.Coord.Z * 32f);
    }

    private static string BuildPlacementKey(string name, Vec3 world)
        => $"{name}|{world.X:F3}|{world.Y:F3}|{world.Z:F3}";

    private static float ReadFloatOption(IReadOnlyDictionary<string, string> options, string name, float fallback)
        => options.TryGetValue(name, out var raw)
            ? float.Parse(raw, CultureInfo.InvariantCulture)
            : fallback;

    private readonly record struct MacroblockWaterTemplate(
        Ident LowerModel,
        int LowerFlags,
        Direction LowerDirection,
        Ident TopModel,
        int TopFlags,
        Direction TopDirection);
}
