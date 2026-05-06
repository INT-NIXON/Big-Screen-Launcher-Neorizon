namespace BSLN.Core.Domain;

public sealed record LauncherSettings(
    bool StartFullscreen,
    InputDeviceFamily PreferredGlyphFamily,
    string? LastSelectedGameId,
    IReadOnlyList<RecentGameEntry> RecentGames)
{
    public static LauncherSettings Default { get; } = new(
        StartFullscreen: true,
        PreferredGlyphFamily: InputDeviceFamily.Auto,
        LastSelectedGameId: null,
        RecentGames: []);
}
