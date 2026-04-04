using GBX.NET.Engines.Game;

namespace UnderwaterMapConverter.Infrastructure;

internal static class UnderwaterMapPresetResolver
{
    public const string DefaultNormalPrototypeFilter = "RoadIceWithWallLeftDiagLeftStraightOnWaterHill";
    public const string DefaultPrototypeFilter = "RoadIceWithWallLeftDiagLeftStraightOn";

    public static string DefaultTemplateMapPath => BundledAssetResolver.ResolveMapPath("example map 3.Map.Gbx");
    public static string DefaultNormalPrototypeMapPath => BundledAssetResolver.ResolveMapPath("example map 3.Map.Gbx");

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
                MeshlessPrototypeMapPath: BundledAssetResolver.ResolveMapPath("Water Test White Shore.Map.Gbx"),
                MeshlessPrototypeFilter: "RoadIceWithWallLeftDiagLeftStraightOnWaterShore1",
                MeshlessEmitName: @"MinimalWaterWrappers\WhiteShore\Working\RoadIceWithWallLeftDiagLeftStraightOnWaterShore1.Block.Gbx_CustomBlock"),
            "GreenCoast" => new UnderwaterMapPreset(
                Environment: "GreenCoast",
                NormalEmitName: "RoadIceWithWallLeftDiagLeftStraightOnLakeShore",
                MeshlessPrototypeMapPath: BundledAssetResolver.ResolveMapPath("Water Test Great Coast.Map.Gbx"),
                MeshlessPrototypeFilter: "RoadIceWithWallLeftDiagLeftStraightOnLakeShore",
                MeshlessEmitName: @"MinimalWaterWrappers\GreenCoast\Working\RoadIceWithWallLeftDiagLeftStraightOnLakeShore.Block.Gbx_CustomBlock"),
            "RedIsland" => new UnderwaterMapPreset(
                Environment: "RedIsland",
                NormalEmitName: "RoadIceWithWallLeftDiagLeftStraightOnWaterHill",
                MeshlessPrototypeMapPath: BundledAssetResolver.ResolveMapPath("Water Test Red Island.Map.Gbx"),
                MeshlessPrototypeFilter: "RoadIceWithWallLeftDiagLeftStraightOnWaterHill",
                MeshlessEmitName: @"MinimalWaterWrappers\RedIsland\Working\RoadIceWithWallLeftDiagLeftStraightOnWaterHill.Block.Gbx_CustomBlock"),
            "BlueBay" => new UnderwaterMapPreset(
                Environment: "BlueBay",
                NormalEmitName: "RoadIceWithWallLeftDiagLeftStraightOnBeach",
                MeshlessPrototypeMapPath: BundledAssetResolver.ResolveMapPath("Water Test Blue Bay.Map.Gbx"),
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

}

internal readonly record struct UnderwaterMapPreset(
    string Environment,
    string NormalEmitName,
    string? MeshlessPrototypeMapPath,
    string? MeshlessPrototypeFilter,
    string MeshlessEmitName);
