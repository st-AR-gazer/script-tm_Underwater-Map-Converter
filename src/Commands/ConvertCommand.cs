namespace UnderwaterMapConverter.Commands;

internal static class ConvertCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            return CliHelp.Write("Missing input map path for convert.");
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

    private static string[] EnsureWrite(List<string> args)
    {
        if (!args.Contains("--write", StringComparer.OrdinalIgnoreCase))
        {
            args.Add("--write");
        }

        return args.ToArray();
    }
}
