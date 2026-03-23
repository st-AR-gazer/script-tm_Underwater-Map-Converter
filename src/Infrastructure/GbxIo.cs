using GBX.NET;
using GBX.NET.Engines.Game;

namespace UnderwaterMapConverter.Infrastructure;

internal static class GbxIo
{
    public static Gbx<CGameCtnChallenge> ParseChallengeBestEffort(string path)
    {
        var settings = new GbxReadSettings
        {
            IgnoreExceptionsInBody = true,
            SafeSkippableChunks = true,
        };

        try
        {
            return Gbx.Parse<CGameCtnChallenge>(path, settings);
        }
        catch
        {
            return Gbx.Parse<CGameCtnChallenge>(path, settings with { OpenPlanetHookExtractMode = true });
        }
    }
}
