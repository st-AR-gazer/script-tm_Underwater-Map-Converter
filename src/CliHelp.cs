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
        return string.IsNullOrWhiteSpace(error) ? 0 : 1;
    }
}
