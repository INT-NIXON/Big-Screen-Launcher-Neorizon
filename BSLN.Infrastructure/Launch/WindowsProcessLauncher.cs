using System.Diagnostics;
using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace BSLN.Infrastructure.Launch;

public sealed class WindowsProcessLauncher : IGameLauncher
{
    public Task LaunchAsync(LaunchTarget launchTarget, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = launchTarget.Kind switch
        {
            LaunchKind.Executable => new ProcessStartInfo
            {
                FileName = launchTarget.PathOrUri,
                Arguments = launchTarget.Arguments ?? string.Empty,
                WorkingDirectory = launchTarget.WorkingDirectory ?? string.Empty,
                UseShellExecute = true,
            },
            LaunchKind.Uri => new ProcessStartInfo
            {
                FileName = launchTarget.PathOrUri,
                UseShellExecute = true,
            },
            _ => throw new InvalidOperationException($"Unsupported launch kind: {launchTarget.Kind}"),
        };

        var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start the selected target.");
        }

        return Task.CompletedTask;
    }
}
