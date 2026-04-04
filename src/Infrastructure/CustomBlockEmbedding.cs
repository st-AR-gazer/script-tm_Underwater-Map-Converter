using System.Collections.Immutable;
using System.IO.Compression;
using System.Reflection;
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;

namespace UnderwaterMapConverter.Infrastructure;

internal static class CustomBlockEmbedding
{
    public static int EmbedReferencedCustomBlocks(CGameCtnChallenge map)
    {
        var assets = CollectCustomBlockAssets(map).ToList();
        if (assets.Count == 0)
        {
            SetExpectedEmbeddedItemModels(map, ImmutableList<Ident>.Empty);
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

        SetExpectedEmbeddedItemModels(
            map,
            assets
                .Select(asset => asset.ExpectedEmbeddedItemModel)
                .Distinct()
                .ToImmutableList());

        return assets.Count;
    }

    public static string ResolveCustomBlockAuthor(string? rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId) || !rawId.EndsWith(".Block.Gbx_CustomBlock", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var relativeBlockPath = rawId
            .Replace(".Block.Gbx_CustomBlock", ".Block.Gbx", StringComparison.OrdinalIgnoreCase)
            .Replace('/', '\\')
            .TrimStart('\\');

        var sourceFilePath = BundledAssetResolver.ResolveBlockPath(Path.GetFileName(relativeBlockPath));
        return sourceFilePath is null ? string.Empty : ReadWrapperAuthor(sourceFilePath);
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

        var sourceFilePath = BundledAssetResolver.ResolveBlockPath(Path.GetFileName(relativeBlockPath));

        var expectedEmbeddedItemModel = BuildExpectedEmbeddedItemModel(relativeBlockPath, sourceFilePath);
        var embeddedEntryPath = Path.Combine("Blocks", relativeBlockPath).Replace('\\', '/');
        asset = new CustomBlockAsset(sourceFilePath, embeddedEntryPath, expectedEmbeddedItemModel);
        return true;
    }

    private static Ident BuildExpectedEmbeddedItemModel(string relativeBlockPath, string sourceFilePath)
    {
        var environment = ExtractEnvironment(relativeBlockPath);
        var author = ReadWrapperAuthor(sourceFilePath);
        return new Ident(relativeBlockPath, environment, author);
    }

    private static string ExtractEnvironment(string relativeBlockPath)
    {
        var segments = relativeBlockPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : string.Empty;
    }

    private static string ReadWrapperAuthor(string sourceFilePath)
    {
        var settings = new GbxReadSettings
        {
            IgnoreExceptionsInBody = true,
            SafeSkippableChunks = true,
        };

        try
        {
            return Gbx.Parse<CGameItemModel>(sourceFilePath, settings).Node?.Ident.Author ?? string.Empty;
        }
        catch
        {
            return Gbx.Parse<CGameItemModel>(sourceFilePath, settings with { OpenPlanetHookExtractMode = true }).Node?.Ident.Author ?? string.Empty;
        }
    }

    private static void SetExpectedEmbeddedItemModels(CGameCtnChallenge map, ImmutableList<Ident> value)
    {
        var property = map.GetType().GetProperty(
            "ExpectedEmbeddedItemModels",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var setter = property?.GetSetMethod(nonPublic: true);
        if (setter is not null)
        {
            setter.Invoke(map, [value]);
            return;
        }

        var backingField = map.GetType().GetField(
            "<ExpectedEmbeddedItemModels>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (backingField is not null)
        {
            backingField.SetValue(map, value);
            return;
        }

        throw new InvalidOperationException("Could not update ExpectedEmbeddedItemModels on CGameCtnChallenge.");
    }
    private readonly record struct CustomBlockAsset(string SourceFilePath, string EmbeddedEntryPath, Ident ExpectedEmbeddedItemModel);
}
