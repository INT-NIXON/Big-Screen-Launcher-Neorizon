using System.Diagnostics;
using System.IO;

namespace PCL.Core.Game.Xbox;

public static class XboxGameLauncher
{
    public static void Launch(string gameId)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"shell:AppsFolder\\{gameId}!Game",
            UseShellExecute = false
        });
    }
}
