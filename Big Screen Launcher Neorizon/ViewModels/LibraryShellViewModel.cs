using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSLN.Core.Application;
using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace Big_Screen_Launcher_Neorizon.ViewModels;

public sealed class LibraryShellViewModel : ViewModelBase
{
    private readonly LibraryShellService _shellService;
    private readonly IControllerInputSource _controllerInputSource;
    private bool _runtimeActivated;
    private ShellState _state = ShellState.Empty;
    private string _selectedTitle = "Loading...";
    private string _selectedDescription = string.Empty;
    private string _selectedPlatform = string.Empty;
    private string _selectedAccentHex = "#5A7CF7";
    private string? _errorMessage;
    private string _currentTime = DateTime.Now.ToString("HH:mm");
    private string _batteryStatus = "100%";
    private string _settingsLabel = "Settings";
    private string _homeHeader = "Home";
    private string _gameRailTitle = "My games";
    private int _selectedIndex;

    public LibraryShellViewModel(LibraryShellService shellService, IControllerInputSource controllerInputSource)
    {
        _shellService = shellService;
        _controllerInputSource = controllerInputSource;
    }

    public ObservableCollection<GameTileViewModel> Games { get; } = [];
    public ObservableCollection<InputHintViewModel> Hints { get; } = [];

    public GameTileViewModel? Game1 => Games.ElementAtOrDefault(0);
    public GameTileViewModel? Game2 => Games.ElementAtOrDefault(1);
    public GameTileViewModel? Game3 => Games.ElementAtOrDefault(2);
    public InputHintViewModel? Hint1 => Hints.ElementAtOrDefault(0);
    public InputHintViewModel? Hint2 => Hints.ElementAtOrDefault(1);
    public InputHintViewModel? Hint3 => Hints.ElementAtOrDefault(2);
    public InputHintViewModel? Hint4 => Hints.ElementAtOrDefault(3);

    public bool HasGame1 => Game1 is not null;
    public bool HasGame2 => Game2 is not null;
    public bool HasGame3 => Game3 is not null;
    public bool HasHint1 => Hint1 is not null;
    public bool HasHint2 => Hint2 is not null;
    public bool HasHint3 => Hint3 is not null;
    public bool HasHint4 => Hint4 is not null;

    public string SafeHint1GlyphPath => Hint1?.GlyphPath ?? string.Empty;
    public string SafeHint2GlyphPath => Hint2?.GlyphPath ?? string.Empty;
    public string SafeHint3GlyphPath => Hint3?.GlyphPath ?? string.Empty;
    public string SafeHint4GlyphPath => Hint4?.GlyphPath ?? string.Empty;
    public string SafeHint1Label => Hint1?.Label ?? string.Empty;
    public string SafeHint2Label => Hint2?.Label ?? string.Empty;
    public string SafeHint3Label => Hint3?.Label ?? string.Empty;
    public string SafeHint4Label => Hint4?.Label ?? string.Empty;
    public string SafeGame1AccentHex => Game1?.AccentHex ?? "#5A7CF7";
    public string SafeGame2AccentHex => Game2?.AccentHex ?? "#5A7CF7";
    public string SafeGame3AccentHex => Game3?.AccentHex ?? "#5A7CF7";
    public bool SafeGame1Selected => Game1?.IsSelected == true;
    public bool SafeGame2Selected => Game2?.IsSelected == true;
    public bool SafeGame3Selected => Game3?.IsSelected == true;
    public string SafeGame1PlatformLabel => Game1?.PlatformLabel ?? string.Empty;
    public string SafeGame2PlatformLabel => Game2?.PlatformLabel ?? string.Empty;
    public string SafeGame3PlatformLabel => Game3?.PlatformLabel ?? string.Empty;
    public string SafeGame1Title => Game1?.Title ?? string.Empty;
    public string SafeGame2Title => Game2?.Title ?? string.Empty;
    public string SafeGame3Title => Game3?.Title ?? string.Empty;

    public string SelectedTitle
    {
        get => _selectedTitle;
        private set => SetProperty(ref _selectedTitle, value);
    }

    public string SelectedDescription
    {
        get => _selectedDescription;
        private set => SetProperty(ref _selectedDescription, value);
    }

    public string SelectedPlatform
    {
        get => _selectedPlatform;
        private set => SetProperty(ref _selectedPlatform, value);
    }

    public string SelectedAccentHex
    {
        get => _selectedAccentHex;
        private set => SetProperty(ref _selectedAccentHex, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        private set => SetProperty(ref _selectedIndex, value);
    }

    public string CurrentTime
    {
        get => _currentTime;
        private set => SetProperty(ref _currentTime, value);
    }

    public string BatteryStatus
    {
        get => _batteryStatus;
        private set => SetProperty(ref _batteryStatus, value);
    }

    public string SettingsLabel
    {
        get => _settingsLabel;
        private set => SetProperty(ref _settingsLabel, value);
    }

    public string HomeHeader
    {
        get => _homeHeader;
        private set => SetProperty(ref _homeHeader, value);
    }

    public string GameRailTitle
    {
        get => _gameRailTitle;
        private set => SetProperty(ref _gameRailTitle, value);
    }

    public void ActivateRuntime()
    {
        if (_runtimeActivated)
        {
            return;
        }

        _runtimeActivated = true;
        _controllerInputSource.ActionReceived += OnControllerActionReceived;
        _controllerInputSource.InputFamilyChanged += OnControllerInputFamilyChanged;
        _controllerInputSource.Start();
        CurrentTime = DateTime.Now.ToString("HH:mm");
        BatteryStatus = "100%";
    }

    public async Task InitializeAsync()
    {
        _state = await _shellService.LoadAsync();
        ApplyState();
    }

    public async Task HandleActionAsync(SemanticInputAction action)
    {
        if (action == SemanticInputAction.Accept)
        {
            var result = await _shellService.LaunchSelectedAsync(_state);
            _state = result.State;
        }
        else
        {
            _state = _shellService.ApplyAction(_state, action);
        }

        ApplyState();
    }

    public Task SetInputFamilyAsync(InputDeviceFamily inputFamily)
    {
        _state = _shellService.SetInputFamily(_state, inputFamily);
        ApplyState();
        return Task.CompletedTask;
    }

    public void LoadDesignData(string title, string description, string platform, string accentHex, IReadOnlyList<GameTileViewModel> games, IReadOnlyList<InputHintViewModel> hints)
    {
        Games.Clear();
        foreach (var game in games)
        {
            Games.Add(game);
        }

        Hints.Clear();
        foreach (var hint in hints)
        {
            Hints.Add(hint);
        }

        SelectedTitle = title;
        SelectedDescription = description;
        SelectedPlatform = platform;
        SelectedAccentHex = accentHex;
        SelectedIndex = 0;
        CurrentTime = "21:37";
        BatteryStatus = "82%";
        SettingsLabel = "Settings";
        HomeHeader = "Home";
        GameRailTitle = "My games";
        ErrorMessage = null;
        RaiseCollectionDerivedProperties();
    }

    public static LibraryShellViewModel CreateDesignViewModel()
    {
        var viewModel = new LibraryShellViewModel(DesignSupport.NullShellService, DesignSupport.ControllerInputSourceInstance);
        viewModel.LoadDesignData(
            title: "Forza Horizon 5",
            description: "A sample design-time hero description so Rider can render the launcher shell without app startup, XInput, or local JSON files.",
            platform: "Xbox / Windows",
            accentHex: "#D94D5C",
            games:
            [
                new GameTileViewModel { Id = "forza", Title = "Forza Horizon 5", PlatformLabel = "Xbox / Windows", AccentHex = "#D94D5C", IsSelected = true },
                new GameTileViewModel { Id = "hades", Title = "Hades", PlatformLabel = "Steam", AccentHex = "#8B5CF6", IsSelected = false },
                new GameTileViewModel { Id = "ori", Title = "Ori and the Will of the Wisps", PlatformLabel = "Steam", AccentHex = "#22C3EE", IsSelected = false },
                new GameTileViewModel { Id = "cyberpunk", Title = "Cyberpunk 2077", PlatformLabel = "Steam", AccentHex = "#F5E625", IsSelected = false },
                new GameTileViewModel { Id = "halo", Title = "Halo Infinite", PlatformLabel = "Xbox / Windows", AccentHex = "#1EAA61", IsSelected = false },
                new GameTileViewModel { Id = "starfield", Title = "Starfield", PlatformLabel = "Xbox / Windows", AccentHex = "#C8A971", IsSelected = false },
                new GameTileViewModel { Id = "diablo", Title = "Diablo IV", PlatformLabel = "Battle.net", AccentHex = "#8A2E2E", IsSelected = false },
                new GameTileViewModel { Id = "elden", Title = "Elden Ring", PlatformLabel = "Steam", AccentHex = "#C49A3C", IsSelected = false },
            ],
            hints:
            [
                new InputHintViewModel { Label = "Navigate", GlyphPath = "avares://Big_Screen_Launcher_Neorizon/Resources/Xbox%20Series/xbox_dpad.png" },
                new InputHintViewModel { Label = "Launch", GlyphPath = "avares://Big_Screen_Launcher_Neorizon/Resources/Xbox%20Series/xbox_button_a.png" },
                new InputHintViewModel { Label = "Back", GlyphPath = "avares://Big_Screen_Launcher_Neorizon/Resources/Xbox%20Series/xbox_button_b.png" },
                new InputHintViewModel { Label = "Menu", GlyphPath = "avares://Big_Screen_Launcher_Neorizon/Resources/Xbox%20Series/xbox_button_menu.png" },
            ]);
        return viewModel;
    }

    private async void OnControllerActionReceived(object? sender, SemanticInputAction action)
    {
        await HandleActionAsync(action);
    }

    private async void OnControllerInputFamilyChanged(object? sender, InputDeviceFamily inputFamily)
    {
        await SetInputFamilyAsync(inputFamily);
    }

    private void ApplyState()
    {
        var selectedGameId = _state.SelectedGame?.Id;

        Games.Clear();
        foreach (var game in _state.Games)
        {
            Games.Add(new GameTileViewModel
            {
                Id = game.Id,
                Title = game.Title,
                PlatformLabel = game.PlatformLabel,
                AccentHex = game.AccentHex,
                CoverImagePath = game.HeroImagePath,
                IsSelected = game.Id == selectedGameId,
            });
        }

        if (Games.Count > 0)
        {
            var sel = Games.FirstOrDefault(t => t.IsSelected);
            System.Diagnostics.Debug.WriteLine($"[CoverVM] games={Games.Count} firstTitle={Games[0].Title} firstCover={Games[0].CoverImagePath ?? "null"} selectedCover={sel?.CoverImagePath ?? "null"}");
        }

        Hints.Clear();
        foreach (var hint in _state.Hints)
        {
            Hints.Add(new InputHintViewModel
            {
                Label = hint.Label,
                GlyphPath = hint.GlyphPath,
            });
        }

        SelectedTitle = _state.SelectedGame?.Title ?? "No games found";
        SelectedDescription = _state.SelectedGame?.Description ?? "Add items to the local catalog to populate the launcher.";
        SelectedPlatform = _state.SelectedGame?.PlatformLabel ?? string.Empty;
        SelectedAccentHex = _state.SelectedGame?.AccentHex ?? "#5A7CF7";
        CurrentTime = DateTime.Now.ToString("HH:mm");
        BatteryStatus = "100%";
        ErrorMessage = _state.ErrorMessage;
        SelectedIndex = _state.SelectedIndex;
        RaiseCollectionDerivedProperties();
    }

    private void RaiseCollectionDerivedProperties()
    {
        RaisePropertyChanged(nameof(Game1));
        RaisePropertyChanged(nameof(Game2));
        RaisePropertyChanged(nameof(Game3));
        RaisePropertyChanged(nameof(Hint1));
        RaisePropertyChanged(nameof(Hint2));
        RaisePropertyChanged(nameof(Hint3));
        RaisePropertyChanged(nameof(Hint4));
        RaisePropertyChanged(nameof(HasGame1));
        RaisePropertyChanged(nameof(HasGame2));
        RaisePropertyChanged(nameof(HasGame3));
        RaisePropertyChanged(nameof(HasHint1));
        RaisePropertyChanged(nameof(HasHint2));
        RaisePropertyChanged(nameof(HasHint3));
        RaisePropertyChanged(nameof(HasHint4));
        RaisePropertyChanged(nameof(SafeHint1GlyphPath));
        RaisePropertyChanged(nameof(SafeHint2GlyphPath));
        RaisePropertyChanged(nameof(SafeHint3GlyphPath));
        RaisePropertyChanged(nameof(SafeHint4GlyphPath));
        RaisePropertyChanged(nameof(SafeHint1Label));
        RaisePropertyChanged(nameof(SafeHint2Label));
        RaisePropertyChanged(nameof(SafeHint3Label));
        RaisePropertyChanged(nameof(SafeHint4Label));
        RaisePropertyChanged(nameof(SafeGame1AccentHex));
        RaisePropertyChanged(nameof(SafeGame2AccentHex));
        RaisePropertyChanged(nameof(SafeGame3AccentHex));
        RaisePropertyChanged(nameof(SafeGame1Selected));
        RaisePropertyChanged(nameof(SafeGame2Selected));
        RaisePropertyChanged(nameof(SafeGame3Selected));
        RaisePropertyChanged(nameof(SafeGame1PlatformLabel));
        RaisePropertyChanged(nameof(SafeGame2PlatformLabel));
        RaisePropertyChanged(nameof(SafeGame3PlatformLabel));
        RaisePropertyChanged(nameof(SafeGame1Title));
        RaisePropertyChanged(nameof(SafeGame2Title));
        RaisePropertyChanged(nameof(SafeGame3Title));
    }

    private static class DesignSupport
    {
        public static readonly LibraryShellService NullShellService = new(
            new NullGameCatalogRepository(),
            new NullSettingsRepository(),
            new NullGameLauncher(),
            new InputHintService(new NullInputGlyphCatalog()),
            new ShellStateReducer(),
            new NullGameDiscoveryAggregator());

        public static readonly IControllerInputSource ControllerInputSourceInstance = new NullControllerInputSource();

        private sealed class NullGameDiscoveryAggregator : IGameDiscoveryAggregator
        {
            public Task<IReadOnlyList<GameEntry>> DiscoverAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GameEntry>>([]);
        }

        private sealed class NullGameCatalogRepository : IGameCatalogRepository
        {
            public Task<IReadOnlyList<GameEntry>> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GameEntry>>([]);
        }

        private sealed class NullSettingsRepository : ISettingsRepository
        {
            public Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(LauncherSettings.Default);
            public Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class NullGameLauncher : IGameLauncher
        {
            public Task LaunchAsync(LaunchTarget launchTarget, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class NullInputGlyphCatalog : IInputGlyphCatalog
        {
            public string GetGlyphPath(InputDeviceFamily inputFamily, SemanticInputAction action) => string.Empty;
        }

        private sealed class NullControllerInputSource : IControllerInputSource
        {
            public event EventHandler<SemanticInputAction>? ActionReceived;
            public event EventHandler<InputDeviceFamily>? InputFamilyChanged;
            public void Start() { }
            public void Stop() { }
        }
    }
}
