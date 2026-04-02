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

        Exception? firstException = null;

        try
        {
            return Gbx.Parse<CGameCtnChallenge>(path, settings);
        }
        catch (Exception ex)
        {
            firstException = ex;
        }

        try
        {
            return Gbx.Parse<CGameCtnChallenge>(path, settings with { OpenPlanetHookExtractMode = true });
        }
        catch (Exception ex)
        {
            if (!GbxBootstrap.IsLzoNativeAvailable
                && (IsLikelyNativeLzoMissing(firstException) || IsLikelyNativeLzoMissing(ex)))
            {
                throw new InvalidOperationException(
                    $"Failed to parse map GBX. 'liblzo2.dll' could not be loaded. " +
                    $"Make sure 'liblzo2.dll' is next to UnderwaterMapConverter.exe (or run from dist\\win-x64). " +
                    $"Path: {path}",
                    ex);
            }

            throw new InvalidOperationException($"Failed to parse map GBX. Path: {path}", ex);
        }
    }

    private static bool IsLikelyNativeLzoMissing(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }

        if (exception is DllNotFoundException)
        {
            return true;
        }

        if (exception is ArgumentOutOfRangeException argumentOutOfRangeException
            && string.Equals(argumentOutOfRangeException.ParamName, "length", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
