using System.Runtime.InteropServices;
using GBX.NET;
using GBX.NET.LZO;
using GBX.NET.ZLib;

namespace UnderwaterMapConverter.Infrastructure;

internal static class GbxBootstrap
{
    private static bool initialized;
    private static nint lzoHandle;

    public static bool IsLzoNativeAvailable { get; private set; } = true;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        IsLzoNativeAvailable = TryLoadLzoNative();
        Gbx.LZO = new Lzo();
        Gbx.ZLib = new ZLib();
        initialized = true;
    }

    private static bool TryLoadLzoNative()
    {
        if (NativeLibrary.TryLoad("liblzo2", out lzoHandle))
        {
            return true;
        }

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "liblzo2.dll"),
            Path.Combine(baseDirectory, "dist", "win-x64", "liblzo2.dll"),
        };

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                lzoHandle = NativeLibrary.Load(candidate);
                return true;
            }
            catch
            {
                // Fall through and try the next candidate.
            }
        }

        return false;
    }
}
