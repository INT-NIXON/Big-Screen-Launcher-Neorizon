using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Big_Screen_Launcher_Neorizon.Models;
using PCL.Core.Game.Steam;
using PCL.Core.Game.Xbox;

namespace Big_Screen_Launcher_Neorizon.Controls;

public partial class GameCardControl : UserControl
{
    private GameItem? _item;

    public GameCardControl()
    {
        InitializeComponent();
    }

    public GameItem? Item => _item;

    public void Bind(GameItem item)
    {
        _item = item;
        GameName.Text = item.Name;
        PlatformText.Text = item.Platform switch
        {
            GamePlatform.Steam => "Steam",
            GamePlatform.Xbox => "Xbox",
            _ => ""
        };

        if (item.CoverPath != null && File.Exists(item.CoverPath))
        {
            try { CoverImage.Source = new Avalonia.Media.Imaging.Bitmap(item.CoverPath); }
            catch { }
        }

        PlatformBadge.Background = item.Platform switch
        {
            GamePlatform.Steam => new SolidColorBrush(Color.Parse("#1B2838")),
            GamePlatform.Xbox => new SolidColorBrush(Color.Parse("#107C10")),
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public void SetSelected(bool selected)
    {
        // Selection frame fades in/out via Opacity transition
        SelectionFrame.Opacity = selected ? 1 : 0;
    }

    public void SetDimmed(bool dimmed)
    {
        TileBorder.Opacity = dimmed ? 0.75 : 1.0;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_item != null)
            LaunchGame(_item);
    }

    private static void LaunchGame(GameItem game)
    {
        if (game.Platform == GamePlatform.Steam)
            SteamGameLauncher.Launch(game.AppId);
        else
            XboxGameLauncher.Launch(game.AppId);
    }
}
