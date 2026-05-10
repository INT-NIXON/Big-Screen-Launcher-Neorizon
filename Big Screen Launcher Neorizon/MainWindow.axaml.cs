using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Big_Screen_Launcher_Neorizon.Controls;
using Big_Screen_Launcher_Neorizon.Models;
using Big_Screen_Launcher_Neorizon.Services;
using PCL.Core.App.IoC;
using PCL.Core.Game.Steam;
using PCL.Core.Game.Xbox;
using PCL.Core.Logging;

namespace Big_Screen_Launcher_Neorizon;

public partial class MainWindow : Window
{
    private List<GameItem> _allGames = [];
    private List<GameCardControl> _tiles = [];
    private int _selectedIndex = 0;
    private XInputService? _xinput;
    private GamepadService? _gamepad;
    private Avalonia.Threading.DispatcherTimer? _inputTimer;
    private bool _isPlayStation;

    public MainWindow()
    {
        InitializeComponent();

        Lifecycle.OnLoading();
        GameLibraryService.Load();
        Lifecycle.OnWindowCreated();
        LogWrapper.Info($"Game library loaded: {GameLibraryService.Games.Count} games");

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        ExtendClientAreaToDecorationsHint = true;
        WindowState = WindowState.Maximized;

        _allGames = [.. GameLibraryService.Games];

        StartInput();
        _isPlayStation = !_xinput!.IsConnected && DetectPlayStationController();
        LoadHintImages();

        BuildTiles();
        AnimateEntranceAsync();

        LoadingOverlay.IsVisible = false;
        MainContent.IsVisible = true;

        Focus();
    }

    // ---- Controller detection ----

    private static bool DetectPlayStationController()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            using var baseKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Enum\HID");
            if (baseKey == null) return false;

            string[] psPrefixes = ["VID_054C&PID_0CE6", "VID_054C&PID_0DF2", "VID_054C&PID_09CC",
                                   "VID_054C&PID_0DA0", "VID_054C&PID_0BA0"];
            foreach (var name in baseKey.GetSubKeyNames())
                foreach (var prefix in psPrefixes)
                    if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return true;
        }
        catch { }
        return false;
    }

    // ---- Hint images ----

    private void LoadHintImages()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var family = _isPlayStation ? "PlayStation Series" : "Xbox Series";
        var resDir = Path.Combine(baseDir, "Resources", family);

        LoadImage(HintDpad, Path.Combine(resDir, _isPlayStation ? "playstation_dpad.png" : "xbox_dpad.png"));
        LoadImage(HintA, Path.Combine(resDir, _isPlayStation ? "playstation_button_cross.png" : "xbox_button_a.png"));
        LoadImage(HintB, Path.Combine(resDir, _isPlayStation ? "playstation_button_circle.png" : "xbox_button_b.png"));
    }

    private void SwitchHintImages(bool playStation)
    {
        if (_isPlayStation == playStation) return;
        _isPlayStation = playStation;
        LoadHintImages();
    }

    private static void LoadImage(Image img, string path)
    {
        if (File.Exists(path))
        {
            try { img.Source = new Bitmap(path); }
            catch { }
        }
    }

    // ---- Game tiles ----

    private void BuildTiles()
    {
        GamePanel.Children.Clear();
        _tiles.Clear();

        for (int i = 0; i < _allGames.Count; i++)
        {
            var idx = i;
            var card = new GameCardControl
            {
                Width = 260,
                Height = 340
            };
            card.Bind(_allGames[i]);
            card.PointerPressed += (_, _) => { SelectTile(idx); LaunchSelected(); };
            GamePanel.Children.Add(card);
            _tiles.Add(card);
        }

        GameCountText.Text = $"{_allGames.Count} 个游戏";
        SelectTile(0);
    }

    private void SelectTile(int index)
    {
        if (index < 0 || index >= _tiles.Count)
            return;

        _tiles.ForEach(t => t.SetSelected(false));
        _selectedIndex = index;
        _tiles[index].SetSelected(true);

        SmoothScrollToTile(index);
        UpdateHeroCrossfade();

        for (int i = 0; i < _tiles.Count; i++)
            _tiles[i].SetDimmed(i != index);
    }

    private void SelectNext() => SelectTile(_selectedIndex + 1);
    private void SelectPrev() => SelectTile(_selectedIndex - 1);

    private async void SmoothScrollToTile(int index)
    {
        double tileStart = index * (260 + 20) + 48; // margin 48
        double viewWidth = GameScroller.Bounds.Width;
        if (viewWidth <= 0) return;
        double targetX = tileStart - (viewWidth - 260) / 2.0;
        if (targetX < 0) targetX = 0;

        double startX = GameScroller.Offset.X;
        double distance = targetX - startX;
        if (Math.Abs(distance) < 1) return;
        double durationMs = Math.Clamp(Math.Abs(distance) * 0.5, 200, 400);

        var tcs = new TaskCompletionSource();
        var sw = Stopwatch.StartNew();

        void OnFrame(TimeSpan _)
        {
            double elapsed = sw.Elapsed.TotalMilliseconds;
            if (elapsed >= durationMs)
            {
                GameScroller.Offset = new Vector(targetX, 0);
                tcs.TrySetResult();
                return;
            }
            double t = 1.0 - Math.Pow(1.0 - elapsed / durationMs, 3);
            GameScroller.Offset = new Vector(startX + distance * t, 0);
            RequestAnimationFrame(OnFrame);
        }

        RequestAnimationFrame(OnFrame);
        await tcs.Task;
    }

    // ---- Hero crossfade ----

    private async void UpdateHeroCrossfade()
    {
        var game = _selectedIndex >= 0 && _selectedIndex < _allGames.Count
            ? _allGames[_selectedIndex] : null;

        string? newPath = game?.HeroPath;
        if (newPath == null || !File.Exists(newPath))
        {
            HeroImage1.Source = null;
            HeroImage2.Source = null;
            return;
        }

        try
        {
            var newImg = HeroImage1.Opacity > 0.5 ? HeroImage2 : HeroImage1;
            var oldImg = HeroImage1.Opacity > 0.5 ? HeroImage1 : HeroImage2;

            var bitmap = await Task.Run(() => new Bitmap(newPath));

            if (oldImg.Source == null && newImg.Source != null)
                oldImg.Source = newImg.Source;

            newImg.Source = bitmap;
            newImg.Opacity = 1;
            oldImg.Opacity = 0;
        }
        catch { }
    }

    private void LaunchSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _allGames.Count)
            return;

        var game = _allGames[_selectedIndex];
        LogWrapper.Info($"Launching: {game.Name} ({game.Platform})");

        if (game.Platform == GamePlatform.Steam)
            SteamGameLauncher.Launch(game.AppId);
        else if (game.Platform == GamePlatform.Xbox)
            XboxGameLauncher.Launch(game.AppId);
    }

    // ---- Input handling ----

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var action = e.Key switch
        {
            Key.Left => SemanticInputAction.MoveLeft,
            Key.Right => SemanticInputAction.MoveRight,
            Key.Up => SemanticInputAction.MoveLeft,
            Key.Down => SemanticInputAction.MoveRight,
            Key.Enter => SemanticInputAction.Accept,
            Key.Space => SemanticInputAction.Accept,
            Key.Escape => SemanticInputAction.Back,
            _ => (SemanticInputAction?)null
        };

        if (action.HasValue)
        {
            HandleAction(action.Value);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    private void HandleAction(SemanticInputAction action)
    {
        switch (action)
        {
            case SemanticInputAction.MoveLeft:
            case SemanticInputAction.MoveUp:
                SelectPrev();
                break;
            case SemanticInputAction.MoveRight:
            case SemanticInputAction.MoveDown:
                SelectNext();
                break;
            case SemanticInputAction.Accept:
                LaunchSelected();
                break;
        }
    }

    // ---- Input (WGI Gamepad + XInput fallback) ----

    private void StartInput()
    {
        try
        {
            _xinput = new XInputService();  // connection detection only
            _gamepad = new GamepadService();
            _gamepad.ActionReceived += action => HandleAction(action);

            _inputTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _inputTimer.Tick += (_, _) =>
            {
                _xinput.PollNow();
                _gamepad.Poll();
            };
            _inputTimer.Start();
            _xinput.PollNow();
            _gamepad.Poll();
            LogWrapper.Info("Gamepad (WGI) polling started");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Input init error");
        }
    }

    // ---- Entrance animation ----

    private async void AnimateEntranceAsync()
    {
        foreach (var tile in _tiles)
        {
            tile.Opacity = 0;
            tile.Margin = new Thickness(40, 0, 0, 0);
        }

        await Task.Delay(150);

        for (int i = 0; i < _tiles.Count; i++)
        {
            _tiles[i].Opacity = 1;
            _tiles[i].Margin = new Thickness(0);
            await Task.Delay(60);
        }
    }

    // ---- Cleanup ----

    private void OnClosing(object? sender, EventArgs e)
    {
        _inputTimer?.Stop();
    }
}
