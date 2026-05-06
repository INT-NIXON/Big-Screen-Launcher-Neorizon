using BSLN.Core.Domain;

namespace BSLN.Core.Application.Abstractions;

public interface IGameDiscoveryAggregator
{
    Task<IReadOnlyList<GameEntry>> DiscoverAllAsync(CancellationToken cancellationToken = default);
}
