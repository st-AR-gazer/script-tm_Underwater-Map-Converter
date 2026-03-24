using GBX.NET;
using GBX.NET.Engines.Game;
using UnderwaterMapConverter.Infrastructure;

namespace UnderwaterMapConverter.Commands;

internal static class StampMacroblockCommand
{
    private const string DefaultMacroblockPath = @"C:\Users\ar\Documents\Trackmania2020\Blocks\Stadium\macroblock_water_3x3.Macroblock.Gbx";

    public static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            return CliHelp.Write("Missing input map path for stamp-macroblock.");
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
            : MapPathHelpers.BuildDefaultOutputPath(inputMapPath, "Macroblock Stamped");

        var macroblockPath = optionArgs.TryGetValue("--macroblock-template", out var macroblockRaw)
            ? Path.GetFullPath(macroblockRaw)
            : DefaultMacroblockPath;
        if (!File.Exists(macroblockPath))
        {
            throw new FileNotFoundException($"Macroblock template not found: {macroblockPath}");
        }

        var write = flagArgs.Contains("--write");

        var gbx = GbxIo.ParseChallengeBestEffort(inputMapPath);
        var map = gbx.Node ?? throw new InvalidOperationException("Map node is null.");
        map.Blocks ??= [];

        var macroblock = Gbx.Parse<CGameCtnMacroBlockInfo>(macroblockPath);
        var macroblockNode = macroblock.Node ?? throw new InvalidOperationException("Macroblock node is null.");
        var spawns = (macroblockNode.BlockSpawns ?? []).ToList();
        if (spawns.Count == 0)
        {
            throw new InvalidOperationException("Macroblock has no BlockSpawns.");
        }

        var minX = spawns.Min(spawn => spawn.Coord.X);
        var maxX = spawns.Max(spawn => spawn.Coord.X);
        var minY = spawns.Min(spawn => spawn.Coord.Y);
        var maxY = spawns.Max(spawn => spawn.Coord.Y);
        var minZ = spawns.Min(spawn => spawn.Coord.Z);
        var maxZ = spawns.Max(spawn => spawn.Coord.Z);

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var depth = maxZ - minZ + 1;

        var size = map.Size;
        var baseX = optionArgs.TryGetValue("--coord-x", out var coordXRaw)
            ? int.Parse(coordXRaw, System.Globalization.CultureInfo.InvariantCulture)
            : Math.Max(0, (size.X - width) / 2);
        var baseY = optionArgs.TryGetValue("--coord-y", out var coordYRaw)
            ? int.Parse(coordYRaw, System.Globalization.CultureInfo.InvariantCulture)
            : DetermineDefaultY(map);
        var baseZ = optionArgs.TryGetValue("--coord-z", out var coordZRaw)
            ? int.Parse(coordZRaw, System.Globalization.CultureInfo.InvariantCulture)
            : Math.Max(0, (size.Z - depth) / 2);

        var targetBounds = new Int3(baseX + width - 1, baseY + height - 1, baseZ + depth - 1);
        if (baseX < 0 || baseY < 0 || baseZ < 0 || targetBounds.X >= size.X || targetBounds.Y >= size.Y || targetBounds.Z >= size.Z)
        {
            throw new InvalidOperationException("Macroblock placement would exceed map bounds.");
        }

        var existingCoords = map.Blocks
            .Where(block => !block.IsFree)
            .Select(block => BuildCoordKey(block.Coord))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var translatedCoords = spawns
            .Select(spawn => new Int3(baseX + (spawn.Coord.X - minX), baseY + (spawn.Coord.Y - minY), baseZ + (spawn.Coord.Z - minZ)))
            .ToList();

        var overlappingCoords = translatedCoords
            .Where(coord => existingCoords.Contains(BuildCoordKey(coord)))
            .Distinct()
            .ToList();

        if (!write)
        {
            Console.WriteLine($$"""
            {
              "mode": "dry-run",
              "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
              "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
              "macroblockPath": "{{macroblockPath.Replace("\\", "\\\\")}}",
              "macroblockName": "{{macroblockNode.Name}}",
              "spawnCount": {{spawns.Count}},
              "normalizedBounds": {
                "width": {{width}},
                "height": {{height}},
                "depth": {{depth}}
              },
              "targetMinCoord": [{{baseX}}, {{baseY}}, {{baseZ}}],
              "targetMaxCoord": [{{targetBounds.X}}, {{targetBounds.Y}}, {{targetBounds.Z}}],
              "overlappingCoordCount": {{overlappingCoords.Count}},
              "note": "Pass --write to stamp the macroblock into the map."
            }
            """);

            return 0;
        }

        if (overlappingCoords.Count > 0)
        {
            throw new InvalidOperationException($"Macroblock placement overlaps {overlappingCoords.Count} existing block coordinates.");
        }

        var instance = new MacroblockInstance();
        map.MacroblockInstances ??= [];
        map.MacroblockInstances.Add(instance);

        foreach (var spawn in spawns)
        {
            var coord = new Int3(baseX + (spawn.Coord.X - minX), baseY + (spawn.Coord.Y - minY), baseZ + (spawn.Coord.Z - minZ));
            var block = new CGameCtnBlock
            {
                Name = spawn.BlockModel.Id,
                BlockModel = spawn.BlockModel,
                Coord = coord,
                Direction = spawn.Direction,
                Flags = spawn.Flags,
                MacroblockReference = instance,
            };

            map.Blocks.Add(block);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputMapPath)!);
        gbx.Save(outputMapPath);

        Console.WriteLine($$"""
        {
          "mode": "write",
          "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
          "outputMapPath": "{{outputMapPath.Replace("\\", "\\\\")}}",
          "macroblockPath": "{{macroblockPath.Replace("\\", "\\\\")}}",
          "macroblockName": "{{macroblockNode.Name}}",
          "spawnCount": {{spawns.Count}},
          "targetMinCoord": [{{baseX}}, {{baseY}}, {{baseZ}}],
          "targetMaxCoord": [{{targetBounds.X}}, {{targetBounds.Y}}, {{targetBounds.Z}}],
          "macroblockInstanceCount": {{map.MacroblockInstances.Count}},
          "totalBlockCount": {{map.Blocks.Count}}
        }
        """);

        return 0;
    }

    private static int DetermineDefaultY(CGameCtnChallenge map)
    {
        var bakedGrassYs = (map.BakedBlocks ?? [])
            .Where(block => string.Equals(block.Name, "Grass", StringComparison.OrdinalIgnoreCase))
            .Select(block => block.Coord.Y)
            .ToArray();

        if (bakedGrassYs.Length > 0)
        {
            return bakedGrassYs.Max() + 1;
        }

        return 10;
    }

    private static string BuildCoordKey(Int3 coord)
        => $"{coord.X}|{coord.Y}|{coord.Z}";
}
