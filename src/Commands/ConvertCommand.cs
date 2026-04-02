namespace UnderwaterMapConverter.Commands;

internal static class ConvertCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            return CliHelp.Write("Missing input map path for convert.");
        }

        if (!args.Contains("--method", StringComparer.OrdinalIgnoreCase) && LooksLikeMakeUnderwaterMapInvocation(args))
        {
            return MakeUnderwaterMapCommand.Run(args);
        }

        var method = "carrier-lattice";
        var forwardedArgs = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            if (arg.Equals("--method", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length)
                {
                    return CliHelp.Write("Missing value after --method.");
                }

                method = args[index + 1];
                index++;
                continue;
            }

            forwardedArgs.Add(arg);
        }

        return method.Trim().ToLowerInvariant() switch
        {
            "carrier-lattice" or "lattice" => PlaceWaterCarrierLatticeCommand.Run(forwardedArgs.ToArray()),
            "vista-flood" or "flood-vista" => FloodVistaCommand.Run(forwardedArgs.ToArray()),
            "template-volume" or "volume" => ExtrudeTemplateVolumeCommand.Run(EnsureWrite(forwardedArgs)),
            _ => CliHelp.Write($"Unknown convert method '{method}'.")
        };
    }

    private static bool LooksLikeMakeUnderwaterMapInvocation(string[] args)
    {
        if (args.Length < 2)
        {
            return false;
        }

        if (args[1].StartsWith("--", StringComparison.Ordinal))
        {
            return false;
        }

        if (args.Contains("--variant", StringComparer.OrdinalIgnoreCase)
            || args.Contains("--coverage", StringComparer.OrdinalIgnoreCase)
            || args.Contains("--overscan-blocks", StringComparer.OrdinalIgnoreCase)
            || args.Contains("--rotate-quarter-turns", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return !LooksLikeOutputPath(args[1]);
    }

    private static bool LooksLikeOutputPath(string value)
    {
        if (value.EndsWith(".gbx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Path.IsPathRooted(value))
        {
            return true;
        }

        return value.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0;
    }

    private static string[] EnsureWrite(List<string> args)
    {
        if (!args.Contains("--write", StringComparer.OrdinalIgnoreCase))
        {
            args.Add("--write");
        }

        return args.ToArray();
    }
}
