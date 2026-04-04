using GBX.NET.Engines.Game;

namespace UnderwaterMapConverter.Infrastructure;

internal static class UnderwaterMapPresetResolver
{
    private const string DefaultTemplateMapPathFallback = @"C:\Users\ar\Documents\Trackmania2020\Maps\My Maps\example map 3.Map.Gbx";
    private const string DefaultNormalPrototypeMapPathFallback = @"C:\Users\ar\Documents\Trackmania2020\Maps\My Maps\example map 3.Map.Gbx";
    public const string DefaultNormalPrototypeFilter = "RoadIceWithWallLeftDiagLeftStraightOnWaterHill";
    private const string WhiteShoreMeshlessPrototypeMapPathFallback = @"C:\Users\ar\Documents\Trackmania2020\Maps\My Maps\Water Test White Shore.Map.Gbx";
    private const string GreenCoastMeshlessPrototypeMapPathFallback = @"C:\Users\ar\Documents\Trackmania2020\Maps\My Maps\Water Test Great Coast.Map.Gbx";
    private const string RedIslandMeshlessPrototypeMapPathFallback = @"C:\Users\ar\Documents\Trackmania2020\Maps\My Maps\Water Test Red Island.Map.Gbx";
    private const string BlueBayMeshlessPrototypeMapPathFallback = @"C:\Users\ar\Documents\Trackmania2020\Maps\My Maps\Water Test Blue Bay.Map.Gbx";
    public const string DefaultPrototypeFilter = "RoadIceWithWallLeftDiagLeftStraightOn";

    public static string DefaultTemplateMapPath => ResolveBundledMapPath("example map 3.Map.Gbx", DefaultTemplateMapPathFallback);
    public static string DefaultNormalPrototypeMapPath => ResolveBundledMapPath("example map 3.Map.Gbx", DefaultNormalPrototypeMapPathFallback);

    public static string DetectEnvironment(CGameCtnChallenge map)
        => map.Collection?.ToString()
           ?? map.MapInfo.Collection.ToString()
           ?? string.Empty;

    public static bool TryResolve(string environment, out UnderwaterMapPreset preset)
    {
        preset = environment.Trim() switch
        {
            "WhiteShore" => new UnderwaterMapPreset(
                Environment: "WhiteShore",
                NormalEmitName: "RoadIceWithWallLeftDiagLeftStraightOnWaterShore1",
                MeshlessPrototypeMapPath: TryResolveBundledMapPath("Water Test White Shore.Map.Gbx", WhiteShoreMeshlessPrototypeMapPathFallback),
                MeshlessPrototypeFilter: "RoadIceWithWallLeftDiagLeftStraightOnWaterShore1",
                MeshlessEmitName: @"MinimalWaterWrappers\WhiteShore\Working\RoadIceWithWallLeftDiagLeftStraightOnWaterShore1.Block.Gbx_CustomBlock"),
            "GreenCoast" => new UnderwaterMapPreset(
                Environment: "GreenCoast",
                NormalEmitName: "RoadIceWithWallLeftDiagLeftStraightOnLakeShore",
                MeshlessPrototypeMapPath: TryResolveBundledMapPath("Water Test Great Coast.Map.Gbx", GreenCoastMeshlessPrototypeMapPathFallback),
                MeshlessPrototypeFilter: "RoadIceWithWallLeftDiagLeftStraightOnLakeShore",
                MeshlessEmitName: @"MinimalWaterWrappers\GreenCoast\Working\RoadIceWithWallLeftDiagLeftStraightOnLakeShore.Block.Gbx_CustomBlock"),
            "RedIsland" => new UnderwaterMapPreset(
                Environment: "RedIsland",
                NormalEmitName: "RoadIceWithWallLeftDiagLeftStraightOnWaterHill",
                MeshlessPrototypeMapPath: TryResolveBundledMapPath("Water Test Red Island.Map.Gbx", RedIslandMeshlessPrototypeMapPathFallback),
                MeshlessPrototypeFilter: "RoadIceWithWallLeftDiagLeftStraightOnWaterHill",
                MeshlessEmitName: @"MinimalWaterWrappers\RedIsland\Working\RoadIceWithWallLeftDiagLeftStraightOnWaterHill.Block.Gbx_CustomBlock"),
            "BlueBay" => new UnderwaterMapPreset(
                Environment: "BlueBay",
                NormalEmitName: "RoadIceWithWallLeftDiagLeftStraightOnBeach",
                MeshlessPrototypeMapPath: TryResolveBundledMapPath("Water Test Blue Bay.Map.Gbx", BlueBayMeshlessPrototypeMapPathFallback),
                MeshlessPrototypeFilter: "RoadIceWithWallLeftDiagLeftStraightOnBeach",
                MeshlessEmitName: @"MinimalWaterWrappers\BlueBay\Working\RoadIceWithWallLeftDiagLeftStraightOnBeach.Block.Gbx_CustomBlock"),
            _ => default
        };

        return !string.IsNullOrWhiteSpace(preset.Environment);
    }

    public static int DetermineOneLayerCoordY(CGameCtnChallenge map)
    {
        var startBlocks = (map.Blocks ?? [])
            .Where(block => block.Name.Contains("Start", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (startBlocks.Count > 0)
        {
            return startBlocks.Min(block => block.Coord.Y);
        }

        return (map.Blocks ?? []).Count > 0
            ? map.Blocks!.Min(block => block.Coord.Y)
            : 0;
    }

    private static string ResolveBundledMapPath(string fileName, string fallbackPath)
        => TryResolveBundledMapPath(fileName, fallbackPath) ?? fallbackPath;

    private static string? TryResolveBundledMapPath(string fileName, string? fallbackPath = null)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var currentDirectory = Environment.CurrentDirectory;
        var currentDirectoryParent = Directory.GetParent(currentDirectory)?.FullName;
        var candidates = new[]
        {
            Path.Combine(currentDirectory, fileName),
            Path.Combine(currentDirectory, "templates", fileName),
            currentDirectoryParent is null ? null : Path.Combine(currentDirectoryParent, fileName),
            currentDirectoryParent is null ? null : Path.Combine(currentDirectoryParent, "templates", fileName),
            Path.Combine(baseDirectory, "templates", fileName),
            Path.Combine(baseDirectory, fileName),
        }.Where(static path => !string.IsNullOrWhiteSpace(path)).Cast<string>();

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return !string.IsNullOrWhiteSpace(fallbackPath) && File.Exists(fallbackPath)
            ? fallbackPath
            : null;
    }
}

internal readonly record struct UnderwaterMapPreset(
    string Environment,
    string NormalEmitName,
    string? MeshlessPrototypeMapPath,
    string? MeshlessPrototypeFilter,
    string MeshlessEmitName);
