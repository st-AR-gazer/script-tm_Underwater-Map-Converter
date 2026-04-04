using System.Reflection;
using GBX.NET;

namespace UnderwaterMapConverter.Infrastructure;

internal static class ReflectionAccess
{

    public static string? ReadStringProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanRead)
        {
            return null;
        }

        return property.GetValue(instance) as string;
    }

    public static int ReadInt32Property(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanRead)
        {
            return 0;
        }

        var value = property.GetValue(instance);
        return value switch
        {
            int direct => direct,
            IConvertible convertible => convertible.ToInt32(System.Globalization.CultureInfo.InvariantCulture),
            _ => 0,
        };
    }

    public static TEnum ReadEnumProperty<TEnum>(object instance, string propertyName) where TEnum : struct
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanRead)
        {
            return default;
        }

        var value = property.GetValue(instance);
        return value is TEnum typed ? typed : default;
    }

    public static IReadOnlyList<string> ReadStringArrayProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanRead)
        {
            return Array.Empty<string>();
        }

        var value = property.GetValue(instance);
        if (value is string[] strings)
        {
            return strings;
        }

        if (value is IEnumerable<string> enumerable)
        {
            return enumerable.ToArray();
        }

        return Array.Empty<string>();
    }

    public static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
        {
            throw new InvalidOperationException($"Property '{propertyName}' is not writable on {instance.GetType().FullName}.");
        }

        property.SetValue(instance, value);
    }

    public static string NormalizeTypeIdValue(string rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId))
        {
            return "Unknown";
        }

        return rawId
            .Replace(".Block.Gbx_CustomBlock", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".Block.Gbx", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".Item.Gbx", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace('/', '\\')
            .Trim();
    }
}
