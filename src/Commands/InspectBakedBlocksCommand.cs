using System.Globalization;
using System.Text.Json;
using GBX.NET.Engines.Game;
using UnderwaterMapConverter.Infrastructure;

namespace UnderwaterMapConverter.Commands;

internal static class InspectBakedBlocksCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            return CliHelp.Write("Usage: inspect-baked-blocks <mapPath> <type-id-or-substring> [limit]");
        }

        var mapPath = Path.GetFullPath(args[0]);
        if (!File.Exists(mapPath))
        {
            throw new FileNotFoundException($"Map not found: {mapPath}");
        }

        var filter = args[1].Trim();
        var limit = 20;
        if (args.Length >= 3)
        {
            limit = int.Parse(args[2], CultureInfo.InvariantCulture);
        }

        if (limit < 0)
        {
            throw new InvalidOperationException("limit must be >= 0.");
        }

        var gbx = GbxIo.ParseChallengeBestEffort(mapPath);
        var map = gbx.Node ?? throw new InvalidOperationException("Map node is null.");
        var bakedBlocks = map.BakedBlocks ?? [];

        var normalizedFilter = filter;
        var exactMatch = bakedBlocks.Any(block =>
            string.Equals(ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id), normalizedFilter, StringComparison.OrdinalIgnoreCase));

        var matches = new List<object>();
        var matchCount = 0;

        for (var index = 0; index < bakedBlocks.Count; index++)
        {
            var block = bakedBlocks[index];
            var typeId = ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id);

            var isMatch = exactMatch
                ? string.Equals(typeId, normalizedFilter, StringComparison.OrdinalIgnoreCase)
                : typeId.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase);

            if (!isMatch)
            {
                continue;
            }

            matchCount += 1;

            if (matches.Count >= limit)
            {
                continue;
            }

            matches.Add(new
            {
                index,
                name = block.Name,
                typeId,
                rawId = block.BlockModel.Id,
                coord = new { x = block.Coord.X, y = block.Coord.Y, z = block.Coord.Z },
                direction = block.Direction.ToString(),
                flags = block.Flags,
                variant = block.Variant,
                subVariant = block.SubVariant,
                isGround = block.IsGround,
                isClip = block.IsClip,
                isPillar = block.IsPillar,
                isGhost = block.IsGhost,
                isFree = block.IsFree,
                decalId = block.DecalId,
                decalIntensity = block.DecalIntensity,
                decalVariant = block.DecalVariant,
                color = block.Color.ToString(),
                lightmapQuality = block.LightmapQuality.ToString(),
            });
        }

        var report = new
        {
            mapPath,
            filter,
            bakedBlockCount = bakedBlocks.Count,
            matchCount,
            usedExactMatch = exactMatch,
            limit,
            blocks = matches,
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
        return 0;
    }
}

