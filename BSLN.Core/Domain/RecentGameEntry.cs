namespace BSLN.Core.Domain;

public sealed record RecentGameEntry(
    string GameId,
    DateTimeOffset LastLaunchedAt);
