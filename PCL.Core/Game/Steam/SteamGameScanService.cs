using System;
using System.Text;
using Narod;
using Narod.SteamGameFinder;
using PCL.Core.App.IoC;

namespace PCL.Core.Game.Steam;

[LifecycleService(LifecycleState.Loading, Priority = -100)]
public sealed class SteamGameScanService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private SteamGameScanService() : base("steam-scan", "Steam游戏扫描", false)
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
            Context.Info($"Steam 安装路径: {steamPath}");

            var libraryPaths = locator.getSteamLibraryLocations();
            Context.Info($"Steam 库数量: {libraryPaths.Count}");
            foreach (var path in libraryPaths)
                Context.Info($"Steam库: {path}");

            var allGames = locator.getAllGames();
            Context.Info($"已安装 Steam 游戏总数: {allGames.Count}");

            var result = new StringBuilder();
            result.Append("Steam 游戏扫描结果:");
            foreach (var game in allGames)
                result.Append("\n        [✓] ").Append(game.steamGameName).Append(" | ").Append(game.steamGameLocation);
            Context.Info(result.ToString());

            Context.Info("Steam 游戏扫描完成");
        }
        catch (Exception ex)
        {
            Context.Error("Steam 游戏扫描异常", ex);
        }
    }
}