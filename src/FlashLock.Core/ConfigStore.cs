using System.Text.Json;

namespace FlashLock.Core;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string GetMetadataDirectory(string driveRoot) =>
        Path.Combine(NormalizeRoot(driveRoot), ".flashlock");

    public string GetConfigPath(string driveRoot) =>
        Path.Combine(GetMetadataDirectory(driveRoot), "config.json");

    public string GetSnapshotPath(string driveRoot, string fileName = FlashLockConfig.DefaultSnapshotFileName) =>
        Path.Combine(GetMetadataDirectory(driveRoot), fileName);

    public async Task<FlashLockConfig?> LoadAsync(string driveRoot, CancellationToken cancellationToken = default)
    {
        var path = GetConfigPath(driveRoot);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return await JsonSerializer.DeserializeAsync<FlashLockConfig>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveAsync(string driveRoot, FlashLockConfig config, CancellationToken cancellationToken = default)
    {
        var directory = GetMetadataDirectory(driveRoot);
        Directory.CreateDirectory(directory);
        TryHideMetadataDirectory(directory);

        var path = GetConfigPath(driveRoot);
        var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");

        await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, config, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temp, path, overwrite: true);
    }

    private static string NormalizeRoot(string driveRoot)
    {
        var full = Path.GetFullPath(driveRoot);
        var root = Path.GetPathRoot(full);
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("A drive root is required.", nameof(driveRoot));
        }

        return root;
    }

    private static void TryHideMetadataDirectory(string directory)
    {
        try
        {
            var attributes = File.GetAttributes(directory);
            File.SetAttributes(directory, attributes | FileAttributes.Hidden | FileAttributes.System);
        }
        catch
        {
            // Cosmetic only. Protection must not fail because Hidden/System attributes could not be set.
        }
    }
}
