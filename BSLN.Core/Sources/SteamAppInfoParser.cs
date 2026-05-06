namespace BSLN.Core.Sources;

public static class SteamAppInfoParser
{
    private const int SearchWindowSize = 4096;

    public static bool IsGameApp(byte[] appInfoBytes, uint steamAppId, string expectedName)
    {
        if (string.IsNullOrWhiteSpace(expectedName))
        {
            return false;
        }

        var needle = BitConverter.GetBytes(steamAppId);
        for (var i = 0; i <= appInfoBytes.Length - needle.Length; i++)
        {
            if (!BytesMatch(appInfoBytes, i, needle))
            {
                continue;
            }

            var windowEnd = Math.Min(i + SearchWindowSize, appInfoBytes.Length);
            var window = appInfoBytes.AsSpan(i..windowEnd);

            if (!WindowContainsName(window, expectedName))
            {
                continue;
            }

            var appType = ParseAppTypeToken(window);
            if (appType is not null)
            {
                return appType == "game";
            }
        }

        return false;
    }

    public static byte[]? LoadAppInfoBytes(string steamDirectory)
    {
        var appInfoPath = Path.Combine(steamDirectory, "appcache", "appinfo.vdf");
        if (!File.Exists(appInfoPath))
        {
            return null;
        }

        try
        {
            var bytes = File.ReadAllBytes(appInfoPath);
            return bytes.Length > 0 ? bytes : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool BytesMatch(byte[] data, int offset, byte[] needle)
    {
        for (var i = 0; i < needle.Length; i++)
        {
            if (data[offset + i] != needle[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool WindowContainsName(ReadOnlySpan<byte> window, string expectedName)
    {
        if (expectedName.Length == 0)
        {
            return false;
        }

        // Convert window bytes to string for searching
        // The binary VDF contains null-terminated strings
        var printableChars = new char[window.Length];
        var charCount = 0;

        foreach (var b in window)
        {
            if (b is >= 0x20 and <= 0x7e)
            {
                printableChars[charCount++] = (char)b;
            }
            else
            {
                printableChars[charCount++] = ' ';
            }
        }

        var haystack = new string(printableChars, 0, charCount);
        return haystack.Contains(expectedName, StringComparison.Ordinal);
    }

    private static string? ParseAppTypeToken(ReadOnlySpan<byte> window)
    {
        var start = -1;

        for (var i = 0; i < window.Length; i++)
        {
            var b = window[i];
            var isPrintable = b is >= 0x20 and <= 0x7e;

            if (isPrintable)
            {
                if (start < 0)
                {
                    start = i;
                }

                continue;
            }

            if (start >= 0)
            {
                var token = GetAsciiString(window[start..i]);
                start = -1;

                var result = ClassifyToken(token);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        if (start >= 0)
        {
            var token = GetAsciiString(window[start..]);
            return ClassifyToken(token);
        }

        return null;
    }

    private static string? ClassifyToken(string token)
    {
        return token.ToUpperInvariant() switch
        {
            "GAME" => "game",
            "APPLICATION" => "application",
            "TOOL" => "tool",
            "DEMO" => "demo",
            "DLC" => "dlc",
            "VIDEO" => "video",
            "MUSIC" => "music",
            _ => null,
        };
    }

    private static string GetAsciiString(ReadOnlySpan<byte> bytes)
    {
        var chars = new char[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i] = (char)bytes[i];
        }

        return new string(chars);
    }
}
