namespace UnderwaterMapConverter.Infrastructure;

internal static class MapPathHelpers
{
    public static string BuildDefaultOutputPath(string inputMapPath, string suffix)
    {
        var directory = Path.GetDirectoryName(inputMapPath) ?? Environment.CurrentDirectory;
        return Path.Combine(directory, $"{BuildMapStem(inputMapPath)} - {suffix}.Map.Gbx");
    }

    public static string BuildSuffixedOutputPath(string inputMapPath, string suffix)
        => Path.Combine(Path.GetDirectoryName(inputMapPath) ?? Environment.CurrentDirectory, $"{BuildMapStem(inputMapPath)} - {suffix}.Map.Gbx");

    private static string BuildMapStem(string inputMapPath)
    {
        var fileName = Path.GetFileName(inputMapPath);
        return fileName.EndsWith(".Map.Gbx", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^8]
            : Path.GetFileNameWithoutExtension(fileName);
    }
}
