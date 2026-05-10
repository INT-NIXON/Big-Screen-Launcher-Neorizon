namespace Big_Screen_Launcher_Neorizon.Models;

public sealed class GameItem
{
    public required string AppId { get; init; }
    public required string Name { get; init; }
    public required GamePlatform Platform { get; init; }
    public string? CoverPath { get; set; }
    public string? HeroPath { get; set; }
    public string? InstallPath { get; init; }
}
