using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Big_Screen_Launcher_Neorizon.Models;
using PCL.Core.Game.Steam;
using PCL.Core.Game.Xbox;
using PCL.Core.Logging;

namespace Big_Screen_Launcher_Neorizon.Services;

public static class GameLibraryService
{
    public static IReadOnlyList<GameItem> Games { get; private set; } = [];

    public static void Load()
    {
        LogWrapper.Info("Building unified game library...");

        // Wait up to 10s for game scans to complete
        for (int i = 0; i < 50; i++)
        {
            if (SteamGameData.IsScanComplete || XboxGameData.IsScanComplete)
                break;
            Thread.Sleep(200);
        }

        var list = new List<GameItem>();

        if (SteamGameData.IsScanComplete)
        {
            foreach (var sg in SteamGameData.InstalledGames)
            {
                var pic = SteamGameData.Pictures.FirstOrDefault(p => p.AppId == sg.steamGameID);
                list.Add(new GameItem
                {
                    AppId = sg.steamGameID,
                    Name = sg.steamGameName,
                    Platform = GamePlatform.Steam,
                    CoverPath = pic?.IconPath ?? pic?.LogoPath,
                    LogoPath = pic?.LogoPath,
                    InstallPath = sg.steamGameLocation
                });
            }
            LogWrapper.Info($"Steam games: {SteamGameData.InstalledGames.Length}");
        }

        if (XboxGameData.IsScanComplete)
        {
            foreach (var xg in XboxGameData.InstalledGames)
            {
                var pic = XboxGameData.Pictures.FirstOrDefault(p => p.Id == xg.Id.Value);
                list.Add(new GameItem
                {
                    AppId = xg.Id.Value,
                    Name = xg.DisplayName,
                    Platform = GamePlatform.Xbox,
                    CoverPath = pic?.IconPath ?? pic?.HeroPath,
                    LogoPath = pic?.HeroPath,
                    InstallPath = xg.Path.GetFullPath()
                });
            }
            LogWrapper.Info($"Xbox games: {XboxGameData.InstalledGames.Length}");
        }

        Games = [.. list.OrderBy(g => g.Name)];
        LogWrapper.Info($"Total games: {Games.Count}");
    }
}
