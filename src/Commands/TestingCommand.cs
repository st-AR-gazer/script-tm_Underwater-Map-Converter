using System.Diagnostics;

namespace UnderwaterMapConverter.Commands;

internal static class TestingCommand
{
    private const string LegacyMapViewerRoot = @"C:\Users\ar\Desktop\trackmania\misc\map_viewer";

    public static int Run(string[] args)
    {
        var legacyExePath = ResolveLegacyExecutablePath();
        if (legacyExePath is null)
        {
            Console.Error.WriteLine("Could not find a usable MapViewer.Testing executable.");
            Console.Error.WriteLine($"Checked under: {LegacyMapViewerRoot}");
            Console.Error.WriteLine("Try building the old tool once, or update the bridge path in TestingCommand.");
            return 1;
        }

        if (args.Length == 0)
        {
            Console.WriteLine("Testing bridge");
            Console.WriteLine();
            Console.WriteLine("This forwards commands to the original MapViewer.Testing environment.");
            Console.WriteLine($"Legacy root: {LegacyMapViewerRoot}");
            Console.WriteLine($"Executable: {legacyExePath}");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine(@"  UnderwaterMapConverter testing inspect-prefab ""C:\path\to\Prefab.Gbx""");
            Console.WriteLine(@"  UnderwaterMapConverter testing inspect-map-blocks ""C:\path\to\Map.Gbx"" ""RoadIce"" 20");
            Console.WriteLine();
            Console.WriteLine("Docs:");
            Console.WriteLine(@"  .nadeo_re_documentation\testing\README.md");
            return 0;
        }

        var psi = new ProcessStartInfo
        {
            FileName = legacyExePath,
            WorkingDirectory = Path.GetDirectoryName(legacyExePath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi);
        if (process is null)
        {
            Console.Error.WriteLine($"Failed to start legacy testing executable: {legacyExePath}");
            return 1;
        }

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                Console.WriteLine(eventArgs.Data);
            }
        };

        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                Console.Error.WriteLine(eventArgs.Data);
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;
    }

    private static string? ResolveLegacyExecutablePath()
    {
        var candidates = new[]
        {
            Path.Combine(LegacyMapViewerRoot, "backend", "MapViewer.Testing", "bin", "Debug", "net10.0", "MapViewer.Testing.exe"),
            Path.Combine(LegacyMapViewerRoot, "backend", "MapViewer.Testing", "bin", "Release", "net10.0", "MapViewer.Testing.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
