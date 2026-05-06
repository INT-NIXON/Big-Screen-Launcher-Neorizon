using System.Text.Json;
using BSLN.Core.Application.Abstractions;
using BSLN.Core.Domain;

namespace BSLN.Infrastructure.Catalog;

public sealed class JsonGameCatalogRepository : IGameCatalogRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _catalogPath;

    public JsonGameCatalogRepository(string catalogPath)
    {
        _catalogPath = catalogPath;
    }

    public async Task<IReadOnlyList<GameEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_catalogPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_catalogPath)!);
            await using var createStream = File.Create(_catalogPath);
            await JsonSerializer.SerializeAsync(createStream, new List<GameEntry>(), JsonOptions, cancellationToken);
            return [];
        }

        await using var stream = File.OpenRead(_catalogPath);
        var games = await JsonSerializer.DeserializeAsync<List<GameEntry>>(stream, JsonOptions, cancellationToken);
        return games ?? [];
    }
}
