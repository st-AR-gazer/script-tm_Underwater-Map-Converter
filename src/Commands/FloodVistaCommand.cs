using GBX.NET;
using GBX.NET.Engines.Game;
using UnderwaterMapConverter.Infrastructure;

namespace UnderwaterMapConverter.Commands;

internal static class FloodVistaCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            return CliHelp.Write("Missing input map path for flood-vista.");
        }

        var optionArgs = args.Where(static arg => arg.StartsWith("--", StringComparison.Ordinal)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var positionalArgs = args.Where(static arg => !arg.StartsWith("--", StringComparison.Ordinal)).ToArray();

        var preserveTerrainY = optionArgs.Contains("--preserve-terrain-y");
        var setDecoOffset = optionArgs.Contains("--set-deco-offset");

        var inputMapPath = Path.GetFullPath(positionalArgs[0]);
        if (!File.Exists(inputMapPath))
        {
            throw new FileNotFoundException($"Input map not found: {inputMapPath}");
        }

        var outputMapPath = positionalArgs.Length >= 2
            ? Path.GetFullPath(positionalArgs[1])
            : MapPathHelpers.BuildDefaultOutputPath(inputMapPath, "All Water Prototype");

        int? requestedFloodY = null;
        if (positionalArgs.Length >= 3)
        {
            if (!int.TryParse(positionalArgs[2], out var parsedFloodY))
            {
                throw new InvalidOperationException($"Invalid floodY '{positionalArgs[2]}'.");
            }

            requestedFloodY = parsedFloodY;
        }

        var gbx = GbxIo.ParseChallengeBestEffort(inputMapPath);
        var map = gbx.Node ?? throw new InvalidOperationException("Map node is null.");

        var environment = ReflectionAccess.TryExtractEnvironmentFromHeader(inputMapPath)
                          ?? map.Collection?.ToString()
                          ?? string.Empty;
        var targetWaterZoneId = ResolveTargetWaterZoneId(environment);
        if (targetWaterZoneId is null)
        {
            throw new InvalidOperationException($"Unsupported or unknown environment '{environment}'.");
        }

        var genealogies = map.ZoneGenealogy;
        if (genealogies is null || genealogies.Count == 0)
        {
            throw new InvalidOperationException("Map has no ZoneGenealogy entries.");
        }

        var blocks = map.Blocks;
        if (blocks is null || blocks.Count == 0)
        {
            throw new InvalidOperationException("Map has no blocks.");
        }

        var waterGenealogyPrototype = genealogies
            .FirstOrDefault(g => string.Equals(ReflectionAccess.ReadStringProperty(g, "CurrentZoneId"), targetWaterZoneId, StringComparison.OrdinalIgnoreCase));
        if (waterGenealogyPrototype is null)
        {
            throw new InvalidOperationException($"Could not find a '{targetWaterZoneId}' genealogy prototype in the map.");
        }

        var waterBlockPrototype = blocks
            .FirstOrDefault(block =>
                block.IsGround &&
                string.Equals(ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id), targetWaterZoneId, StringComparison.OrdinalIgnoreCase));
        if (waterBlockPrototype is null)
        {
            throw new InvalidOperationException($"Could not find a '{targetWaterZoneId}' terrain block prototype in the map.");
        }

        var terrainTypeIds = CollectTerrainTypeIds(genealogies, environment);
        var defaultFloodY = ComputeDefaultFloodY(blocks, terrainTypeIds, targetWaterZoneId, waterBlockPrototype.Coord.Y);
        var floodY = requestedFloodY ?? defaultFloodY;
        var originalWaterY = waterBlockPrototype.Coord.Y;
        var originalDecoBaseHeightOffset = ReflectionAccess.ReadInt32Property(map, "DecoBaseHeightOffset");
        var targetDecoBaseHeightOffset = floodY - originalWaterY;

        var prototypeZoneIds = ReflectionAccess.ReadStringArrayProperty(waterGenealogyPrototype, "ZoneIds");
        var prototypeCurrentIndex = ReflectionAccess.ReadInt32Property(waterGenealogyPrototype, "CurrentIndex");
        var prototypeDir = ReflectionAccess.ReadEnumProperty<Direction>(waterGenealogyPrototype, "Dir");
        var prototypeCurrentZoneId = ReflectionAccess.ReadStringProperty(waterGenealogyPrototype, "CurrentZoneId") ?? targetWaterZoneId;

        var rewrittenGenealogyCount = 0;
        foreach (var genealogy in genealogies)
        {
            ReflectionAccess.SetProperty(genealogy, "ZoneIds", prototypeZoneIds.ToArray());
            ReflectionAccess.SetProperty(genealogy, "CurrentIndex", prototypeCurrentIndex);
            ReflectionAccess.SetProperty(genealogy, "Dir", prototypeDir);
            ReflectionAccess.SetProperty(genealogy, "CurrentZoneId", prototypeCurrentZoneId);
            rewrittenGenealogyCount += 1;
        }

        var rewrittenTerrainBlockCount = 0;
        foreach (var block in blocks)
        {
            if (!block.IsGround)
            {
                continue;
            }

            var typeId = ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id);
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
            ReflectionAccess.SetProperty(map, "DecoBaseHeightOffset", targetDecoBaseHeightOffset);
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
          "rewrittenTerrainBlockCount": {{rewrittenTerrainBlockCount}}
        }
        """);

        return 0;
    }

    private static string? ResolveTargetWaterZoneId(string environment) => environment.Trim() switch
    {
        "WhiteShore" => "Water",
        "RedIsland" => "Water",
        "BlueBay" => "Sea",
        "GreenCoast" => "Lake",
        _ => null,
    };

    private static HashSet<string> CollectTerrainTypeIds(IEnumerable<object> genealogies, string environment)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var genealogy in genealogies)
        {
            var currentZoneId = ReflectionAccess.ReadStringProperty(genealogy, "CurrentZoneId");
            if (!string.IsNullOrWhiteSpace(currentZoneId) && !currentZoneId.StartsWith("VoidTo", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(currentZoneId);
            }

            foreach (var zoneId in ReflectionAccess.ReadStringArrayProperty(genealogy, "ZoneIds"))
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

    private static IEnumerable<string> GetEnvironmentTerrainExtras(string environment) => environment.Trim() switch
    {
        "WhiteShore" => ["DecoTerrainRocky"],
        _ => Array.Empty<string>(),
    };

    private static int ComputeDefaultFloodY(
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

            var typeId = ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id);

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

    private static bool IsFloodReferenceBlock(string typeId, CGameCtnBlock block)
    {
        if (block.WaypointSpecialProperty is not null)
        {
            return true;
        }

        return typeId.StartsWith("Road", StringComparison.OrdinalIgnoreCase)
               || typeId.StartsWith("Platform", StringComparison.OrdinalIgnoreCase)
               || typeId.StartsWith("TrackWall", StringComparison.OrdinalIgnoreCase);
    }
}
