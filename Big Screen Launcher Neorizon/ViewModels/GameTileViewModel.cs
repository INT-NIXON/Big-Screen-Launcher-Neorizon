namespace Big_Screen_Launcher_Neorizon.ViewModels;

public sealed class GameTileViewModel : ViewModelBase
{
    private bool _isSelected;

    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string PlatformLabel { get; init; }
    public required string AccentHex { get; init; }
    public string? CoverImagePath { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
