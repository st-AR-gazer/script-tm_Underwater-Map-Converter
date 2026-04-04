using System.IO.Compression;
using GBX.NET.Engines.Game;

namespace UnderwaterMapConverter.Infrastructure;

internal static class CustomBlockEmbedding
{
    private static readonly string TrackmaniaBlocksRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Trackmania2020",
        "Blocks");

    public static int EmbedReferencedCustomBlocks(CGameCtnChallenge map)
    {
        var assets = CollectCustomBlockAssets(map).ToList();
        if (assets.Count == 0)
        {
            return 0;
        }

        map.UpdateEmbeddedZipData(zip =>
        {
            foreach (var asset in assets)
            {
                zip.GetEntry(asset.EmbeddedEntryPath)?.Delete();
                zip.CreateEntryFromFile(asset.SourceFilePath, asset.EmbeddedEntryPath, CompressionLevel.Optimal);
            }
        });

        return assets.Count;
    }

    private static IEnumerable<CustomBlockAsset> CollectCustomBlockAssets(CGameCtnChallenge map)
    {
        var seenEntryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var block in map.Blocks ?? [])
        {
            if (!TryResolveAsset(block.BlockModel.Id, out var asset))
            {
                continue;
            }

            if (seenEntryPaths.Add(asset.EmbeddedEntryPath))
            {
                yield return asset;
            }
        }
    }

    private static bool TryResolveAsset(string? rawId, out CustomBlockAsset asset)
    {
        asset = default;

        if (string.IsNullOrWhiteSpace(rawId) || !rawId.EndsWith(".Block.Gbx_CustomBlock", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var relativeBlockPath = rawId
            .Replace(".Block.Gbx_CustomBlock", ".Block.Gbx", StringComparison.OrdinalIgnoreCase)
            .Replace('/', '\\')
            .TrimStart('\\');

        var sourceFilePath = ResolveSourceFilePath(relativeBlockPath);
        if (sourceFilePath is null)
        {
            throw new FileNotFoundException($"Could not resolve custom block source for '{rawId}'.", relativeBlockPath);
        }

        var embeddedEntryPath = Path.Combine(TrackmaniaBlocksRoot, relativeBlockPath).Replace('\\', '/');
        asset = new CustomBlockAsset(sourceFilePath, embeddedEntryPath);
        return true;
    }

    private static string? ResolveSourceFilePath(string relativeBlockPath)
    {
        var fileName = Path.GetFileName(relativeBlockPath);
        var currentDirectory = Environment.CurrentDirectory;
        var currentDirectoryParent = Directory.GetParent(currentDirectory)?.FullName;
        var baseDirectory = AppContext.BaseDirectory;

        var candidates = new[]
        {
            Path.Combine(TrackmaniaBlocksRoot, relativeBlockPath),
            Path.Combine(currentDirectory, relativeBlockPath),
            Path.Combine(currentDirectory, fileName),
            currentDirectoryParent is null ? null : Path.Combine(currentDirectoryParent, relativeBlockPath),
            currentDirectoryParent is null ? null : Path.Combine(currentDirectoryParent, fileName),
            Path.Combine(baseDirectory, relativeBlockPath),
            Path.Combine(baseDirectory, fileName),
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

    private readonly record struct CustomBlockAsset(string SourceFilePath, string EmbeddedEntryPath);
}
