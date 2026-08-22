namespace FlashLock.Core;

public static class DriveSafetyGuard
{
    public static PortableDriveInfo EnsureSafeTarget(string requestedRoot, string executablePath, string? expectedSerial = null)
    {
        var requested = NormalizeRoot(requestedRoot);
        var executableRoot = NormalizeRoot(executablePath);

        if (!string.Equals(requested, executableRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("FlashLock may only protect the drive containing the running FlashLock executable.");
        }

        var systemRoot = NormalizeRoot(Environment.SystemDirectory);
        if (string.Equals(requested, systemRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to modify the Windows system drive.");
        }

        var drive = new DriveInfo(requested);
        if (!drive.IsReady)
        {
            throw new IOException($"Drive {requested} is not ready.");
        }

        if (!string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"FlashLock v0.1 requires NTFS; {requested} is {drive.DriveFormat}.");
        }

        var serial = VolumeIdentityProvider.GetSerialNumber(requested);
        if (!string.IsNullOrWhiteSpace(expectedSerial) &&
            !string.Equals(serial, expectedSerial, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The volume identity changed. Refusing to modify a different drive.");
        }

        return new PortableDriveInfo(
            drive.RootDirectory.FullName,
            drive.DriveFormat,
            drive.DriveType,
            string.IsNullOrWhiteSpace(drive.VolumeLabel) ? null : drive.VolumeLabel,
            drive.TotalSize,
            drive.AvailableFreeSpace,
            serial);
    }

    public static string NormalizeRoot(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full)
            ?? throw new ArgumentException("Unable to determine drive root.", nameof(path));
        return root;
    }
}
