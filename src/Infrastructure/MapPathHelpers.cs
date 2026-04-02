namespace UnderwaterMapConverter.Infrastructure;

internal static class MapPathHelpers
{
    public static string FormatParenthesizedSuffix(string suffix)
    {
        var trimmed = suffix.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
        {
            return trimmed;
        }

        return $"({trimmed})";
    }

    public static string BuildDefaultOutputPath(string inputMapPath, string suffix)
    {
        var directory = Path.GetDirectoryName(inputMapPath) ?? Environment.CurrentDirectory;
        return Path.Combine(directory, BuildSuffixedFileName(Path.GetFileName(inputMapPath), suffix));
    }

    public static string BuildSuffixedOutputPath(string inputMapPath, string suffix)
        => Path.Combine(Path.GetDirectoryName(inputMapPath) ?? Environment.CurrentDirectory, BuildSuffixedFileName(Path.GetFileName(inputMapPath), suffix));

    private static string BuildSuffixedFileName(string fileName, string suffix)
    {
        var formattedSuffix = FormatParenthesizedSuffix(suffix);
        var mapExtensionIndex = fileName.LastIndexOf(".Map", StringComparison.OrdinalIgnoreCase);

        if (mapExtensionIndex >= 0)
        {
            var stem = fileName[..mapExtensionIndex];
            var mapTail = fileName[mapExtensionIndex..];
            return $"{stem} {formattedSuffix}{mapTail}";
        }

        var extension = Path.GetExtension(fileName);
        var stemWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return string.IsNullOrEmpty(extension)
            ? $"{stemWithoutExtension} {formattedSuffix}"
            : $"{stemWithoutExtension} {formattedSuffix}{extension}";
    }
}
