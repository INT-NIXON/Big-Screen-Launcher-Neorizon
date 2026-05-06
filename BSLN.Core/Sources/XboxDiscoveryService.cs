using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace BSLN.Core.Sources;

public sealed partial class XboxDiscoveryService : IDiscoveredGameSource
{
    public string SourceName => "Xbox Game Pass";

    public Task<IReadOnlyList<DiscoveredGameEntry>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DiscoveredGameEntry>();
        var gamingrootPackages = CollectGamingRootPackageNames();
        var appxPackages = QueryAppxPackages();

        if (appxPackages is null || appxPackages.Count == 0)
            return Task.FromResult<IReadOnlyList<DiscoveredGameEntry>>(results);

        if (!appxPackages.Any(p => p.Name == "Microsoft.GamingApp"))
            return Task.FromResult<IReadOnlyList<DiscoveredGameEntry>>(results);

        var seenFamilies = new HashSet<string>();

        foreach (var package in appxPackages)
        {
            if (package.IsFramework || package.IsResourcePackage) continue;
            if (!seenFamilies.Add(package.PackageFamilyName)) continue;
            if (!Directory.Exists(package.InstallLocation)) continue;

            string manifestContent;
            try { manifestContent = File.ReadAllText(Path.Combine(package.InstallLocation, "AppxManifest.xml")); }
            catch { continue; }

            var hasGamingRoot = gamingrootPackages.Contains(package.Name);
            if (!hasGamingRoot && !ManifestHasXboxHint(manifestContent)) continue;

            var app = ParseManifestApplication(manifestContent, package.InstallLocation);
            if (app?.Executable is null) continue;

            var exePath = Path.Combine(package.InstallLocation, app.Executable);
            if (!File.Exists(exePath)) continue;

            var icon = FindXboxIcon(package.InstallLocation);
            Debug.WriteLine($"[Xbox] {app.DisplayName} icon={icon ?? "null"}");

            results.Add(new DiscoveredGameEntry(
                Title: app.DisplayName,
                Description: "Xbox Game Pass",
                PlatformLabel: "Xbox Game Pass",
                ImagePathOrAccentHex: "#107C10",
                CoverImagePath: icon,
                LaunchTarget: new LaunchTarget(LaunchKind.Executable, exePath),
                Source: GameSource.XboxGamePass));
        }

        return Task.FromResult<IReadOnlyList<DiscoveredGameEntry>>(results);
    }

    private static HashSet<string> CollectGamingRootPackageNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var c = 'A'; c <= 'Z'; c++)
        {
            var drive = $@"{c}:\";
            if (!Directory.Exists(drive)) continue;
            var dir = ReadGamingRootTarget(Path.Combine(drive, ".GamingRoot"));
            if (dir is null || !Directory.Exists(dir)) continue;
            try
            {
                foreach (var sub in Directory.EnumerateDirectories(dir))
                {
                    var name = ReadConfigIdentityName(Path.Combine(sub, "Content", "MicrosoftGame.config"));
                    if (name is not null) names.Add(name);
                }
            }
            catch { }
        }
        return names;
    }

    private static string? ReadGamingRootTarget(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length < 5 || bytes[0] != 'R' || bytes[1] != 'G' || bytes[2] != 'B' || bytes[3] != 'X') return null;
            var chars = new List<char>();
            for (var i = 5; i < bytes.Length && bytes[i] != 0; i++) chars.Add((char)bytes[i]);
            return chars.Count > 0 ? Path.GetFullPath(new string(chars.ToArray())) : null;
        }
        catch { return null; }
    }

    private static string? ReadConfigIdentityName(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var content = File.ReadAllText(path);
            var v = ConfigVersionRegex().Match(content);
            if (!v.Success || (v.Groups[1].Value != "0" && v.Groups[1].Value != "1")) return null;
            return IdentityNameRegex().Match(content).Groups[1].Value;
        }
        catch { return null; }
    }

    private static List<AppxPackageRecord>? QueryAppxPackages()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -NonInteractive -Command \"& { $ProgressPreference='SilentlyContinue'; Get-AppxPackage | Select-Object Name, PackageFamilyName, InstallLocation, IsFramework, IsResourcePackage | ConvertTo-Json -Compress }\"",
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            if (process is null) return null;
            var json = process.StandardOutput.ReadToEnd();
            process.WaitForExit(15000);
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(json)) return null;
            json = json.Trim();
            return json.StartsWith('[') ? JsonSerializer.Deserialize<List<AppxPackageRecord>>(json) : [JsonSerializer.Deserialize<AppxPackageRecord>(json)!];
        }
        catch { return null; }
    }

    private static bool ManifestHasXboxHint(string content) =>
        content.Contains("Microsoft.Xbox.Services", StringComparison.OrdinalIgnoreCase) || content.Contains("ms-xbl-", StringComparison.OrdinalIgnoreCase);

    private static AppManifestApplication? ParseManifestApplication(string content, string installDir)
    {
        var dm = DisplayNameRegex().Match(content);
        var display = dm.Success ? dm.Groups[1].Value : null;
        if (display is not null && display.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase)) return null;
        var tag = ApplicationTagRegex().Match(content);
        if (!tag.Success) return null;
        var t = tag.Value;
        return new AppManifestApplication
        {
            DisplayName = display ?? Path.GetFileName(installDir) ?? "Xbox Game",
            Id = string.IsNullOrEmpty(ApplicationIdRegex().Match(t).Groups[1].Value) ? "App" : ApplicationIdRegex().Match(t).Groups[1].Value,
            Executable = string.IsNullOrEmpty(ApplicationExecutableRegex().Match(t).Groups[1].Value) ? null : ApplicationExecutableRegex().Match(t).Groups[1].Value,
        };
    }

    private static string? FindXboxIcon(string installDir)
    {
        try
        {
            var names = new[] { "Square44x44Logo.png", "StoreLogo.png", "Square150x150Logo.png", "Wide310x150Logo.png" };
            foreach (var n in names)
            {
                var p = Path.Combine(installDir, n);
                if (File.Exists(p)) return p;
            }
            var assets = Path.Combine(installDir, "Assets");
            if (Directory.Exists(assets))
                foreach (var n in names) { var p = Path.Combine(assets, n); if (File.Exists(p)) return p; }
        }
        catch { }
        return null;
    }

    [GeneratedRegex("<DisplayName>([^<]+)</DisplayName>")] private static partial Regex DisplayNameRegex();
    [GeneratedRegex("<Application\\b[^>]*>")] private static partial Regex ApplicationTagRegex();
    [GeneratedRegex("\\bId=\"([^\"]+)\"")] private static partial Regex ApplicationIdRegex();
    [GeneratedRegex("\\bExecutable=\"([^\"]+)\"")] private static partial Regex ApplicationExecutableRegex();
    [GeneratedRegex("<Game\\b[^>]*\\bconfigVersion=\"([^\"]+)\"")] private static partial Regex ConfigVersionRegex();
    [GeneratedRegex("<Identity\\b[^>]*\\bName=\"([^\"]+)\"")] private static partial Regex IdentityNameRegex();

    private sealed record AppxPackageRecord
    {
        public string Name { get; set; } = "";
        public string PackageFamilyName { get; set; } = "";
        public string InstallLocation { get; set; } = "";
        public bool IsFramework { get; set; }
        public bool IsResourcePackage { get; set; }
    }

    private sealed class AppManifestApplication
    {
        public string DisplayName { get; set; } = "";
        public string Id { get; set; } = "App";
        public string? Executable { get; set; }
    }
}
