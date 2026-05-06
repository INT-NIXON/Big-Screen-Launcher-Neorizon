namespace BSLN.Core.Domain;

public sealed record GameEntry(
    string Id,
    string Title,
    string Description,
    string PlatformLabel,
    string? HeroImagePath,
    string AccentHex,
    LaunchTarget LaunchTarget,
    GameSource Source = GameSource.Manual);
