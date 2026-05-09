using System;
using System.Collections.Generic;
using System.Text;
using GameFinder.StoreHandlers.Xbox;
using NexusMods.Paths;
using PCL.Core.App.IoC;

namespace PCL.Core.Game.Xbox;

public record XboxGamePicture(string Id, string? IconPath, string? HeroPath);

public static class XboxGameData
{
    public static bool IsScanComplete { get; set; }

    private static XboxGame[] _games = [];
    public static XboxGame[] InstalledGames
    {
        get => _games;
        set => _games = value ?? [];
    }

    public static List<XboxGamePicture> Pictures { get; set; } = [];
}

[LifecycleService(LifecycleState.Loading, Priority = -120)]
public sealed class XboxGameScanService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private XboxGameScanService() : base("xbox-scan", "Xbox 游戏扫描", false)
    {
        _context = ServiceContext;
    }

    public override void Start()
    {
        Context.Info("正在扫描已安装的 Xbox 游戏...");
        try
        {
            var handler = new XboxHandler(FileSystem.Shared);
            var results = handler.FindAllGames();

            var games = new List<XboxGame>();
            foreach (var result in results)
            {
                if (result.IsT0)
                    games.Add(result.AsT0);
            }

            XboxGameData.InstalledGames = games.ToArray();
            Context.Info($"已安装 Xbox 游戏总数: {games.Count}");

            var output = new StringBuilder();
            output.Append("Xbox 游戏扫描结果:");
            foreach (var game in games)
            {
                output.Append("\n        [✓] [").Append(game.Id.Value)
                      .Append("] ").Append(game.DisplayName)
                      .Append(" | ").Append(game.Path.GetFullPath());
            }
            Context.Info(output.ToString());

            XboxGameData.IsScanComplete = true;
            Context.Info("Xbox 游戏扫描完成");
        }
        catch (Exception ex)
        {
            Context.Error("Xbox 游戏扫描异常", ex);
        }
    }
}
