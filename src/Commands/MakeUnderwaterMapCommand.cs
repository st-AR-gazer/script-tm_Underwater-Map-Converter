using System.Globalization;
using UnderwaterMapConverter.Infrastructure;

namespace UnderwaterMapConverter.Commands;

internal static class MakeUnderwaterMapCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            return CliHelp.Write("Usage: make-underwater-map <inputMapPath> <suffix> [--variant normal|meshless|both] [--coverage one-layer|full-stack]");
        }

        var optionArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var positionalArgs = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                positionalArgs.Add(arg);
                continue;
            }

            if (index + 1 >= args.Length)
            {
                return CliHelp.Write($"Missing value after {arg}.");
            }

            optionArgs[arg] = args[index + 1];
            index++;
        }

        var inputMapPath = Path.GetFullPath(positionalArgs[0]);
        var suffix = positionalArgs[1];

        if (!File.Exists(inputMapPath))
        {
            return CliHelp.Write($"Input map not found: {inputMapPath}");
        }

        var variant = optionArgs.TryGetValue("--variant", out var variantRaw)
            ? variantRaw.Trim().ToLowerInvariant()
            : "normal";
        var coverage = optionArgs.TryGetValue("--coverage", out var coverageRaw)
            ? coverageRaw.Trim().ToLowerInvariant()
            : "full-stack";
        var overscanBlocks = optionArgs.TryGetValue("--overscan-blocks", out var overscanRaw)
            ? int.Parse(overscanRaw, CultureInfo.InvariantCulture)
            : 5;
        var rotateQuarterTurns = optionArgs.TryGetValue("--rotate-quarter-turns", out var rotateRaw)
            ? int.Parse(rotateRaw, CultureInfo.InvariantCulture)
            : 0;

        var gbx = GbxIo.ParseChallengeBestEffort(inputMapPath);
        var map = gbx.Node ?? throw new InvalidOperationException("Map node is null.");
        var environment = UnderwaterMapPresetResolver.DetectEnvironment(map, inputMapPath);
        if (!UnderwaterMapPresetResolver.TryResolve(environment, out var preset))
        {
            return CliHelp.Write($"Unsupported environment for make-underwater-map: '{environment}'.");
        }

        var maxWorldY = (map.Size.Y - 1) * 8;
        var oneLayerCoordY = UnderwaterMapPresetResolver.DetermineOneLayerCoordY(map);
        var oneLayerWorldY = oneLayerCoordY * 8;

        var minWorldY = coverage switch
        {
            "one-layer" => oneLayerWorldY,
            "full-stack" => 0,
            _ => throw new InvalidOperationException($"Unknown coverage '{coverage}'.")
        };

        var effectiveMaxWorldY = coverage switch
        {
            "one-layer" => oneLayerWorldY,
            "full-stack" => maxWorldY,
            _ => maxWorldY
        };

        var runNormal = variant is "normal" or "both";
        var runMeshless = variant is "meshless" or "both";
        if (!runNormal && !runMeshless)
        {
            return CliHelp.Write($"Unknown variant '{variant}'. Use normal, meshless, or both.");
        }

        if (runNormal)
        {
            var outputPath = MapPathHelpers.BuildSuffixedOutputPath(inputMapPath, runMeshless ? $"{suffix} Normal" : suffix);
            var exitCode = ExtrudeTemplateVolumeCommand.Run(
            [
                inputMapPath,
                outputPath,
                UnderwaterMapPresetResolver.DefaultPrototypeFilter,
                "--template-map", UnderwaterMapPresetResolver.DefaultTemplateMapPath,
                "--placement-mode", "uniform-sheet",
                "--emit-prototype-map", UnderwaterMapPresetResolver.DefaultNormalPrototypeMapPath,
                "--emit-prototype-filter", UnderwaterMapPresetResolver.DefaultNormalPrototypeFilter,
                "--emit-name-override", preset.NormalEmitName,
                "--overscan-blocks", overscanBlocks.ToString(CultureInfo.InvariantCulture),
                "--rotate-quarter-turns", rotateQuarterTurns.ToString(CultureInfo.InvariantCulture),
                "--min-world-y", minWorldY.ToString(CultureInfo.InvariantCulture),
                "--max-world-y", effectiveMaxWorldY.ToString(CultureInfo.InvariantCulture),
                "--write"
            ]);

            if (exitCode != 0)
            {
                return exitCode;
            }
        }

        if (runMeshless)
        {
            var outputPath = MapPathHelpers.BuildSuffixedOutputPath(inputMapPath, runNormal ? $"{suffix} Meshless" : suffix);
            var exitCode = ExtrudeTemplateVolumeCommand.Run(
            [
                inputMapPath,
                outputPath,
                UnderwaterMapPresetResolver.DefaultPrototypeFilter,
                "--template-map", UnderwaterMapPresetResolver.DefaultTemplateMapPath,
                "--placement-mode", "uniform-sheet",
                "--emit-prototype-map", UnderwaterMapPresetResolver.DefaultMeshlessPrototypeMapPath,
                "--emit-prototype-filter", UnderwaterMapPresetResolver.DefaultMeshlessPrototypeFilter,
                "--emit-name-override", preset.MeshlessEmitName,
                "--overscan-blocks", overscanBlocks.ToString(CultureInfo.InvariantCulture),
                "--rotate-quarter-turns", rotateQuarterTurns.ToString(CultureInfo.InvariantCulture),
                "--min-world-y", minWorldY.ToString(CultureInfo.InvariantCulture),
                "--max-world-y", effectiveMaxWorldY.ToString(CultureInfo.InvariantCulture),
                "--write"
            ]);

            if (exitCode != 0)
            {
                return exitCode;
            }
        }

        Console.WriteLine($$"""
        {
          "inputMapPath": "{{inputMapPath.Replace("\\", "\\\\")}}",
          "suffix": "{{suffix}}",
          "environment": "{{environment}}",
          "coverage": "{{coverage}}",
          "variant": "{{variant}}",
          "overscanBlocks": {{overscanBlocks}},
          "rotateQuarterTurns": {{rotateQuarterTurns}},
          "oneLayerCoordY": {{oneLayerCoordY}},
          "minWorldY": {{minWorldY}},
          "maxWorldY": {{effectiveMaxWorldY}}
        }
        """);

        return 0;
    }
}
