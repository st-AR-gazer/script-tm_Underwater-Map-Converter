#pragma warning disable GBXNET10001

using System.Globalization;
using GBX.NET;
using GBX.NET.Engines.Game;
using UnderwaterMapConverter.Infrastructure;

namespace UnderwaterMapConverter.Commands;

internal static class ExtrudeTemplateVolumeCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            return CliHelp.Write("Missing input map path for extrude-template-volume.");
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

        var prototypeFilter = positionalArgs.Count >= 3
            ? positionalArgs[2]
            : "RoadIceWithWallLeftDiagLeftStraightOn";

        var templateMapPath = optionArgs.TryGetValue("--template-map", out var templateMapRaw)
            ? Path.GetFullPath(templateMapRaw)
            : inputMapPath;
        var placementMode = optionArgs.TryGetValue("--placement-mode", out var placementModeRaw)
            ? placementModeRaw
            : "template-copy";
        var shells = ReadIntOption(optionArgs, "--shells", 1);
        var overscanBlocks = ReadIntOption(optionArgs, "--overscan-blocks", 5);
        var rotateQuarterTurns = ReadIntOption(optionArgs, "--rotate-quarter-turns", 0);
        var verticalStepWorld = ReadFloatOption(optionArgs, "--vertical-step-world", 8f);
        var requestedTemplateY = optionArgs.TryGetValue("--template-y", out var templateYRaw)
            ? int.Parse(templateYRaw, CultureInfo.InvariantCulture)
            : (int?)null;
        var emitPrototypeMapPath = optionArgs.TryGetValue("--emit-prototype-map", out var emitPrototypeMapRaw)
            ? Path.GetFullPath(emitPrototypeMapRaw)
            : inputMapPath;
        var emitPrototypeFilter = optionArgs.TryGetValue("--emit-prototype-filter", out var emitPrototypeFilterRaw)
            ? emitPrototypeFilterRaw
            : prototypeFilter;
        var emitNameOverride = optionArgs.TryGetValue("--emit-name-override", out var emitNameOverrideRaw)
            ? emitNameOverrideRaw
            : null;
        var mapNameSuffix = optionArgs.TryGetValue("--map-name-suffix", out var mapNameSuffixRaw)
            ? mapNameSuffixRaw
            : null;
        var write = flagArgs.Contains("--write");

        var gbx = GbxIo.ParseChallengeBestEffort(inputMapPath);
        var map = gbx.Node ?? throw new InvalidOperationException("Map node is null.");
        if (map.Blocks is null)
        {
            throw new InvalidOperationException("Map has no Blocks list.");
        }

        if (!File.Exists(templateMapPath))
        {
            throw new FileNotFoundException($"Template map not found: {templateMapPath}");
        }

        var templateGbx = GbxIo.ParseChallengeBestEffort(templateMapPath);
        var templateMap = templateGbx.Node ?? throw new InvalidOperationException("Template map node is null.");
        if (templateMap.Blocks is null)
        {
            throw new InvalidOperationException("Template map has no Blocks list.");
        }

        var matchingBlocks = templateMap.Blocks
            .Where(block => block.Name.Contains(prototypeFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matchingBlocks.Count == 0)
        {
            throw new InvalidOperationException($"Could not find any template blocks matching '{prototypeFilter}' in '{templateMapPath}'.");
        }

        var templateY = requestedTemplateY
                        ?? matchingBlocks
                            .GroupBy(block => block.Coord.Y)
                            .OrderByDescending(group => group.Count())
                            .ThenByDescending(group => group.Key)
                            .First()
                            .Key;

        var templateBlocks = matchingBlocks
            .Where(block => block.Coord.Y == templateY)
            .ToList();
        if (templateBlocks.Count == 0)
        {
            throw new InvalidOperationException($"No blocks matched template Y layer {templateY}.");
        }

        if (!File.Exists(emitPrototypeMapPath))
        {
            throw new FileNotFoundException($"Emit prototype map not found: {emitPrototypeMapPath}");
        }

        var emitPrototypeGbx = GbxIo.ParseChallengeBestEffort(emitPrototypeMapPath);
        var emitPrototypeMap = emitPrototypeGbx.Node ?? throw new InvalidOperationException("Emit prototype map node is null.");
        if (emitPrototypeMap.Blocks is null)
        {
            throw new InvalidOperationException("Emit prototype map has no Blocks list.");
        }

        var emitPrototype = emitPrototypeMap.Blocks
            .Where(block => block.Name.Contains(emitPrototypeFilter, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(block => block.Name.Contains(".Block.Gbx_CustomBlock", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(block => block.IsGhost)
            .FirstOrDefault();

        if (emitPrototype is null)
        {
            throw new InvalidOperationException($"Could not find an emit prototype matching '{emitPrototypeFilter}' in '{emitPrototypeMapPath}'.");
        }

        var size = map.Size;
        var mapWidthWorld = size.X * 32f;
        var mapDepthWorld = size.Z * 32f;
        var minAllowedWorldX = -overscanBlocks * 32f;
        var maxAllowedWorldX = ((size.X - 1) + overscanBlocks) * 32f;
        var minAllowedWorldZ = -overscanBlocks * 32f;
        var maxAllowedWorldZ = ((size.Z - 1) + overscanBlocks) * 32f;

        var templateWorldY = templateBlocks
            .Select(GetWorldPosition)
            .Average(position => position.Y);

        var minWorldY = ReadFloatOption(optionArgs, "--min-world-y", templateWorldY);
        var maxWorldY = ReadFloatOption(optionArgs, "--max-world-y", (size.Y - 1) * 8f);

        if (verticalStepWorld <= 0f)
        {
            throw new InvalidOperationException("--vertical-step-world must be > 0.");
        }

        if (maxWorldY < minWorldY)
        {
            throw new InvalidOperationException("--max-world-y must be >= --min-world-y.");
        }

        var layerWorldYs = BuildLayerWorldYs(minWorldY, maxWorldY, verticalStepWorld);
        var tileOffsets = BuildTileOffsets(shells, mapWidthWorld, mapDepthWorld);
        var emittedPrototypeName = string.IsNullOrWhiteSpace(emitNameOverride)
            ? RewritePrototypeName(emitPrototype.Name)
            : emitNameOverride;

        var projectedCount = 0L;
        foreach (var placement in BuildPlacements(
                     placementMode,
                     templateBlocks,
                     tileOffsets,
                     layerWorldYs,
                     minAllowedWorldX,
                     maxAllowedWorldX,
                     minAllowedWorldZ,
                     maxAllowedWorldZ,
                     templateWorldY,
                     rotateQuarterTurns))
        {
            projectedCount += 1;
        }

        if (!write)
        {
            Console.WriteLine($$"""
            {
              "mode": "dry-run",
              "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
              "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
              "prototypeFilter": "{{prototypeFilter}}",
                      "templateMapPath": "{{templateMapPath.Replace("\\", "\\\\")}}",
                      "templateY": {{templateY}},
                      "templateBlockCount": {{templateBlocks.Count}},
                      "placementMode": "{{placementMode}}",
                      "emitPrototypeMapPath": "{{emitPrototypeMapPath.Replace("\\", "\\\\")}}",
                      "emitPrototypeFilter": "{{emitPrototypeFilter.Replace("\\", "\\\\")}}",
                      "emitPrototypeName": "{{emittedPrototypeName.Replace("\\", "\\\\")}}",
                      "mapNameSuffix": "{{mapNameSuffix?.Replace("\\", "\\\\") ?? string.Empty}}",
                      "overscanBlocks": {{overscanBlocks}},
                      "rotateQuarterTurns": {{rotateQuarterTurns}},
                      "shells": {{shells}},
                      "tileCount": {{tileOffsets.Count}},
                      "verticalStepWorld": {{verticalStepWorld.ToString(CultureInfo.InvariantCulture)}},
              "minWorldY": {{minWorldY.ToString(CultureInfo.InvariantCulture)}},
              "maxWorldY": {{maxWorldY.ToString(CultureInfo.InvariantCulture)}},
              "layerCount": {{layerWorldYs.Count}},
              "projectedAddedBlockCount": {{projectedCount}},
              "note": "Pass --write to actually emit the output map."
            }
            """);

            return 0;
        }

        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var block in map.Blocks)
        {
            var world = GetWorldPosition(block);
            existing.Add(BuildPlacementKey(block.Name, world, block.Direction));
        }

        var added = 0;
        var skippedExisting = 0;

        foreach (var placement in BuildPlacements(
                     placementMode,
                     templateBlocks,
                     tileOffsets,
                     layerWorldYs,
                     minAllowedWorldX,
                     maxAllowedWorldX,
                     minAllowedWorldZ,
                     maxAllowedWorldZ,
                     templateWorldY,
                     rotateQuarterTurns))
        {
            var key = BuildPlacementKey(emittedPrototypeName, placement.World, placement.Direction);
            if (!existing.Add(key))
            {
                skippedExisting++;
                continue;
            }

            var clone = (CGameCtnBlock)emitPrototype.DeepClone();
            clone.Name = emittedPrototypeName;
            clone.BlockModel = clone.BlockModel with { Id = emittedPrototypeName };
            clone.IsGhost = true;
            clone.IsFree = true;
            clone.AbsolutePositionInMap = placement.World;
            clone.Direction = placement.Direction;
            clone.YawPitchRoll = new Vec3(AdditionalMath.ToRadians(placement.Direction), 0f, 0f);
            clone.Coord = new Int3(
                WorldToCoord(placement.World.X, 32f),
                WorldToCoord(placement.World.Y, 8f),
                WorldToCoord(placement.World.Z, 32f));
            map.Blocks.Add(clone);
            added++;
        }

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

        var embeddedCustomBlockCount = CustomBlockEmbedding.EmbedReferencedCustomBlocks(map);

        Directory.CreateDirectory(Path.GetDirectoryName(outputMapPath)!);
        gbx.Save(outputMapPath);

        Console.WriteLine($$"""
        {
          "mode": "write",
          "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
          "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
          "prototypeFilter": "{{prototypeFilter}}",
          "templateMapPath": "{{templateMapPath.Replace("\\", "\\\\")}}",
          "templateY": {{templateY}},
          "templateBlockCount": {{templateBlocks.Count}},
          "placementMode": "{{placementMode}}",
          "emitPrototypeMapPath": "{{emitPrototypeMapPath.Replace("\\", "\\\\")}}",
          "emitPrototypeFilter": "{{emitPrototypeFilter.Replace("\\", "\\\\")}}",
          "emitPrototypeName": "{{emittedPrototypeName.Replace("\\", "\\\\")}}",
          "mapNameSuffix": "{{mapNameSuffix?.Replace("\\", "\\\\") ?? string.Empty}}",
          "overscanBlocks": {{overscanBlocks}},
          "rotateQuarterTurns": {{rotateQuarterTurns}},
          "shells": {{shells}},
          "tileCount": {{tileOffsets.Count}},
          "verticalStepWorld": {{verticalStepWorld.ToString(CultureInfo.InvariantCulture)}},
          "minWorldY": {{minWorldY.ToString(CultureInfo.InvariantCulture)}},
          "maxWorldY": {{maxWorldY.ToString(CultureInfo.InvariantCulture)}},
          "layerCount": {{layerWorldYs.Count}},
          "addedBlockCount": {{added}},
          "skippedExistingCount": {{skippedExisting}},
          "embeddedCustomBlockCount": {{embeddedCustomBlockCount}},
          "totalBlockCount": {{map.Blocks.Count}}
        }
        """);

        return 0;
    }

    private static List<float> BuildLayerWorldYs(float minWorldY, float maxWorldY, float step)
    {
        var result = new List<float>();
        for (var current = minWorldY; current <= maxWorldY + 0.001f; current += step)
        {
            result.Add(current);
        }

        return result;
    }

    private static List<TileOffset> BuildTileOffsets(int shells, float mapWidthWorld, float mapDepthWorld)
    {
        var result = new List<TileOffset>();
        for (var tileX = -shells; tileX <= shells; tileX++)
        {
            for (var tileZ = -shells; tileZ <= shells; tileZ++)
            {
                result.Add(new TileOffset(
                    tileX,
                    tileZ,
                    tileX * mapWidthWorld,
                    tileZ * mapDepthWorld));
            }
        }

        return result;
    }

    private static IEnumerable<Placement> BuildPlacements(
        string placementMode,
        IReadOnlyList<CGameCtnBlock> templateBlocks,
        IReadOnlyList<TileOffset> tileOffsets,
        IReadOnlyList<float> layerWorldYs,
        float minAllowedWorldX,
        float maxAllowedWorldX,
        float minAllowedWorldZ,
        float maxAllowedWorldZ,
        float templateWorldY,
        int rotateQuarterTurns)
    {
        return placementMode.Trim().ToLowerInvariant() switch
        {
            "uniform-sheet" or "uniform" => BuildUniformSheetPlacements(
                templateBlocks,
                layerWorldYs,
                minAllowedWorldX,
                maxAllowedWorldX,
                minAllowedWorldZ,
                maxAllowedWorldZ,
                rotateQuarterTurns),
            _ => BuildTemplateCopyPlacements(
                templateBlocks,
                tileOffsets,
                layerWorldYs,
                minAllowedWorldX,
                maxAllowedWorldX,
                minAllowedWorldZ,
                maxAllowedWorldZ,
                templateWorldY,
                rotateQuarterTurns)
        };
    }

    private static IEnumerable<Placement> BuildTemplateCopyPlacements(
        IReadOnlyList<CGameCtnBlock> templateBlocks,
        IReadOnlyList<TileOffset> tileOffsets,
        IReadOnlyList<float> layerWorldYs,
        float minAllowedWorldX,
        float maxAllowedWorldX,
        float minAllowedWorldZ,
        float maxAllowedWorldZ,
        float templateWorldY,
        int rotateQuarterTurns)
    {
        foreach (var tileOffset in tileOffsets)
        {
            foreach (var layerWorldY in layerWorldYs)
            {
                var isOriginalSlice = tileOffset.TileX == 0 && tileOffset.TileZ == 0 && NearlyEqual(layerWorldY, templateWorldY);
                if (isOriginalSlice)
                {
                    continue;
                }

                foreach (var templateBlock in templateBlocks)
                {
                    var templateWorld = GetWorldPosition(templateBlock);
                    var targetWorld = new Vec3(
                        templateWorld.X + tileOffset.WorldOffsetX,
                        layerWorldY,
                        templateWorld.Z + tileOffset.WorldOffsetZ);

                    if (!IsWithinOverscanBounds(targetWorld, minAllowedWorldX, maxAllowedWorldX, minAllowedWorldZ, maxAllowedWorldZ))
                    {
                        continue;
                    }

                    yield return new Placement(
                        targetWorld,
                        RotateDirection(templateBlock.Direction, rotateQuarterTurns));
                }
            }
        }
    }

    private static IEnumerable<Placement> BuildUniformSheetPlacements(
        IReadOnlyList<CGameCtnBlock> templateBlocks,
        IReadOnlyList<float> layerWorldYs,
        float minAllowedWorldX,
        float maxAllowedWorldX,
        float minAllowedWorldZ,
        float maxAllowedWorldZ,
        int rotateQuarterTurns)
    {
        var coordXs = templateBlocks.Select(block => block.Coord.X).Distinct().OrderBy(x => x).ToArray();
        var coordZs = templateBlocks.Select(block => block.Coord.Z).Distinct().OrderBy(z => z).ToArray();

        var xStep = Math.Max(1, ComputeStep(coordXs));
        var zStep = Math.Max(1, ComputeStep(coordZs));

        var xResidue = PositiveModulo(coordXs.First(), xStep);
        var zResidue = PositiveModulo(coordZs.First(), zStep);

        var minCoordX = WorldToCoord(minAllowedWorldX, 32f);
        var maxCoordX = WorldToCoord(maxAllowedWorldX, 32f);
        var minCoordZ = WorldToCoord(minAllowedWorldZ, 32f);
        var maxCoordZ = WorldToCoord(maxAllowedWorldZ, 32f);

        var baseDirection = RotateDirection(templateBlocks[0].Direction, rotateQuarterTurns);

        for (var coordZ = minCoordZ; coordZ <= maxCoordZ; coordZ++)
        {
            if (PositiveModulo(coordZ, zStep) != zResidue)
            {
                continue;
            }

            for (var coordX = minCoordX; coordX <= maxCoordX; coordX++)
            {
                if (PositiveModulo(coordX, xStep) != xResidue)
                {
                    continue;
                }

                foreach (var layerWorldY in layerWorldYs)
                {
                    yield return new Placement(
                        new Vec3(coordX * 32f, layerWorldY, coordZ * 32f),
                        baseDirection);
                }
            }
        }
    }

    private static Vec3 GetWorldPosition(CGameCtnBlock block)
    {
        if (block.AbsolutePositionInMap is Vec3 absolutePositionInMap)
        {
            return absolutePositionInMap;
        }

        return new Vec3(block.Coord.X * 32f, block.Coord.Y * 8f, block.Coord.Z * 32f);
    }

    private static int WorldToCoord(float value, float unitSize)
        => (int)MathF.Floor(value / unitSize);

    private static bool NearlyEqual(float a, float b)
        => MathF.Abs(a - b) <= 0.001f;

    private static int ComputeStep(IReadOnlyList<int> values)
    {
        if (values.Count <= 1)
        {
            return 1;
        }

        var step = 0;
        for (var index = 1; index < values.Count; index++)
        {
            var diff = Math.Abs(values[index] - values[index - 1]);
            if (diff == 0)
            {
                continue;
            }

            step = step == 0 ? diff : Gcd(step, diff);
        }

        return step == 0 ? 1 : step;
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return Math.Abs(a);
    }

    private static int PositiveModulo(int value, int modulus)
        => ((value % modulus) + modulus) % modulus;

    private static bool IsWithinOverscanBounds(Vec3 world, float minX, float maxX, float minZ, float maxZ)
        => world.X >= minX && world.X <= maxX
           && world.Z >= minZ && world.Z <= maxZ;

    private static string RewritePrototypeName(string originalName)
        => originalName.Replace("Codex_MinimalWaterWrappers\\", "MinimalWaterWrappers\\", StringComparison.OrdinalIgnoreCase);

    private static Direction RotateDirection(Direction direction, int quarterTurns)
    {
        var normalizedTurns = ((quarterTurns % 4) + 4) % 4;
        return (Direction)(((int)direction + normalizedTurns) % 4);
    }

    private static string BuildPlacementKey(string name, Vec3 world, Direction direction)
        => $"{name}|{world.X:F3}|{world.Y:F3}|{world.Z:F3}|{direction}";

    private static int ReadIntOption(IReadOnlyDictionary<string, string> options, string name, int fallback)
        => options.TryGetValue(name, out var raw)
            ? int.Parse(raw, CultureInfo.InvariantCulture)
            : fallback;

    private static float ReadFloatOption(IReadOnlyDictionary<string, string> options, string name, float fallback)
        => options.TryGetValue(name, out var raw)
            ? float.Parse(raw, CultureInfo.InvariantCulture)
            : fallback;

    private readonly record struct TileOffset(int TileX, int TileZ, float WorldOffsetX, float WorldOffsetZ);
    private readonly record struct Placement(Vec3 World, Direction Direction);
}
