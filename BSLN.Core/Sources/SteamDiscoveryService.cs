using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace BSLN.Core.Sources;

public sealed partial class SteamDiscoveryService : IDiscoveredGameSource
{
    private static readonly string[] SteamInstallCandidates =
    [
        @"C:\Program Files (x86)\Steam",
        @"C:\Program Files\Steam",
    ];

    public string SourceName => "Steam";

    public Task<IReadOnlyList<DiscoveredGameEntry>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DiscoveredGameEntry>();
        var steamDir = FindSteamInstall();

        if (steamDir is null)
        {
            return Task.FromResult<IReadOnlyList<DiscoveredGameEntry>>(results);
        }

        var appInfoBytes = SteamAppInfoParser.LoadAppInfoBytes(steamDir);

        var steamAppsDir = Path.Combine(steamDir, "steamapps");
        var libraryPaths = ParseLibraryFolders(steamAppsDir);

        if (!libraryPaths.Contains(steamAppsDir, StringComparer.OrdinalIgnoreCase))
        {
            libraryPaths.Insert(0, steamAppsDir);
        }

        foreach (var libraryPath in libraryPaths)
        {
            if (!Directory.Exists(libraryPath))
            {
                continue;
            }

            foreach (var manifestFile in Directory.EnumerateFiles(libraryPath, "appmanifest_*.acf"))
            {
                var game = ParseAppManifest(manifestFile, appInfoBytes, steamDir);
                if (game is not null)
                {
                    results.Add(game);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<DiscoveredGameEntry>>(results);
    }

    private static string? FindSteamInstall()
    {
        foreach (var candidate in SteamInstallCandidates)
        {
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "steam.exe")))
            {
                return candidate;
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var regPath = @"SOFTWARE\Valve\Steam";
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath)
                    ?? Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regPath);
                var installPath = key?.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                {
                    return installPath;
                }
            }
            catch { }
        }

        return null;
    }

    private static List<string> ParseLibraryFolders(string steamAppsDir)
    {
        var vdfPath = Path.Combine(steamAppsDir, "libraryfolders.vdf");
        if (!File.Exists(vdfPath)) return [];

        try
        {
            var text = File.ReadAllText(vdfPath);
            var libraries = new List<string>();
            foreach (Match match in LibraryFolderRegex().Matches(text))
            {
                var path = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Replace("\\\\", "\\");
                    libraries.Add(Path.Combine(path, "steamapps"));
                }
            }
            return libraries;
        }
        catch { return []; }
    }

    private static DiscoveredGameEntry? ParseAppManifest(string manifestPath, byte[]? appInfoBytes, string steamDir)
    {
        try
        {
            var text = File.ReadAllText(manifestPath);
            if (!uint.TryParse(AppIdRegex().Match(text).Groups[1].Value, out var appId)) return null;
            var name = NameRegex().Match(text).Groups[1].Value;
            var installDir = InstallDirRegex().Match(text).Groups[1].Value;
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (appInfoBytes is not null && !SteamAppInfoParser.IsGameApp(appInfoBytes, appId, name)) return null;

            var libraryPath = Path.GetDirectoryName(manifestPath)!;
            var coverPath = FindSteamCoverImage(libraryPath, steamDir, appId);
            var gameDir = Path.Combine(libraryPath, "common", installDir);
            var exePath = FindGameExecutable(gameDir);

            return new DiscoveredGameEntry(
                Title: name,
                Description: $"Steam — app ID {appId}",
                PlatformLabel: "Steam",
                ImagePathOrAccentHex: "#1B2838",
                CoverImagePath: coverPath,
                LaunchTarget: exePath is not null
                    ? new LaunchTarget(LaunchKind.Executable, exePath)
                    : new LaunchTarget(LaunchKind.Executable, $"steam://rungameid/{appId}"),
                Source: GameSource.Steam);
        }
        catch { return null; }
    }

    private static string? FindGameExecutable(string gameDir)
    {
        if (!Directory.Exists(gameDir)) return null;
        try
        {
            var exes = Directory.EnumerateFiles(gameDir, "*.exe", SearchOption.TopDirectoryOnly)
                .Where(e => { var n = Path.GetFileNameWithoutExtension(e); return !n.Contains("UnityCrash", StringComparison.OrdinalIgnoreCase) && !n.Contains("Launcher", StringComparison.OrdinalIgnoreCase) && n != "crashhandler" && n != "uninstall"; }).ToList();
            var dir = Path.GetFileName(gameDir);
            return exes.FirstOrDefault(e => Path.GetFileNameWithoutExtension(e).Equals(dir, StringComparison.OrdinalIgnoreCase)) ?? exes.FirstOrDefault();
        }
        catch { return null; }
    }

    private static string? FindSteamCoverImage(string libraryPath, string steamDir, uint appId)
    {
        try
        {
            var candidates = new List<string>();

            if (!string.IsNullOrEmpty(steamDir))
            {
                var grid = Path.Combine(steamDir, "grid");
                if (Directory.Exists(grid)) { candidates.Add(Path.Combine(grid, $"{appId}.png")); candidates.Add(Path.Combine(grid, $"{appId}p.png")); }

                var cache = Path.Combine(steamDir, "appcache", "library", "cache");
                if (Directory.Exists(cache))
                    foreach (var f in Directory.EnumerateFiles(cache, $"{appId}_*"))
                        if (Path.GetExtension(f).ToLowerInvariant() is ".jpg" or ".png") candidates.Add(f);

                var userdata = Path.Combine(steamDir, "userdata");
                if (Directory.Exists(userdata))
                    foreach (var uid in Directory.EnumerateDirectories(userdata))
                    { var ug = Path.Combine(uid, "config", "grid"); if (Directory.Exists(ug)) { var ugf = Path.Combine(ug, $"{appId}.png"); if (File.Exists(ugf)) candidates.Add(ugf); } }
            }

            var found = candidates.FirstOrDefault(File.Exists);
            System.Diagnostics.Debug.WriteLine($"[Cover] appId={appId} local={found ?? "null"}");

            // Fallback: Steam CDN header image
            var cdn = $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{appId}/header.jpg";
            System.Diagnostics.Debug.WriteLine($"[Cover] CDN fallback: {cdn}");
            return cdn;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Cover] error: {ex.Message}");
            return null;
        }
    }

    [GeneratedRegex("\"path\"\\s+\"([^\"]+)\"")] private static partial Regex LibraryFolderRegex();
    [GeneratedRegex("\"appid\"\\s+\"(\\d+)\"")] private static partial Regex AppIdRegex();
    [GeneratedRegex("\"name\"\\s+\"([^\"]+)\"")] private static partial Regex NameRegex();
    [GeneratedRegex("\"installdir\"\\s+\"([^\"]+)\"")] private static partial Regex InstallDirRegex();
}
