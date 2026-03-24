using UnderwaterMapConverter;
using UnderwaterMapConverter.Commands;
using UnderwaterMapConverter.Infrastructure;

GbxBootstrap.Initialize();

if (args.Length == 0)
{
    CliHelp.Write();
    return 1;
}

var command = args[0];
var commandArgs = args.Skip(1).ToArray();

try
{
    return command switch
    {
        "make-underwater-map" => MakeUnderwaterMapCommand.Run(commandArgs),
        "convert" => ConvertCommand.Run(commandArgs),
        "testing" => TestingCommand.Run(commandArgs),
        "extrude-template-volume" => ExtrudeTemplateVolumeCommand.Run(commandArgs),
        "flood-vista" => FloodVistaCommand.Run(commandArgs),
        "place-water-carrier-lattice" => PlaceWaterCarrierLatticeCommand.Run(commandArgs),
        "fill-stadium-water" => FillStadiumWaterCommand.Run(commandArgs),
        "diff-baked-blocks" => DiffBakedBlocksCommand.Run(commandArgs),
        "diff-blocks" => DiffBlocksCommand.Run(commandArgs),
        "inspect-baked-blocks" => InspectBakedBlocksCommand.Run(commandArgs),
        "fill-stadium-water-macroblock" => FillStadiumWaterFromMacroblockCommand.Run(commandArgs),
        "stamp-macroblock" => StampMacroblockCommand.Run(commandArgs),
        "help" or "--help" or "-h" => CliHelp.Write(),
        _ => CliHelp.Write($"Unknown command '{command}'.")
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
