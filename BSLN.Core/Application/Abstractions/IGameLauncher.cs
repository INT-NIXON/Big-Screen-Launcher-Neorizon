using BSLN.Core.Domain;

namespace BSLN.Core.Application.Abstractions;

public interface IGameLauncher
{
    Task LaunchAsync(LaunchTarget launchTarget, CancellationToken cancellationToken = default);
}
