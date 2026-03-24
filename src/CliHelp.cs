namespace UnderwaterMapConverter;

internal static class CliHelp
{
    public static int Write(string? error = null)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine();
        }

        Console.WriteLine("UnderwaterMapConverter");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  make-underwater-map <inputMapPath> <suffix> [--variant normal|meshless|both] [--coverage one-layer|full-stack] [--overscan-blocks N] [--rotate-quarter-turns N]");
        Console.WriteLine("  convert <inputMapPath> [outputMapPath] [--method carrier-lattice|vista-flood|template-volume] [method options]");
        Console.WriteLine("  testing [legacy MapViewer.Testing arguments]");
        Console.WriteLine("  extrude-template-volume <inputMapPath> [outputMapPath] [prototypeFilter] [--template-y N] [--template-map PATH] [--placement-mode template-copy|uniform-sheet] [--emit-prototype-map PATH] [--emit-prototype-filter TEXT] [--emit-name-override TEXT] [--shells N] [--overscan-blocks N] [--rotate-quarter-turns N] [--vertical-step-world F] [--min-world-y F] [--max-world-y F] [--write]");
        Console.WriteLine("  flood-vista <inputMapPath> [outputMapPath] [floodY] [--preserve-terrain-y] [--set-deco-offset]");
        Console.WriteLine("  place-water-carrier-lattice <inputMapPath> [outputMapPath] [prototypeFilter]");
        Console.WriteLine("  fill-stadium-water <inputMapPath> [outputMapPath] [--min-world-y F] [--max-world-y F] [--map-name-suffix TEXT] [--limit N] [--write]");
        Console.WriteLine("  fill-stadium-water-macroblock <inputMapPath> [outputMapPath] [--macroblock-template PATH] [--min-world-y F] [--max-world-y F] [--map-name-suffix TEXT] [--write]");
        Console.WriteLine("  stamp-macroblock <inputMapPath> [outputMapPath] [--macroblock-template PATH] [--coord-x N] [--coord-y N] [--coord-z N] [--write]");
        Console.WriteLine("  diff-blocks <referenceMapPath> <actualMapPath> [--filter TEXT] [--limit N] [--write-json PATH]");
        Console.WriteLine("  diff-baked-blocks <referenceMapPath> <actualMapPath> [--filter TEXT] [--limit N] [--write-json PATH]");
        Console.WriteLine("  inspect-baked-blocks <mapPath> <type-id-or-substring> [limit]");
        Console.WriteLine();
        Console.WriteLine("Convert methods:");
        Console.WriteLine("  carrier-lattice   Default. Fills the map with repeated water-carrier lines.");
        Console.WriteLine("  vista-flood       Rewrites vista terrain topology into all-water terrain.");
        Console.WriteLine("  template-volume   Reuses a known-good template slice and emits a freeblock water layer/volume.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  UnderwaterMapConverter make-underwater-map \"C:\\Maps\\Winter 2026 - 03.Map.Gbx\" \"Underwater\" --variant both --coverage full-stack");
        Console.WriteLine("  UnderwaterMapConverter convert \"C:\\Maps\\Input.Map.Gbx\"");
        Console.WriteLine("  UnderwaterMapConverter convert \"C:\\Maps\\Input.Map.Gbx\" --method vista-flood --set-deco-offset");
        Console.WriteLine("  UnderwaterMapConverter convert \"C:\\Maps\\Input.Map.Gbx\" --method template-volume --template-map \"C:\\Maps\\Template.Map.Gbx\" --min-world-y 168 --max-world-y 168");
        Console.WriteLine("  UnderwaterMapConverter testing inspect-prefab \"C:\\Data\\SomePrefab.Gbx\"");
        Console.WriteLine("  UnderwaterMapConverter extrude-template-volume \"C:\\Maps\\Example.Map.Gbx\" --template-map \"C:\\Maps\\Template.Map.Gbx\" --placement-mode uniform-sheet --shells 1 --overscan-blocks 5 --rotate-quarter-turns 1 --vertical-step-world 4");
        Console.WriteLine("  UnderwaterMapConverter extrude-template-volume \"C:\\Maps\\Example.Map.Gbx\" --emit-prototype-map \"C:\\Maps\\Prototype.Map.Gbx\" --emit-prototype-filter \"Codex_MinimalWaterWrappers\"");
        Console.WriteLine("  UnderwaterMapConverter flood-vista \"C:\\Maps\\MyVista.Map.Gbx\"");
        Console.WriteLine("  UnderwaterMapConverter place-water-carrier-lattice \"C:\\Maps\\example.Map.Gbx\"");
        Console.WriteLine("  UnderwaterMapConverter fill-stadium-water \"C:\\Maps\\MyStadium.Map.Gbx\" --min-world-y 0 --max-world-y 168 --write");
        Console.WriteLine("  UnderwaterMapConverter fill-stadium-water-macroblock \"C:\\Maps\\MyStadium.Map.Gbx\" --macroblock-template \"C:\\Blocks\\macroblock_water_3x3.Macroblock.Gbx\" --min-world-y 0 --max-world-y 168 --write");
        Console.WriteLine("  UnderwaterMapConverter stamp-macroblock \"C:\\Maps\\Empty.Map.Gbx\" --macroblock-template \"C:\\Blocks\\macroblock_water_3x3.Macroblock.Gbx\" --write");
        Console.WriteLine("  UnderwaterMapConverter diff-blocks \"C:\\Maps\\SavedByEditor.Map.Gbx\" \"C:\\Maps\\Generated.Map.Gbx\" --filter DecoWallWaterBase --limit 10");
        Console.WriteLine("  UnderwaterMapConverter diff-baked-blocks \"C:\\Maps\\SavedByEditor.Map.Gbx\" \"C:\\Maps\\Generated.Map.Gbx\" --filter DecoWallWaterVFC --limit 10");
        Console.WriteLine("  UnderwaterMapConverter inspect-baked-blocks \"C:\\Maps\\Generated.Map.Gbx\" \"DecoWallWaterFCT\" 5");
        return string.IsNullOrWhiteSpace(error) ? 0 : 1;
    }
}
