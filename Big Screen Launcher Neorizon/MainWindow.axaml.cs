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
using Avalonia.Threading;
using Big_Screen_Launcher_Neorizon.Controls;
using Big_Screen_Launcher_Neorizon.Input;
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
    private int _selectedIndex;
    private ControllerService? _controller;
    private DispatcherTimer? _inputTimer;
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
        LoadHintImages();

        BuildTiles();
        AnimateEntranceAsync();

        MainContent.IsVisible = true;
        Focus();
    }

    // ── Hint images (Xbox vs PlayStation glyphs) ──

    private void LoadHintImages()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var family = _isPlayStation ? "PlayStation Series" : "Xbox Series";
        var resDir = Path.Combine(baseDir, "Resources", "Images", family);

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

    // ── Game tiles ──

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
        double tileStart = index * (260 + 20) + 48;
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

    // ── Hero crossfade ──

    private async void UpdateHeroCrossfade()
    {
        var game = _selectedIndex >= 0 && _selectedIndex < _allGames.Count
            ? _allGames[_selectedIndex] : null;

        string? newPath = game?.LogoPath;
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

    // ── Input (keyboard + controller) ──

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var action = KeyActionMap.Map(e.Key);
        if (action.HasValue)
        {
            HandleAction(action.Value);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    private void HandleAction(ControllerAction action)
    {
        switch (action)
        {
            case ControllerAction.MoveLeft:
            case ControllerAction.MoveUp:
                SelectPrev();
                break;

            case ControllerAction.MoveRight:
            case ControllerAction.MoveDown:
                SelectNext();
                break;

            case ControllerAction.Accept:
                LaunchSelected();
                break;

            case ControllerAction.PageUp:
                SelectTile(0);
                break;

            case ControllerAction.PageDown:
                SelectTile(_tiles.Count - 1);
                break;
        }
    }

    private void StartInput()
    {
        _controller = new ControllerService();
        _controller.ActionReceived += action => HandleAction(action);
        _controller.FamilyChanged += family => SwitchHintImages(family == InputDeviceFamily.PlayStation);

        // First poll to detect device family immediately
        _controller.Poll();

        _inputTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _inputTimer.Tick += (_, _) => _controller.Poll();
        _inputTimer.Start();

        LogWrapper.Info("ControllerService started (WGI + XInput, unified)");
    }

    // ── Entrance animation ──

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

    // ── Cleanup ──

    private void OnClosing(object? sender, EventArgs e)
    {
        _inputTimer?.Stop();
    }
}
