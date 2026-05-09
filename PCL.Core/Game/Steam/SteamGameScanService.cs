using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Narod;
using Narod.SteamGameFinder;
using PCL.Core.App.IoC;

namespace PCL.Core.Game.Steam;

public record SteamGamePicture(string AppId, string? IconPath, string? HeroPath);

public static class SteamGameData
{
    public static string? SteamInstallPath { get; set; }
    public static bool IsScanComplete { get; set; }

    // ignore Steamworks Common Redistributables
    public static readonly HashSet<string> ExcludedAppIds = ["228980"];

    private static SteamGameLocator.GameStruct[] _games = [];
    public static SteamGameLocator.GameStruct[] InstalledGames
    {
        get => _games;
        set => _games = value ?? [];
    }

    public static List<SteamGamePicture> Pictures { get; set; } = [];
}

[LifecycleService(LifecycleState.Loading, Priority = -100)]
public sealed class SteamGameScanService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private SteamGameScanService() : base("steam-scan", "Steam 游戏扫描", false)
    {
        _context = ServiceContext;
    }

    public override void Start()
    {
        Context.Info("正在扫描已安装的 Steam 游戏...");
        try
        {
            var locator = new SteamGameLocator(new SteamGameLocatorOptions
            {
                SuppressExceptions = true
            });

            if (!locator.getIsSteamInstalled())
            {
                Context.Info("Steam 未安装，跳过扫描");
                return;
            }

            var steamPath = locator.getSteamInstallLocation();
            SteamGameData.SteamInstallPath = steamPath;
            Context.Info($"Steam 安装路径: {steamPath}");

            var libraryPaths = locator.getSteamLibraryLocations();
            Context.Info($"Steam 库数量: {libraryPaths.Count}");
            foreach (var path in libraryPaths)
                Context.Info($"Steam 库: {path}");

            var allGames = locator.getAllGames();
            var installedGames = allGames.Where(g => !SteamGameData.ExcludedAppIds.Contains(g.steamGameID)).ToArray();
            SteamGameData.InstalledGames = installedGames;
            Context.Info($"已安装 Steam 游戏总数: {allGames.Count}");
            Context.Info($"有效 Steam 游戏总数: {installedGames.Length}");

            var result = new StringBuilder();
            result.Append("Steam 游戏扫描结果:");
            foreach (var game in installedGames)
                result.Append("\n        [✓] [").Append(game.steamGameID).Append("] ").Append(game.steamGameName).Append(" | ").Append(game.steamGameLocation);
            Context.Info(result.ToString());

            SteamGameData.IsScanComplete = true;
            Context.Info("Steam 游戏扫描完成");
        }
        catch (Exception ex)
        {
            Context.Error("Steam 游戏扫描异常", ex);
        }
    }
}