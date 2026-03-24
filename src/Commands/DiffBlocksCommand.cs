using System.Globalization;
using System.Reflection;
using System.Text.Json;
using GBX.NET.Engines.Game;
using UnderwaterMapConverter.Infrastructure;

namespace UnderwaterMapConverter.Commands;

internal static class DiffBlocksCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            return CliHelp.Write("Usage: diff-blocks <referenceMapPath> <actualMapPath> [--filter TEXT] [--limit N] [--write-json PATH]");
        }

        var optionArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var positionalArgs = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                positionalArgs.Add(arg);
                continue;
            }

            if (index + 1 >= args.Length)
            {
                return CliHelp.Write($"Missing value after {arg}.");
            }

            optionArgs[arg] = args[index + 1];
            index++;
        }

        var referenceMapPath = Path.GetFullPath(positionalArgs[0]);
        var actualMapPath = Path.GetFullPath(positionalArgs[1]);

        if (!File.Exists(referenceMapPath))
        {
            throw new FileNotFoundException($"Reference map not found: {referenceMapPath}");
        }

        if (!File.Exists(actualMapPath))
        {
            throw new FileNotFoundException($"Actual map not found: {actualMapPath}");
        }

        var filter = optionArgs.TryGetValue("--filter", out var filterRaw)
            ? filterRaw.Trim()
            : null;
        var sampleLimit = optionArgs.TryGetValue("--limit", out var limitRaw)
            ? int.Parse(limitRaw, CultureInfo.InvariantCulture)
            : 25;
        var writeJsonPath = optionArgs.TryGetValue("--write-json", out var writeJsonRaw)
            ? Path.GetFullPath(writeJsonRaw)
            : null;

        if (sampleLimit < 0)
        {
            throw new InvalidOperationException("--limit must be >= 0.");
        }

        var referenceGbx = GbxIo.ParseChallengeBestEffort(referenceMapPath);
        var referenceMap = referenceGbx.Node ?? throw new InvalidOperationException("Reference map node is null.");

        var actualGbx = GbxIo.ParseChallengeBestEffort(actualMapPath);
        var actualMap = actualGbx.Node ?? throw new InvalidOperationException("Actual map node is null.");

        var referenceBlocks = FilterBlocks(referenceMap.Blocks ?? [], filter);
        var actualBlocks = FilterBlocks(actualMap.Blocks ?? [], filter);

        var referenceIndex = IndexBlocks(referenceBlocks);
        var actualIndex = IndexBlocks(actualBlocks);

        var referenceDuplicateKeys = referenceIndex.Count(static entry => entry.Value.Count > 1);
        var referenceDuplicateCount = referenceIndex.Sum(static entry => Math.Max(0, entry.Value.Count - 1));
        var actualDuplicateKeys = actualIndex.Count(static entry => entry.Value.Count > 1);
        var actualDuplicateCount = actualIndex.Sum(static entry => Math.Max(0, entry.Value.Count - 1));

        var allKeys = new HashSet<string>(referenceIndex.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(actualIndex.Keys);

        var missingKeys = new List<string>();
        var extraKeys = new List<string>();
        var countMismatchedKeys = 0;

        var comparedPairCount = 0;
        var mismatchedPairCount = 0;

        var propertyMismatchCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var samples = new List<object>();

        foreach (var key in allKeys.OrderBy(static k => k, StringComparer.OrdinalIgnoreCase))
        {
            var hasReference = referenceIndex.TryGetValue(key, out var referenceList);
            var hasActual = actualIndex.TryGetValue(key, out var actualList);

            if (!hasReference)
            {
                extraKeys.Add(key);
                continue;
            }

            if (!hasActual)
            {
                missingKeys.Add(key);
                continue;
            }

            referenceList ??= [];
            actualList ??= [];

            if (referenceList.Count != actualList.Count)
            {
                countMismatchedKeys += 1;
            }

            var pairCount = Math.Min(referenceList.Count, actualList.Count);

            for (var index = 0; index < pairCount; index++)
            {
                comparedPairCount += 1;
                var referenceBlock = referenceList[index];
                var actualBlock = actualList[index];

                var referenceSnapshot = SnapshotBlock(referenceBlock);
                var actualSnapshot = SnapshotBlock(actualBlock);
                var diffs = CompareSnapshots(referenceSnapshot, actualSnapshot);

                if (diffs.Count == 0)
                {
                    continue;
                }

                mismatchedPairCount += 1;

                foreach (var diff in diffs)
                {
                    propertyMismatchCounts.TryGetValue(diff.Path, out var existing);
                    propertyMismatchCounts[diff.Path] = existing + 1;
                }

                if (samples.Count < sampleLimit)
                {
                    samples.Add(new
                    {
                        key,
                        reference = BuildBlockSummary(referenceBlock),
                        actual = BuildBlockSummary(actualBlock),
                        diffs,
                    });
                }
            }
        }

        var missingTypeCounts = CountTypes(missingKeys);
        var extraTypeCounts = CountTypes(extraKeys);
        var sortedPropertyMismatchCounts = propertyMismatchCounts
            .OrderByDescending(static kvp => kvp.Value)
            .ThenBy(static kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        var missingKeysSample = missingKeys
            .Take(200)
            .ToArray();
        var extraKeysSample = extraKeys
            .Take(200)
            .ToArray();

        var report = new
        {
            referenceMapPath,
            actualMapPath,
            filter,
            referenceBlockCount = referenceMap.Blocks?.Count ?? 0,
            actualBlockCount = actualMap.Blocks?.Count ?? 0,
            referenceHasGhostBlocks = referenceMap.HasGhostBlocks,
            actualHasGhostBlocks = actualMap.HasGhostBlocks,
            referenceMatchedCount = referenceBlocks.Count,
            actualMatchedCount = actualBlocks.Count,
            referenceUniqueKeyCount = referenceIndex.Count,
            actualUniqueKeyCount = actualIndex.Count,
            referenceDuplicateKeys,
            referenceDuplicateCount,
            actualDuplicateKeys,
            actualDuplicateCount,
            comparedPairCount,
            missingKeyCount = missingKeys.Count,
            missingKeysSample,
            missingKeysTruncated = missingKeys.Count > missingKeysSample.Length,
            extraKeyCount = extraKeys.Count,
            extraKeysSample,
            extraKeysTruncated = extraKeys.Count > extraKeysSample.Length,
            countMismatchedKeyCount = countMismatchedKeys,
            mismatchedPairCount,
            missingTypeCounts = missingTypeCounts
                .OrderByDescending(static kvp => kvp.Value)
                .ThenBy(static kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .Take(30)
                .ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
            extraTypeCounts = extraTypeCounts
                .OrderByDescending(static kvp => kvp.Value)
                .ThenBy(static kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .Take(30)
                .ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
            propertyMismatchCounts = sortedPropertyMismatchCounts,
            samplesLimit = sampleLimit,
            sampleCount = samples.Count,
            samples,
            note = "Keys are grouped by (typeId|coord|direction|variant|subVariant). For large diffs, use --filter to narrow to a single block family (e.g. DecoWallWaterBase).",
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        var json = JsonSerializer.Serialize(report, jsonOptions);

        if (!string.IsNullOrWhiteSpace(writeJsonPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(writeJsonPath)!);
            File.WriteAllText(writeJsonPath, json);
        }

        Console.WriteLine(json);
        return 0;
    }

    private static List<CGameCtnBlock> FilterBlocks(IReadOnlyCollection<CGameCtnBlock> blocks, string? filter)
    {
        if (blocks.Count == 0)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(filter))
        {
            return blocks.ToList();
        }

        var normalizedFilter = filter.Trim();

        var exactMatch = blocks.Any(block =>
            string.Equals(ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id), normalizedFilter, StringComparison.OrdinalIgnoreCase));

        return blocks
            .Where(block =>
            {
                var typeId = ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id);
                return exactMatch
                    ? string.Equals(typeId, normalizedFilter, StringComparison.OrdinalIgnoreCase)
                    : typeId.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();
    }

    private static Dictionary<string, List<CGameCtnBlock>> IndexBlocks(IEnumerable<CGameCtnBlock> blocks)
    {
        var result = new Dictionary<string, List<CGameCtnBlock>>(StringComparer.OrdinalIgnoreCase);

        foreach (var block in blocks)
        {
            var key = BuildKey(block);
            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(block);
        }

        return result;
    }

    private static string BuildKey(CGameCtnBlock block)
    {
        var typeId = ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id);
        var coord = block.Coord;
        return string.Create(CultureInfo.InvariantCulture, $"{typeId}|{coord.X},{coord.Y},{coord.Z}|{block.Direction}|{block.Variant}|{block.SubVariant}");
    }

    private static Dictionary<string, int> CountTypes(IEnumerable<string> keys)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in keys)
        {
            var typeId = key.Split('|', 2, StringSplitOptions.TrimEntries)[0];
            result.TryGetValue(typeId, out var existing);
            result[typeId] = existing + 1;
        }

        return result;
    }

    private static object BuildBlockSummary(CGameCtnBlock block) => new
    {
        name = block.Name,
        typeId = ReflectionAccess.NormalizeTypeIdValue(block.BlockModel.Id),
        coord = new { x = block.Coord.X, y = block.Coord.Y, z = block.Coord.Z },
        direction = block.Direction.ToString(),
        variant = block.Variant,
        subVariant = block.SubVariant,
        flags = block.Flags,
        isGround = block.IsGround,
        isClip = block.IsClip,
        isGhost = block.IsGhost,
        isFree = block.IsFree,
    };

    private static Dictionary<string, SnapshotValue> SnapshotBlock(CGameCtnBlock block)
    {
        var snapshot = new Dictionary<string, SnapshotValue>(StringComparer.OrdinalIgnoreCase);

        snapshot["Name"] = new SnapshotValue(typeof(string).FullName ?? "System.String", block.Name ?? string.Empty);

        var ident = block.BlockModel;
        snapshot["BlockModel.Id"] = new SnapshotValue(typeof(string).FullName ?? "System.String", ident.Id ?? string.Empty);
        snapshot["BlockModel.Author"] = new SnapshotValue(typeof(string).FullName ?? "System.String", ident.Author ?? string.Empty);
        snapshot["BlockModel.Collection.Number"] = new SnapshotValue(
            typeof(int?).FullName ?? "System.Nullable`1[System.Int32]",
            ident.Collection.Number?.ToString(CultureInfo.InvariantCulture) ?? "<null>");
        snapshot["BlockModel.Collection.String"] = new SnapshotValue(
            typeof(string).FullName ?? "System.String",
            ident.Collection.String ?? "<null>");

        snapshot["Coord.X"] = new SnapshotValue(typeof(int).FullName ?? "System.Int32", block.Coord.X.ToString(CultureInfo.InvariantCulture));
        snapshot["Coord.Y"] = new SnapshotValue(typeof(int).FullName ?? "System.Int32", block.Coord.Y.ToString(CultureInfo.InvariantCulture));
        snapshot["Coord.Z"] = new SnapshotValue(typeof(int).FullName ?? "System.Int32", block.Coord.Z.ToString(CultureInfo.InvariantCulture));

        foreach (var property in GetBlockProperties())
        {
            if (property.Name is "BlockModel" or "Coord" or "Name")
            {
                continue;
            }

            string typeName;
            string valueString;

            try
            {
                var value = property.GetValue(block);
                typeName = property.PropertyType.FullName ?? property.PropertyType.Name;
                valueString = FormatValue(value);
            }
            catch (Exception ex)
            {
                typeName = property.PropertyType.FullName ?? property.PropertyType.Name;
                valueString = $"<error: {ex.GetType().Name}: {ex.Message}>";
            }

            snapshot[property.Name] = new SnapshotValue(typeName, valueString);
        }

        return snapshot;
    }

    private static IReadOnlyList<PropertyInfo> GetBlockProperties()
        => CachedBlockProperties.Value;

    private static readonly Lazy<IReadOnlyList<PropertyInfo>> CachedBlockProperties = new(() =>
        typeof(CGameCtnBlock)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead && property.GetIndexParameters().Length == 0)
            .OrderBy(static property => property.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray());

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        return value switch
        {
            string str => str,
            bool boolValue => boolValue ? "True" : "False",
            byte byteValue => byteValue.ToString(CultureInfo.InvariantCulture),
            sbyte sbyteValue => sbyteValue.ToString(CultureInfo.InvariantCulture),
            short shortValue => shortValue.ToString(CultureInfo.InvariantCulture),
            ushort ushortValue => ushortValue.ToString(CultureInfo.InvariantCulture),
            int intValue => intValue.ToString(CultureInfo.InvariantCulture),
            uint uintValue => uintValue.ToString(CultureInfo.InvariantCulture),
            long longValue => longValue.ToString(CultureInfo.InvariantCulture),
            ulong ulongValue => ulongValue.ToString(CultureInfo.InvariantCulture),
            float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
            decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
            Enum enumValue => enumValue.ToString(),
            GBX.NET.Vec3 vec3 => string.Create(CultureInfo.InvariantCulture, $"{vec3.X},{vec3.Y},{vec3.Z}"),
            Array array => $"<Array len={array.Length}>",
            System.Collections.ICollection collection => $"<{value.GetType().Name} count={collection.Count}>",
            _ => value.ToString() ?? value.GetType().FullName ?? value.GetType().Name
        };
    }

    private static List<BlockDiff> CompareSnapshots(
        IReadOnlyDictionary<string, SnapshotValue> reference,
        IReadOnlyDictionary<string, SnapshotValue> actual)
    {
        var diffs = new List<BlockDiff>();

        var keys = new HashSet<string>(reference.Keys, StringComparer.OrdinalIgnoreCase);
        keys.UnionWith(actual.Keys);

        foreach (var key in keys.OrderBy(static k => k, StringComparer.OrdinalIgnoreCase))
        {
            reference.TryGetValue(key, out var refValue);
            actual.TryGetValue(key, out var actualValue);

            var refString = refValue.Value ?? "<missing>";
            var actualString = actualValue.Value ?? "<missing>";

            if (string.Equals(refString, actualString, StringComparison.Ordinal))
            {
                continue;
            }

            diffs.Add(new BlockDiff(key, refString, actualString));
        }

        return diffs;
    }

    private readonly record struct SnapshotValue(string Type, string? Value);

    private readonly record struct BlockDiff(string Path, string Reference, string Actual);
}
