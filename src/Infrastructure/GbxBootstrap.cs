using GBX.NET;
using GBX.NET.LZO;
using GBX.NET.ZLib;

namespace UnderwaterMapConverter.Infrastructure;

internal static class GbxBootstrap
{
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        Gbx.LZO = new Lzo();
        Gbx.ZLib = new ZLib();
        initialized = true;
    }
}
