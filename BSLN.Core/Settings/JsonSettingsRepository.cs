using System.Text.Json;
using System.Threading;
using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace BSLN.Core.Settings;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _settingsPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public JsonSettingsRepository(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public async Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = LauncherSettings.Default;
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(_settingsPath);
        var settings = await JsonSerializer.DeserializeAsync<LauncherSettings>(stream, JsonOptions, cancellationToken);
        return settings ?? LauncherSettings.Default;
    }

    public async Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            await using var stream = new FileStream(_settingsPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
