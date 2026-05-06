using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace BSLN.Core.Application;

public sealed class LibraryShellService(
    IGameCatalogRepository gameCatalogRepository,
    ISettingsRepository settingsRepository,
    IGameLauncher gameLauncher,
    InputHintService inputHintService,
    ShellStateReducer shellStateReducer,
    IGameDiscoveryAggregator discoveryService)
{
    public async Task<ShellState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var manualGames = await gameCatalogRepository.LoadAsync(cancellationToken);
        // Filter out old sample entries in case user has stale catalog.json
        var filteredManual = manualGames.Where(g => g.Id is not ("notepad" or "calculator" or "steam")).ToList();
        var discoveredGames = await discoveryService.DiscoverAllAsync(cancellationToken);

        // Merge: manual first, then discovered (dedup by Id)
        var merged = filteredManual.Concat(discoveredGames).ToList();
        var settings = await settingsRepository.LoadAsync(cancellationToken);
        var selectedIndex = ResolveSelectedIndex(merged, settings.LastSelectedGameId);
        var activeInputFamily = settings.PreferredGlyphFamily == InputDeviceFamily.Auto
            ? InputDeviceFamily.Xbox
            : settings.PreferredGlyphFamily;

        return new ShellState(
            Games: merged,
            SelectedIndex: selectedIndex,
            FocusRegion: FocusRegion.Library,
            ActiveInputFamily: activeInputFamily,
            ErrorMessage: null,
            IsFullscreen: settings.StartFullscreen,
            Hints: inputHintService.GetLibraryHints(activeInputFamily));
    }

    public ShellState ApplyAction(ShellState currentState, SemanticInputAction action)
    {
        return shellStateReducer.Reduce(currentState, action) with
        {
            Hints = inputHintService.GetLibraryHints(currentState.ActiveInputFamily),
        };
    }

    public ShellState SetInputFamily(ShellState currentState, InputDeviceFamily inputFamily)
    {
        return currentState with
        {
            ActiveInputFamily = inputFamily,
            Hints = inputHintService.GetLibraryHints(inputFamily),
        };
    }

    public async Task<(ShellState State, bool Launched)> LaunchSelectedAsync(ShellState currentState, CancellationToken cancellationToken = default)
    {
        var selectedGame = currentState.SelectedGame;
        if (selectedGame is null)
        {
            return (currentState with { ErrorMessage = "No game is selected." }, false);
        }

        try
        {
            await gameLauncher.LaunchAsync(selectedGame.LaunchTarget, cancellationToken);
            await PersistSelectionAsync(currentState, cancellationToken, selectedGame.Id);
            return (currentState with { ErrorMessage = null }, true);
        }
        catch (Exception exception)
        {
            return (currentState with { ErrorMessage = exception.Message }, false);
        }
    }

    public async Task PersistSelectionAsync(ShellState currentState, CancellationToken cancellationToken = default, string? launchedGameId = null)
    {
        var currentSettings = await settingsRepository.LoadAsync(cancellationToken);
        var selectedGameId = launchedGameId ?? currentState.SelectedGame?.Id;
        var recentGames = BuildRecentGames(currentSettings.RecentGames, launchedGameId);

        var nextSettings = currentSettings with
        {
            LastSelectedGameId = selectedGameId,
            StartFullscreen = currentState.IsFullscreen,
            RecentGames = recentGames,
        };

        await settingsRepository.SaveAsync(nextSettings, cancellationToken);
    }

    private static int ResolveSelectedIndex(IReadOnlyList<GameEntry> games, string? lastSelectedGameId)
    {
        if (games.Count == 0 || string.IsNullOrWhiteSpace(lastSelectedGameId))
        {
            return 0;
        }

        var match = games
            .Select((game, index) => (game, index))
            .FirstOrDefault(entry => entry.game.Id == lastSelectedGameId);

        return match == default ? 0 : match.index;
    }

    private static IReadOnlyList<RecentGameEntry> BuildRecentGames(IReadOnlyList<RecentGameEntry> existing, string? launchedGameId)
    {
        if (string.IsNullOrWhiteSpace(launchedGameId))
        {
            return existing;
        }

        var next = existing
            .Where(entry => entry.GameId != launchedGameId)
            .Prepend(new RecentGameEntry(launchedGameId, DateTimeOffset.UtcNow))
            .Take(12)
            .ToArray();

        return next;
    }
}
