using BSLN.Core.Domain;

namespace BSLN.Core.Application.Abstractions;

public interface IGameCatalogRepository
{
    Task<IReadOnlyList<GameEntry>> LoadAsync(CancellationToken cancellationToken = default);
}
