using System.Diagnostics;

namespace PCL.Core.Game.Xbox;

public static class XboxGameLauncher
{
    public static void Launch(string gameId)
    {
        var publisherId = GetPublisherId(gameId);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"shell:AppsFolder\\{gameId}_{publisherId}!Game",
            UseShellExecute = false
        });
    }

    private static string? GetPublisherId(string gameId)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"(Get-AppxPackage -Name '{gameId}').PublisherId\"")
                {
                    UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return string.IsNullOrEmpty(output) ? null : output;
        }
        catch { return null; }
    }
}
