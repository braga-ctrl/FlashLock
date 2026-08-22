using System.Security.AccessControl;
using System.Text.Json;

namespace FlashLock.Core;

public sealed record AclSnapshotEntry(string RelativePath, bool IsDirectory, string Sddl);

public sealed class AclSnapshotStore
{
    private const AccessControlSections Sections = AccessControlSections.Access;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<int> CaptureAsync(string driveRoot, string snapshotPath, CancellationToken cancellationToken = default)
    {
        var root = DriveSafetyGuard.NormalizeRoot(driveRoot);
        var entries = EnumerateProtectedObjects(root).ToList();
        var temp = snapshotPath + ".tmp-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

        await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.WriteThrough))
        await using (var writer = new StreamWriter(stream))
        {
            foreach (var item in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sddl = GetSddl(item.FullPath, item.IsDirectory);
                var record = new AclSnapshotEntry(item.RelativePath, item.IsDirectory, sddl);
                await writer.WriteLineAsync(JsonSerializer.Serialize(record, JsonOptions));
            }

            await writer.FlushAsync(cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temp, snapshotPath, overwrite: true);
        return entries.Count;
    }

    public async Task RestoreAsync(string driveRoot, string snapshotPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(snapshotPath))
        {
            throw new FileNotFoundException("FlashLock ACL recovery snapshot is missing.", snapshotPath);
        }

        var root = DriveSafetyGuard.NormalizeRoot(driveRoot);
        var entries = new List<AclSnapshotEntry>();

        await using (var stream = File.OpenRead(snapshotPath))
        using (var reader = new StreamReader(stream))
        {
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                entries.Add(JsonSerializer.Deserialize<AclSnapshotEntry>(line, JsonOptions)
                    ?? throw new InvalidDataException("ACL snapshot contains an invalid record."));
            }
        }

        foreach (var entry in entries
                     .OrderByDescending(static x => PathDepth(x.RelativePath))
                     .ThenBy(static x => x.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = entry.RelativePath == "."
                ? root
                : Path.Combine(root, entry.RelativePath);

            if (entry.IsDirectory && !Directory.Exists(fullPath))
            {
                continue;
            }

            if (!entry.IsDirectory && !File.Exists(fullPath))
            {
                continue;
            }

            SetSddl(fullPath, entry.IsDirectory, entry.Sddl);
            var restored = GetSddl(fullPath, entry.IsDirectory);
            if (!string.Equals(restored, entry.Sddl, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"ACL restore verification failed: {entry.RelativePath}");
            }
        }
    }

    public IReadOnlyList<(string FullPath, string RelativePath, bool IsDirectory)> EnumerateProtectedObjects(string driveRoot)
    {
        var root = DriveSafetyGuard.NormalizeRoot(driveRoot);
        var result = new List<(string FullPath, string RelativePath, bool IsDirectory)>
        {
            (root, ".", true)
        };

        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(current))
            {
                var relative = Path.GetRelativePath(root, path);
                if (IsExcludedTopLevel(relative))
                {
                    continue;
                }

                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidOperationException($"Reparse points are not supported in v0.1: {relative}");
                }

                var isDirectory = (attributes & FileAttributes.Directory) != 0;
                result.Add((path, relative, isDirectory));
                if (isDirectory)
                {
                    stack.Push(path);
                }
            }
        }

        return result;
    }

    private static string GetSddl(string path, bool isDirectory)
    {
        FileSystemSecurity security = isDirectory
            ? new DirectoryInfo(path).GetAccessControl(Sections)
            : new FileInfo(path).GetAccessControl(Sections);
        return security.GetSecurityDescriptorSddlForm(Sections);
    }

    private static void SetSddl(string path, bool isDirectory, string sddl)
    {
        if (isDirectory)
        {
            var security = new DirectorySecurity();
            security.SetSecurityDescriptorSddlForm(sddl, Sections);
            new DirectoryInfo(path).SetAccessControl(security);
        }
        else
        {
            var security = new FileSecurity();
            security.SetSecurityDescriptorSddlForm(sddl, Sections);
            new FileInfo(path).SetAccessControl(security);
        }
    }

    private static bool IsExcludedTopLevel(string relativePath)
    {
        var first = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return first.Equals(".flashlock", StringComparison.OrdinalIgnoreCase)
            || first.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase)
            || first.Equals("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase);
    }

    private static int PathDepth(string relativePath) =>
        relativePath == "." ? -1 : relativePath.Count(static c => c is '\\' or '/') + 1;
}
