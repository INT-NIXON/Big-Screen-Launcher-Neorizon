using BSLN.Core.Domain;

namespace BSLN.Core.Application;

public sealed record ShellState(
    IReadOnlyList<GameEntry> Games,
    int SelectedIndex,
    FocusRegion FocusRegion,
    InputDeviceFamily ActiveInputFamily,
    string? ErrorMessage,
    bool IsFullscreen,
    IReadOnlyList<HintDefinition> Hints)
{
    public static ShellState Empty { get; } = new(
        Games: [],
        SelectedIndex: 0,
        FocusRegion: FocusRegion.Library,
        ActiveInputFamily: InputDeviceFamily.Xbox,
        ErrorMessage: null,
        IsFullscreen: true,
        Hints: []);

    public GameEntry? SelectedGame =>
        Games.Count == 0 || SelectedIndex < 0 || SelectedIndex >= Games.Count
            ? null
            : Games[SelectedIndex];
}
