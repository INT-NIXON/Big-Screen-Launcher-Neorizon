using BSLN.Core.Domain;

namespace BSLN.Core.Application.Abstractions;

public interface IDiscoveredGameSource
{
    string SourceName { get; }
    Task<IReadOnlyList<DiscoveredGameEntry>> DiscoverAsync(CancellationToken cancellationToken = default);
}

public sealed record DiscoveredGameEntry(
    string Title,
    string? Description,
    string PlatformLabel,
    string? ImagePathOrAccentHex,
    string? CoverImagePath,
    LaunchTarget LaunchTarget,
    GameSource Source);
