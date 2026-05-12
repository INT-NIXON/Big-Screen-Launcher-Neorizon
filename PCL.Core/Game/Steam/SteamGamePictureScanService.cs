using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PCL.Core.App.IoC;

namespace PCL.Core.Game.Steam;

[LifecycleService(LifecycleState.Loading, Priority = -110)]
public sealed class SteamGamePictureScanService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private SteamGamePictureScanService() : base("steam-picture-scan", "Steam 封面扫描", false)
    {
        _context = ServiceContext;
    }

    public override void Start()
    {
        if (!SteamGameData.IsScanComplete || SteamGameData.SteamInstallPath == null)
        {
            Context.Info("Steam 游戏尚未扫描完成，跳过封面扫描");
            return;
        }

        Context.Info("正在扫描 Steam 游戏封面...");
        try
        {
            var pictures = new List<SteamGamePicture>();
            var libraryCacheRoot = Path.Combine(SteamGameData.SteamInstallPath, "appcache", "librarycache");
            Context.Info($"封面缓存根目录: {libraryCacheRoot}");

            foreach (var game in SteamGameData.InstalledGames)
            {
                var gameCacheDir = Path.Combine(libraryCacheRoot, game.steamGameID);
                Context.Info($"    扫描: {gameCacheDir}");

                if (!Directory.Exists(gameCacheDir))
                {
                    Context.Info($"        目录不存在，跳过 [{game.steamGameID}] {game.steamGameName}");
                    continue;
                }

                string? iconPath = null;
                string? logoPath = null;

                var jpgFiles = new List<string>();
                jpgFiles.AddRange(Directory.GetFiles(gameCacheDir, "*.jpg", SearchOption.TopDirectoryOnly));
                foreach (var subDir in Directory.GetDirectories(gameCacheDir))
                    jpgFiles.AddRange(Directory.GetFiles(subDir, "*.jpg", SearchOption.TopDirectoryOnly));

                foreach (var file in jpgFiles)
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName.Contains("blur", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (iconPath == null && fileName.Contains("library_600x900", StringComparison.OrdinalIgnoreCase))
                        iconPath = file;

                    if (iconPath != null)
                        break;
                }

                var pngFiles = new List<string>();
                pngFiles.AddRange(Directory.GetFiles(gameCacheDir, "*.png", SearchOption.TopDirectoryOnly));
                foreach (var subDir in Directory.GetDirectories(gameCacheDir))
                    pngFiles.AddRange(Directory.GetFiles(subDir, "*.png", SearchOption.TopDirectoryOnly));

                foreach (var file in pngFiles)
                {
                    var fileName = Path.GetFileName(file);
                    if (logoPath == null && fileName.Contains("logo", StringComparison.OrdinalIgnoreCase))
                    {
                        logoPath = file;
                        break;
                    }
                }

                pictures.Add(new SteamGamePicture(game.steamGameID, iconPath, logoPath));
            }

            SteamGameData.Pictures = pictures;

            Context.Info($"已扫描 Steam 游戏封面: {pictures.Count} 款游戏");

            var result = new StringBuilder();
            result.Append("Steam 游戏封面扫描结果:");
            foreach (var p in pictures)
            {
                var iconStatus = p.IconPath != null ? $"[✓] {p.IconPath}" : "[✗] 未找到";
                var logoStatus = p.LogoPath != null ? $"[✓] {p.LogoPath}" : "[✗] 未找到";
                result.Append("\n        [AppID: ").Append(p.AppId).Append(']')
                      .Append("\n            图标: ").Append(iconStatus)
                      .Append("\n            Logo: ").Append(logoStatus);
            }
            Context.Info(result.ToString());

            Context.Info("Steam 游戏封面扫描完成");
        }
        catch (Exception ex)
        {
            Context.Error("Steam 游戏封面扫描异常", ex);
        }
    }
}
