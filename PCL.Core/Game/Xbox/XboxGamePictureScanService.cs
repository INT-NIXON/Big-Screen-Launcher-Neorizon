using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PCL.Core.App.IoC;

namespace PCL.Core.Game.Xbox;

[LifecycleService(LifecycleState.Loading, Priority = -130)]
public sealed class XboxGamePictureScanService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private XboxGamePictureScanService() : base("xbox-picture-scan", "Xbox 封面扫描", false)
    {
        _context = ServiceContext;
    }

    public override void Start()
    {
        if (!XboxGameData.IsScanComplete)
        {
            Context.Info("Xbox 游戏尚未扫描完成，跳过封面扫描");
            return;
        }

        Context.Info("正在扫描 Xbox 游戏封面...");
        try
        {
            var pictures = new List<XboxGamePicture>();

            foreach (var game in XboxGameData.InstalledGames)
            {
                var gamePath = game.Path.GetFullPath();
                Context.Info($"    扫描: {gamePath}");

                string? iconPath = null;
                string? heroPath = null;

                foreach (var file in ScanImageFiles(gamePath))
                {
                    var fileName = Path.GetFileName(file);

                    if (iconPath == null && IsIconFile(fileName))
                        iconPath = file;

                    if (heroPath == null && IsHeroFile(fileName))
                        heroPath = file;

                    if (iconPath != null && heroPath != null)
                        break;
                }

                pictures.Add(new XboxGamePicture(game.Id.Value, iconPath, heroPath));
            }

            XboxGameData.Pictures = pictures;

            Context.Info($"已扫描 Xbox 游戏封面: {pictures.Count} 款游戏");

            if (pictures.Count > 0)
            {
                var result = new StringBuilder();
                result.Append("Xbox 游戏封面扫描结果:");
                foreach (var p in pictures)
                {
                    var iconStatus = p.IconPath != null ? $"[✓] {p.IconPath}" : "[✗] 未找到";
                    var heroStatus = p.HeroPath != null ? $"[✓] {p.HeroPath}" : "[✗] 未找到";
                    result.Append("\n        [ID: ").Append(p.Id).Append(']')
                          .Append("\n            图标: ").Append(iconStatus)
                          .Append("\n            背景: ").Append(heroStatus);
                }
                Context.Info(result.ToString());
            }

            Context.Info("Xbox 游戏封面扫描完成");
        }
        catch (Exception ex)
        {
            Context.Error("Xbox 游戏封面扫描异常", ex);
        }
    }

    private static IEnumerable<string> ScanImageFiles(string rootPath)
    {
        const string pattern = "*.png";
        if (Directory.Exists(rootPath))
        {
            foreach (var f in Directory.GetFiles(rootPath, pattern, SearchOption.TopDirectoryOnly))
                yield return f;
        }
        foreach (var subDir in Directory.GetDirectories(rootPath))
        {
            if (!Directory.Exists(subDir)) continue;
            foreach (var f in Directory.GetFiles(subDir, pattern, SearchOption.TopDirectoryOnly))
                yield return f;
        }
    }

    private static bool IsIconFile(string fileName)
    {
        return fileName.Contains("logo", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("icon", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("appicon", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("StoreLogo", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("Square", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHeroFile(string fileName)
    {
        return fileName.Contains("hero", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("background", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("splash", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("Wide310", StringComparison.OrdinalIgnoreCase);
    }
}
