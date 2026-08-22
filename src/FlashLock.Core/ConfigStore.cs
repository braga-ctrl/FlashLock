using System.Text.Json;

namespace FlashLock.Core;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string GetConfigPath(string driveRoot) =>
        Path.Combine(driveRoot, ".flashlock", "config.json");

    public async Task<FlashLockConfig?> LoadAsync(string driveRoot, CancellationToken cancellationToken = default)
    {
        var path = GetConfigPath(driveRoot);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<FlashLockConfig>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveAsync(
        string driveRoot,
        FlashLockConfig config,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(GetConfigPath(driveRoot))!;
        Directory.CreateDirectory(directory);

        var path = GetConfigPath(driveRoot);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, config, JsonOptions, cancellationToken);
    }
}
