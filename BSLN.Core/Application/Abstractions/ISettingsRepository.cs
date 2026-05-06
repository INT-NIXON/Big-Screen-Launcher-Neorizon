using BSLN.Core.Domain;

namespace BSLN.Core.Application.Abstractions;

public interface ISettingsRepository
{
    Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default);
}
