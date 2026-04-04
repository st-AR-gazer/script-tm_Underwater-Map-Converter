using System.Reflection;

namespace UnderwaterMapConverter.Infrastructure;

internal static class BundledAssetResolver
{
    private const string AssetsDirectoryName = "assets";
    private const string BlocksDirectoryName = "blocks";
    private const string MapsDirectoryName = "maps";
    private const string ResourcePrefix = "BundledAssets";

    public static string ResolveBlockPath(string fileName)
        => ResolveAssetPath(BlocksDirectoryName, fileName);

    public static string ResolveMapPath(string fileName)
        => ResolveAssetPath(MapsDirectoryName, fileName);

    private static string ResolveAssetPath(string assetKindDirectoryName, string fileName)
    {
        var onDiskPath = ResolveOnDiskAssetPath(assetKindDirectoryName, fileName);
        if (onDiskPath is not null)
        {
            return onDiskPath;
        }

        return ExtractEmbeddedAsset(assetKindDirectoryName, fileName);
    }

    private static string? ResolveOnDiskAssetPath(string assetKindDirectoryName, string fileName)
    {
        var currentDirectory = Environment.CurrentDirectory;
        var currentDirectoryParent = Directory.GetParent(currentDirectory)?.FullName;
        var baseDirectory = AppContext.BaseDirectory;

        var candidates = new[]
        {
            Path.Combine(currentDirectory, AssetsDirectoryName, assetKindDirectoryName, fileName),
            currentDirectoryParent is null ? null : Path.Combine(currentDirectoryParent, AssetsDirectoryName, assetKindDirectoryName, fileName),
            Path.Combine(baseDirectory, AssetsDirectoryName, assetKindDirectoryName, fileName),
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string ExtractEmbeddedAsset(string assetKindDirectoryName, string fileName)
    {
        var assembly = typeof(BundledAssetResolver).Assembly;
        var resourceName = $"{ResourcePrefix}.{assetKindDirectoryName}.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new FileNotFoundException(
                $"Could not resolve bundled asset '{fileName}' in '{assetKindDirectoryName}'.",
                resourceName);
        }

        var extractedPath = Path.Combine(GetExtractionRoot(), assetKindDirectoryName, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(extractedPath)!);

        if (!File.Exists(extractedPath))
        {
            using var file = File.Create(extractedPath);
            stream.CopyTo(file);
        }

        return extractedPath;
    }

    private static string GetExtractionRoot()
    {
        var stamp = typeof(BundledAssetResolver).Assembly.ManifestModule.ModuleVersionId.ToString("N");

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UnderwaterMapConverter",
            "bundled-assets",
            stamp);
    }
}
