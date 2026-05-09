using System.Diagnostics;

namespace PCL.Core.Game.Steam;

public static class SteamGameLauncher
{
    private const string SteamRunPrefix = "steam://rungameid/";

    public static void Launch(string appId)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"{SteamRunPrefix}{appId}",
            UseShellExecute = true
        });
    }
}
