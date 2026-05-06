using System.Collections.Concurrent;
using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace BSLN.Core.Sources;

public sealed class AggregateDiscoveryService : IGameDiscoveryAggregator
{
    private readonly IReadOnlyList<IDiscoveredGameSource> _sources;

    public AggregateDiscoveryService(IEnumerable<IDiscoveredGameSource> sources)
    {
        _sources = sources.ToList();
    }

    public async Task<IReadOnlyList<GameEntry>> DiscoverAllAsync(CancellationToken cancellationToken = default)
    {
        var seen = new ConcurrentDictionary<string, DiscoveredGameEntry>(StringComparer.OrdinalIgnoreCase);
        var tasks = _sources.Select(source => SafeDiscoverAsync(source, seen, cancellationToken));
        await Task.WhenAll(tasks);

        var idSeed = 0;
        return seen.Values
            .Select(g => new GameEntry(
                Id: $"discovered-{Interlocked.Increment(ref idSeed)}",
                Title: g.Title,
                Description: g.Description ?? $"Source: {g.PlatformLabel}",
                PlatformLabel: g.PlatformLabel,
                HeroImagePath: g.CoverImagePath,
                AccentHex: g.ImagePathOrAccentHex ?? "#555555",
                LaunchTarget: g.LaunchTarget,
                Source: g.Source))
            .ToList();
    }

    private static async Task SafeDiscoverAsync(
        IDiscoveredGameSource source,
        ConcurrentDictionary<string, DiscoveredGameEntry> seen,
        CancellationToken cancellationToken)
    {
        try
        {
            var games = await source.DiscoverAsync(cancellationToken);
            foreach (var game in games)
            {
                seen.TryAdd(game.Title, game);
            }
        }
        catch
        {
            // Source failed silently
        }
    }
}
